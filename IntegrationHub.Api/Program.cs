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
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Net;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

// KONFIGURACJA SERILOG – musi byæ przed builder.Build()
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.
// ====== DB CONTEXT ======
builder.Services.AddDbContext<PiespDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//Rejestracja ClientCertificateProvider
builder.Services.AddSingleton<IClientCertificateProvider, ClientCertificateProvider>();
// Rejestracja konfiguracji SRP
builder.Services.Configure<SrpConfig>(builder.Configuration.GetSection("ExternalServices:SRP"));



builder.Services.AddTransient<SrpHttpLoggingHandler>();
// ====== HTTP CLIENTS ======
// Rejestracja klienta SOAP do SRP
builder.Services.AddHttpClient("SrpServiceClient")
    .AddHttpMessageHandler<SrpHttpLoggingHandler>()
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var config = sp.GetRequiredService<IOptions<SrpConfig>>().Value;
        var certProvider = sp.GetRequiredService<IClientCertificateProvider>();

        var handler = new HttpClientHandler();

        if (config.TrustServerCerificate)
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        handler.ClientCertificates.Add(certProvider.GetClientCertificate(config));

        return handler;
    });



// ====== DEPENDENCY INJECTION ======
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DutyService>();
builder.Services.AddScoped<SupervisorService>();
builder.Services.AddTransient<ISrpSoapInvoker, SrpSoapInvoker>();
builder.Services.AddTransient<IRdoService, RdoService>();
builder.Services.AddTransient<IPeselService, PeselService>();






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


});

// ====== BUILD ======
var app = builder.Build();

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



