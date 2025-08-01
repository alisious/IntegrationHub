using IntegrationHub.Common.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography.X509Certificates;

namespace IntegrationHub.Common.Helpers
{
    /// <summary>
    /// Zawiera metody pomocnicze do obsługi certyfikatów w kontrolerach,
    /// w tym bezpieczne pobieranie certyfikatu wraz z obsługą wyjątków
    /// oraz przygotowaniem odpowiedzi z użyciem ProblemDetails.
    /// </summary>
    public static class CertificateControllerHelper
    {
        /// <summary>
        /// Próbuje pobrać certyfikat X509 z lokalnego magazynu na podstawie podanego thumbprintu.
        /// W przypadku błędu walidacji lub błędu systemowego ustawia odpowiedni wynik akcji
        /// (z użyciem ProblemDetails, kodem 403 i typem business_rule_violation) oraz opcjonalnie loguje zdarzenie.
        /// </summary>
        /// <param name="thumbprint">Thumbprint certyfikatu do pobrania.</param>
        /// <param name="logger">Opcjonalny logger do rejestrowania problemów. Jeśli null, logowanie zostanie pominięte.</param>
        /// <param name="problemDetails">
        /// Zwracany wynik akcji (ObjectResult z ProblemDetails i statusem 403) w przypadku błędu, w przeciwnym razie null.
        /// </param>
        /// <returns>Znaleziony certyfikat X509 lub null, jeśli wystąpił błąd.</returns>
        public static X509Certificate2? TryGetCertificateWithLogging(
            string thumbprint,
            ILogger? logger,
            out ProblemDetails? problemDetails)
        {
            problemDetails = null;
            try
            {
                return CertificateService.GetCertificateByThumbprint(thumbprint);
            }
            catch (CertificateValidationException ex)
            {
                if (logger != null)
                    logger.LogWarning("Błąd walidacji certyfikatu ({Thumbprint}): {Message}", thumbprint, ex.Message);

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
                if (logger != null)
                    logger.LogError(ex, "Wystąpił błąd podczas pobierania certyfikatu ({Thumbprint})", thumbprint);

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
