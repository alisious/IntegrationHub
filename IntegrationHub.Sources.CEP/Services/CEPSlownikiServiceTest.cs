using IntegrationHub.Sources.CEP.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace IntegrationHub.Sources.CEP.Services;

/// <summary>
/// Testowa implementacja CEPSłowników – czyta dane z pliku XML (SOAP Envelope)
/// z katalogu: &lt;ContentRoot&gt;\TestData\CEP\pobierzListeSlownikow_RESPONSE.xml
/// </summary>
public sealed class CEPSlownikiServiceTest : ICEPSlownikiService
{
    private readonly ILogger<CEPSlownikiServiceTest> _logger;
    private readonly string _testDataDir;

    public CEPSlownikiServiceTest(
        ILogger<CEPSlownikiServiceTest> logger,
        IHostEnvironment env)
    {
        _logger = logger;
        // zgodnie ze wzorcem z PeselServiceTest: <contentRoot>\TestData\<DOSTAWCA>
        _testDataDir = Path.Combine(env.ContentRootPath, "TestData", "CEP"); // …\TestData\CEP
    }

    public async Task<List<SlownikNaglowekDto>> PobierzListeSlownikowAsync(CancellationToken ct = default)
    {
        var xmlPath = Path.Combine(_testDataDir, "pobierzListeSlownikow_RESPONSE.xml");
        if (!File.Exists(xmlPath))
            throw new FileNotFoundException($"Brak pliku z danymi testowymi: {xmlPath}");

        _logger.LogInformation("CEP działa w TRYBIE TESTOWYM. Nie używam certyfikatu klienta.");
        _logger.LogInformation("CEPSlownikiServiceTest: wczytuję {Path}", xmlPath);

        await using var fs = File.OpenRead(xmlPath);
        var doc = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

        // SOAP: Envelope -> Body -> pobierzListeSlownikowRezultat -> slownik[*]
        var envelope = doc.Root ?? throw new InvalidOperationException("Brak SOAP Envelope.");
        var body = envelope.Elements().FirstOrDefault(e => e.Name.LocalName == "Body")
                   ?? throw new InvalidOperationException("Brak SOAP Body.");
        var rezultat = body.Elements().FirstOrDefault(e => e.Name.LocalName == "pobierzListeSlownikowRezultat")
                       ?? throw new InvalidOperationException("Brak elementu pobierzListeSlownikowRezultat.");
        var slowniki = rezultat.Elements().Where(e => e.Name.LocalName == "slownik");

        var list = new List<SlownikNaglowekDto>();
        foreach (var s in slowniki)
        {
            var rodzaj = s.Elements().FirstOrDefault(e => e.Name.LocalName == "rodzajSlownika");

            list.Add(new SlownikNaglowekDto
            {
                Id = GetStr(s, "id"),
                NazwaSlownika = GetStr(s, "nazwaSlownika"),
                Opis = GetStr(s, "opis"),
                DataAktualizacji = GetDate(s, "dataAktualizacji"),
                DataOd = GetDate(s, "dataOd"),
                DataDo = GetDate(s, "dataDo"),
                RodzajSlownika = rodzaj is null ? null : new WartoscSlownikowaDto
                {
                    Kod = GetStr(rodzaj, "kod"),
                    WartoscOpisowa = GetStr(rodzaj, "wartoscOpisowa"),
                    IdentyfikatorSlownika = GetStr(rodzaj, "identyfikatorSlownika")
                }
            });
        }

        return list;
    }

    private static string? GetStr(XElement parent, string childLocalName) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == childLocalName)?.Value?.Trim();

    private static DateTime? GetDate(XElement parent, string childLocalName)
    {
        var v = GetStr(parent, childLocalName);
        if (string.IsNullOrWhiteSpace(v)) return null;
        return DateTime.TryParse(v, out var dt) ? dt : null;
    }
}
