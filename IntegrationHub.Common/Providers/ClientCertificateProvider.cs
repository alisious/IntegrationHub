using IntegrationHub.Common.Config;
using IntegrationHub.Common.Exceptions;
using IntegrationHub.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;


public class ClientCertificateProvider : IClientCertificateProvider
{
    
    private ILogger<ClientCertificateProvider>? _logger;
    public ClientCertificateProvider(ILogger<ClientCertificateProvider>? logger = null)
    {
        _logger = logger;
    }
    public X509Certificate2 GetClientCertificate(ExternalServiceConfigBase config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var serviceName = config.ServiceName ?? "NieznanyService";

        if (string.IsNullOrWhiteSpace(config.ClientCertificateThumbprint))
            throw new CertificateException($"[{serviceName}] Thumbprint certyfikatu klienta nie może być pusty.");

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var normalized = config.ClientCertificateThumbprint.Replace(" ", "").ToUpperInvariant();

        var cert = store.Certificates
            .OfType<X509Certificate2>()
            .FirstOrDefault(c =>
                c.Thumbprint?.Replace(" ", "").ToUpperInvariant() == normalized);

        if (cert == null)
            throw new CertificateException($"[{serviceName}] Nie znaleziono certyfikatu o thumbprincie '{config.ClientCertificateThumbprint}'.");

        if (!cert.HasPrivateKey)
            throw new CertificateException($"[{serviceName}] Certyfikat '{cert.Subject}' nie ma klucza prywatnego.");

        if (cert.NotAfter < DateTime.UtcNow)
            throw new CertificateException($"[{serviceName}] Certyfikat '{cert.Subject}' jest przeterminowany ({cert.NotAfter}).");

        // Tu logujemy sukces:
        _logger?.LogInformation($"[{serviceName}] Certyfikat klienta załadowany pomyślnie: {cert.Subject}");

        return cert;
    }
}
