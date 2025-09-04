using IntegrationHub.Api.Middleware;
using IntegrationHub.Common.Configs;
using IntegrationHub.Common.Interfaces;
using IntegrationHub.Common.Providers;
using IntegrationHub.Common.Services;
using IntegrationHub.PIESP.Data;
using IntegrationHub.PIESP.Services;
using IntegrationHub.SRP.Configuration;
using IntegrationHub.SRP.Interfaces;
using IntegrationHub.SRP.Services;
using Microsoft.EntityFrameworkCore; // Add this using directive for 'UseSqlServer'
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Http.Resilience;
using Swashbuckle.AspNetCore.Filters;

// Serilog – bootstrap logger, ¿eby logowaæ od samego pocz¹tku
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// KONFIGURACJA SERILOG – musi byæ przed builder.Build() U¿ywaj konfiguracji Seriloga z appsettings.*.json
builder.Logging.ClearProviders(); // usuñ domyœlnego ConsoleLoggera itp.
builder.Host.UseSerilog((ctx, services, cfg) =>
cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services));


// Add services to the container.
// ====== DB CONTEXT ======
builder.Services.AddDbContext<PiespDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//Rejestracja ClientCertificateProvider
builder.Services.AddSingleton<IClientCertificateProvider, ClientCertificateProvider>();
// Rejestracja konfiguracji SRP
builder.Services.Configure<SrpConfig>(builder.Configuration.GetSection("ExternalServices:SRP"));
var srpConfig = builder.Configuration.GetSection("ExternalServices:SRP").Get<SrpConfig>();

builder.Services.AddTransient<SrpHttpLoggingHandler>();
// ====== HTTP CLIENTS ======
// Rejestracja klienta SOAP do SRP
//builder.Services.AddHttpClient("SrpServiceClient")
//    .AddHttpMessageHandler<SrpHttpLoggingHandler>()
//    .ConfigurePrimaryHttpMessageHandler(sp =>
//    {
//        var config = sp.GetRequiredService<IOptions<SrpConfig>>().Value;
//        var certProvider = sp.GetRequiredService<IClientCertificateProvider>();

//        var handler = new HttpClientHandler();

//        if (config.TrustServerCerificate)
//            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

//        handler.ClientCertificates.Add(certProvider.GetClientCertificate(config));

//        return handler;
//    });

builder.Services.AddHttpClient("SrpServiceClient", c =>
{
    // Ca³kowity timeout HttpClient wy³¹czamy – kontrolujemy czas przez pipeline (Attempt/Total)
    c.Timeout = Timeout.InfiniteTimeSpan;

    // Spróbuj HTTP/2 (wenn dostêpny), z fallbackiem w dó³ – lepsze mno¿enie strumieni
    c.DefaultRequestVersion = HttpVersion.Version20;
    c.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
})
// Twój logger wiadomoœci – zostaje
.AddHttpMessageHandler<SrpHttpLoggingHandler>()

// Handler gniazd – klucz do wydajnoœci równoleg³ych wywo³añ
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var config = sp.GetRequiredService<IOptions<SrpConfig>>().Value;
    var certProvider = sp.GetRequiredService<IClientCertificateProvider>();
    var clientCert = certProvider.GetClientCertificate(config);

    // ZLATA ZASADA: MaxConnectionsPerServer >= 2 * maxParallel (zapas)
    var maxConn = Math.Max(16, (config.HttpMaxConnectionsPerServer ?? 0)); // opcjonalnie z appsettings
    if (maxConn <= 0) maxConn = 32; // domyœlnie pod bulk

    var h = new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        MaxConnectionsPerServer = maxConn,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5), // rotacja po³¹czeñ (DNS/zdrowie)
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        ConnectTimeout = TimeSpan.FromSeconds(5),
        // W .NET 8 dostêpne: utrzymanie po³¹czeñ H2 przy d³u¿szej bezczynnoœci
        KeepAlivePingDelay = TimeSpan.FromSeconds(30),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(15),
        KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always
    };

    h.SslOptions = new SslClientAuthenticationOptions
    {
        ClientCertificates = new X509CertificateCollection { clientCert },
        RemoteCertificateValidationCallback = config.TrustServerCerificate
            ? new RemoteCertificateValidationCallback((_, _, _, _) => true)
            : null
    };

    return h;
})

