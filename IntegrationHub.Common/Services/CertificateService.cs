
using IntegrationHub.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace IntegrationHub.Common.Services
{
    public class CertificateValidationException : Exception
    {
        public CertificateValidationException(string message) : base(message) { }
        public CertificateValidationException(string message, Exception inner) : base(message, inner) { }
    }

    public static class CertificateService
    {
        public static bool CertificateExists(string thumbprint)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            return certs.Count > 0;
        }

        public static X509Certificate2 GetCertificateByThumbprint(string thumbprint)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            if (certs.Count == 0)
                throw new CertificateValidationException($"Certyfikat o thumbprincie {thumbprint} nie został znaleziony.");

            var cert = certs[0];

            // Sprawdź ważność certyfikatu
            var now = DateTime.Now;
            if (cert.NotBefore > now)
                throw new CertificateValidationException($"Certyfikat o thumbprincie {thumbprint} nie jest jeszcze ważny (ważny od {cert.NotBefore}).");
            if (cert.NotAfter < now)
                throw new CertificateValidationException($"Certyfikat o thumbprincie {thumbprint} jest już nieważny (ważny do {cert.NotAfter}).");

            // Sprawdź obecność klucza prywatnego
            try
            {
                if (!cert.HasPrivateKey)
                    throw new CertificateValidationException($"Certyfikat o thumbprincie {thumbprint} nie zawiera klucza prywatnego.");

                // Spróbuj uzyskać klucz prywatny — niektóre certyfikaty mogą rzucać CryptographicException
                var privateKey = cert.GetRSAPrivateKey();
                if (privateKey == null)
                    throw new CertificateValidationException($"Certyfikat o thumbprincie {thumbprint} nie zawiera klucza prywatnego (RSA).");
            }
            catch (CryptographicException ex)
            {
                throw new CertificateValidationException($"Nie można uzyskać klucza prywatnego z certyfikatu o thumbprincie {thumbprint}. Możliwe, że konto aplikacji nie ma dostępu do klucza prywatnego.", ex);
            }

            return cert;
        }

        /// <summary>
        /// Zwraca podstawowe informacje o certyfikacie (lub zwraca błąd w out errorResult).
        /// </summary>
        /// <param name="thumbprint">Thumbprint certyfikatu</param>
        /// <param name="logger">Logger (opcjonalnie)</param>
        /// <param name="problemDetails">Ustawiany na odpowiedź błędu jeśli nie uda się pobrać certyfikatu</param>
        /// <returns>CertificateInfo lub null jeśli wystąpił błąd</returns>
        public static CertificateInfo? GetCertificateInfo(string thumbprint, ILogger? logger, out ProblemDetails? problemDetails)
        {
            problemDetails = null;
            try
            {
                var cert = GetCertificateByThumbprint(thumbprint);

                logger?.LogInformation("Certyfikat poprawnie załadowany: {Subject} ({Thumbprint})", cert.Subject, cert.Thumbprint);

                return new CertificateInfo
                {
                    Subject = cert.Subject,
                    Issuer = cert.Issuer,
                    Thumbprint = cert.Thumbprint,
                    NotBefore = cert.NotBefore,
                    NotAfter = cert.NotAfter,
                    HasPrivateKey = cert.HasPrivateKey
                };
            }
            catch (CertificateValidationException ex)
            {
                logger?.LogWarning("Błąd walidacji certyfikatu: {Message}", ex.Message);

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Błąd walidacji certyfikatu klienta.",
                    Detail = ex.Message,
                    Type = "business_rule_violation"
                };
                                
                return null;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Nieoczekiwany błąd podczas pobierania certyfikatu ({Thumbprint})", thumbprint);

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Błąd pobierania certyfikatu.",
                    Detail = "Wystąpił nieoczekiwany błąd podczas pobierania certyfikatu.",
                    Type = "business_rule_violation"
                };

                return null;
            }
        }
    }
}

