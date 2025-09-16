using IntegrationHub.Sources.CEP.Config;           // Twój CEPConfig
using IntegrationHub.Sources.CEP.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using CepSlownikiUdostepnianie;
using IntegrationHub.Common.Interfaces;

namespace IntegrationHub.Sources.CEP.Services;

public interface ICEPSlownikiService
{
    Task<List<SlownikNaglowekDto>> PobierzListeSlownikowAsync(CancellationToken ct = default);
}

public sealed class CEPSlownikiService : ICEPSlownikiService
{
    private readonly CEPConfig _cfg;
    private readonly ILogger<CEPSlownikiService> _logger;
    private readonly IClientCertificateProvider _certProvider;

    public CEPSlownikiService(IOptions<CEPConfig> cfg,
                              IClientCertificateProvider certProvider,
                              ILogger<CEPSlownikiService> logger)
    {
        _cfg = cfg.Value;
        _certProvider = certProvider;
        _logger = logger;
    }

    public async Task<List<SlownikNaglowekDto>> PobierzListeSlownikowAsync(CancellationToken ct = default)
    {
        // 1) Binding dopasowany do Twojego klienta (generated uses BasicHttpBinding)
        //    Ustawiamy HTTPS + certyfikat klienta jeżeli EndpointUrl jest https://
        var useHttps = _cfg.EndpointUrl?.StartsWith("https", StringComparison.OrdinalIgnoreCase) == true;

        var binding = new BasicHttpBinding(useHttps ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None)
        {
            MaxBufferSize = int.MaxValue,
            MaxReceivedMessageSize = int.MaxValue,
            ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
            AllowCookies = true
        };

        if (useHttps)
        {
            // mTLS cert
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
        }

        var address = new EndpointAddress(_cfg.EndpointUrl); // zamiast defaultu z referencji
        using var client = new SlownikiUdostepnianieWSClient(binding, address); // :contentReference[oaicite:2]{index=2}

        // 2) Certyfikat klienta – z Twojego providera
        X509Certificate2 clientCert = _certProvider.GetClientCertificate(_cfg);
        client.ClientCredentials.ClientCertificate.Certificate = clientCert;

        // 3) (DEV/TEST) zaufanie do serwera – per kanał
        client.ChannelFactory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode =
            _cfg.TrustServerCerificate
                ? X509CertificateValidationMode.None
                : X509CertificateValidationMode.ChainTrust;

        try
        {
            // 4) Request: bez filtrów (wszystkie słowniki)
            var reqBody = new PobierzListeSlownikow
            {
                // opcjonalne filtry, jeśli będziesz potrzebował:
                // idSlownika = "...",
                // nazwaSlownika = "...",
                // typSlownika = new KodSlownikowy { kod = "..." },
                // dataSubskrypcji = DateTime.UtcNow, dataSubskrypcjiSpecified = true
            };
            
            var response = await client.PobierzListeSlownikowAsync(reqBody);                   // :contentReference[oaicite:4]{index=4}
            var headers = response?.pobierzListeSlownikowRezultat ?? Array.Empty<NaglowekSlownika>(); // :contentReference[oaicite:5]{index=5}

            // 5) Mapowanie -> DTO
            var list = new List<SlownikNaglowekDto>(headers.Length);
            foreach (var h in headers)
            {
                list.Add(new SlownikNaglowekDto
                {
                    Id = h.id,
                    NazwaSlownika = h.nazwaSlownika,
                    Opis = h.opis,
                    DataAktualizacji = h.dataAktualizacji,
                    DataOd = h.dataOd,
                    DataDo = h.dataDo,
                    RodzajSlownika = h.rodzajSlownika is null ? null : new WartoscSlownikowaDto
                    {
                        Kod = h.rodzajSlownika.kod,
                        WartoscOpisowa = h.rodzajSlownika.wartoscOpisowa,
                        IdentyfikatorSlownika = h.rodzajSlownika.identyfikatorSlownika
                    }
                });
            }

            return list;
        }
        catch (FaultException ex) // fault z serwera CEP (Komunikat[])
        {
            _logger.LogError(ex, "CEP Fault w PobierzListeSlownikow");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd techniczny w PobierzListeSlownikow");
            throw;
        }
        finally
        {
            try { await client.CloseAsync(); } // metoda jest wygenerowana warunkowo, ale w .NET 8 działa Close()
            catch { client.Abort(); }
        }
    }
}