// Polly v8 – gotowy „standard” z dopracowaniem czasów i progów
.AddStandardResilienceHandler(opt =>
{
    // 1) Timeout pojedynczej próby (wa¿ne przy retrach)
    opt.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

    // 2) £¹czny limit czasu ca³ego ¿¹dania (przez wszystkie retry)
    opt.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

    // 3) Retry – exponential + jitter (domyœlnie na 5xx/408/transport; 429 te¿ jest sensowny na odczytach)
    opt.Retry.MaxRetryAttempts = 3;

    // 4) Circuit Breaker – ¿eby nie „mêczyæ” us³ugi gdy ewidentnie ma k³opot
    opt.CircuitBreaker.FailureRatio = 0.2;                // 20% pora¿ek w oknie => przerwa
    opt.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
    opt.CircuitBreaker.MinimumThroughput = 20;            // minimalna liczba prób do oceny
    opt.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});

// ====== DEPENDENCY INJECTION ======
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DutyService>();
builder.Services.AddScoped<SupervisorService>();
builder.Services.AddTransient<ISrpSoapInvoker, SrpSoapInvoker>();
builder.Services.AddTransient<IRdoService, RdoService>();

if (srpConfig!.TestMode)
{
    // Jeœli TestMode, zarejestruj PeselService jako PeselServiceTest
    builder.Services.AddTransient<IPeselService, PeselServiceTest>();
}
else
{
   builder.Services.AddTransient<IPeselService, PeselService>();
}







// ====== CONTROLLERS ======
builder.Services.AddControllers();

// ====== AUTHENTICATION ======
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// ====== SWAGGER ======
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Integration Hub API",
        Version = "v1",
        Description = @"System API dla aplikacji patrolowej ¯andarmerii Wojskowej.

    W bazie danych znajduj¹ siê:

    - **U¿ytkownicy**: 'kpr. Jan Kowalski' (badge: 1111, PIN: 1111), 'mjr Tomasz Nowak' (badge: 2222, PIN: 2222), z przypisanymi rolami takimi jak `User` i `Supervisor`.
    - **PIN-y**: przechowywane jako hashe, logowanie wymaga numeru odznaki i PIN-u.
    - **S³u¿by**: przypisane do u¿ytkownika 1111, typy: 'Patrol pieszy', 'Patrol zapobiegawczy', 'Kontrola ruchu', 'Zabezpieczenie wydarzenia', w dniach od 18 do 21.06.2025 r.
    - **Kody bezpieczeñstwa**: generowane na 10 minut w celu resetu PIN-u.
    - **Role**: `User`, `Supervisor`, `PowerUser`.

    **Autoryzacja**: Wymagany token JWT przesy³any w nag³ówku:
    **Autoryzacja JWT – Jak korzystaæ w Swaggerze:**

    1. Wywo³aj endpoint POST /piesp/auth/login z treœci¹:

       badgeNumber: 1111
       pin: 1111

       OdpowiedŸ zawiera token JWT (w polu ''token'').

    2. Kliknij przycisk Authorize (k³ódka w prawym górnym rogu).
    3. Wklej token w formacie:

       Bearer [wklej_token_tutaj]

    4. Kliknij Authorize, a nastêpnie Close.
    5. Teraz mo¿esz wywo³ywaæ wszystkie zabezpieczone endpointy.

    Token JWT jest wa¿ny 1 dzieñ. Po jego wygaœniêciu zaloguj siê ponownie, aby uzyskaæ nowy token.

    Zalecane testowe dane logowania:

    - Badge: 1111, PIN: 1111 (Dowódca patrolu)
    - Badge: 2222, PIN: 2222 (Oficer dy¿urny)"
    });

   // options.SwaggerDoc("SRP", new OpenApiInfo
   // {
   //     Title = "SRP API",
   //     Version = "v1",
   //     Description = @"SRP API"
   // });

   // Automatycznie przypisuje kontrolery do odpowiednich grup

   //options.DocInclusionPredicate((docName, apiDesc) =>
   //{
   //    var groupName = apiDesc.GroupName ?? "default";
   //    return string.Equals(groupName, docName, StringComparison.OrdinalIgnoreCase);
   //});


    //options.DocInclusionPredicate((groupName, apiDesc) =>
    //{
    //    var declaredGroupName = apiDesc.GroupName;
    //    return string.Equals(groupName, declaredGroupName, StringComparison.OrdinalIgnoreCase);
    //});
    //options.TagActionsBy(api => new[] { api.GroupName });

    //// Dodaj obs³ugê komentarzy XML z projektów IntegrationHub
    //// Domyœlny plik XML z tego projektu
    //var basePath = AppContext.BaseDirectory;
    //var srpXml = Path.Combine(basePath, "IntegrationHub.SRP.xml");
    //var piespXml = Path.Combine(basePath, "IntegrationHub.PIESP.xml");
    //var commonXml = Path.Combine(basePath, "IntegrationHub.Common.xml");

    //foreach (var xml in new[] { srpXml, piespXml, commonXml })
    //{
    //    if (File.Exists(xml))
    //        options.IncludeXmlComments(xml, includeControllerXmlComments: true);
    //}


    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "WprowadŸ JWT w formacie 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    options.EnableAnnotations();// ¿eby dzia³a³ [SwaggerOperation] itd.
    options.ExampleFilters();// ¿eby dzia³a³y [SwaggerResponseExample]
});

// Wskazanie assembly, w którym s¹ klasy przyk³adów wyników dzia³ania metod SRPController:
builder.Services.AddSwaggerExamplesFromAssemblyOf<
    IntegrationHub.Api.Swagger.Examples.SRP.SearchPerson200Example>();

// ====== BUILD ======
var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            

            using var scope = app.Services.CreateScope();
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("SrpServiceClient");

            var rdoUrl = cfg["ExternalServices:SRP:RdoShareServiceUrl"];
            if (string.IsNullOrWhiteSpace(rdoUrl)) return;

            var warmupEnabled = cfg.GetValue<bool?>("ExternalServices:SRP:WarmUpEnabled") ?? false;
            if (!warmupEnabled) return; // <-- wy³¹cza warm-up


            // równoleg³oœæ ~ po³owa MaxConnectionsPerServer (bezpiecznie dla dziesi¹tek userów)
            var maxConns = cfg.GetValue<int?>("ExternalServices:SRP:HttpMaxConnectionsPerServer") ?? 32;
            var parallelism = Math.Clamp(maxConns / 2, 4, 32);
            var attempts = 2; // po 2 lekkie strza³y

            using var sem = new SemaphoreSlim(parallelism);
            var tasks = new List<Task>();

            for (int i = 0; i < attempts; i++)
            {
                await sem.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        foreach (var candidate in new[] { $"{rdoUrl}?wsdl", rdoUrl })
                        {
                            try
                            {
                                using var req = new HttpRequestMessage(HttpMethod.Get, candidate)
                                {
                                    Version = HttpVersion.Version20,
                                    VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
                                };
                                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                                break; // handshake/ALPN/H2 i po³¹czenia w puli s¹ gotowe
                            }
                            catch { /* spróbuj kolejny candidate */ }
                        }
                    }
                    finally { sem.Release(); }
                }));
            }

            await Task.WhenAll(tasks);
        }
        catch { /* warm-up nie mo¿e blokowaæ startu */ }
    });
});



// ====== MIDDLEWARE ======
app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging(); // przed MapControllers
app.MapControllers();

// ====== OPTIONAL: DATABASE SEEDING ======
if (app.Environment.IsDevelopment())
{
    
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PiespDbContext>();
        //It checks if the Users table is empty, and if so, it adds two users with roles.
        if (!context.Users.Any()) // Only seed if no users exist
        {
            context.Database.EnsureCreated(); // Ensure database is created
            DbInitializer.Seed(context);
        }
    }
}
else
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PiespDbContext>();
        context.Database.Migrate(); // Apply migrations in production
    }
}


// ====== RUN APPLICATION ======
app.Run();



