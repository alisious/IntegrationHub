using IntegrationHub.Common.Helpers;
using IntegrationHub.Common.Models;
using IntegrationHub.Common.Services;
using IntegrationHub.SRP.Configuration;
using IntegrationHub.SRP.Models;
using IntegrationHub.SRP.PESEL.SoapClient;
using IntegrationHub.SRP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Controllers
{
    /// <summary>
    /// Kontroler do obsługi żądań związanych z PESEL i RDO.
    /// </summary>
    /// <remarks>
    /// Obsługuje wyszukiwanie danych osobowych na podstawie PESEL, pobieranie danych Dowodu Osobistego wybranej osoby oraz testowanie połączenia z usługą PESEL i RDO.
    /// </remarks>
    [ApiController]
    [Route("SRP")]
    [ApiExplorerSettings(GroupName = "SRP")]
    public class SrpController : ControllerBase
    {

        private readonly PeselServiceSettings _peselSoapSettings;
        private readonly PeselService _peselService;
        private readonly ILogger<SrpController> _logger;

        public SrpController(IOptions<PeselServiceSettings> peselSoapSettings, PeselService peselService, ILogger<SrpController> logger)
        {
            _peselSoapSettings = peselSoapSettings.Value;
            _peselService = peselService;
            _logger = logger;
        }

        /// <summary>
        /// Metoda testująca połączenie z endpointem PESEL.
        /// Nie uruchamia żadnej metody - sprawdza dostępność certyfikatu klienta i handshake SSL 
        /// </summary>
        /// <returns></returns>
        [HttpGet("test-pesel-endpoint-ssl-handshake")]
        public async Task<IActionResult> TestPeselEndpointSslHandshake()
        {
            #region Obsługa ładowania certyfikatu klienta

            string thumbprint = _peselSoapSettings.ClientCertificateThumbprint;
            var cert = CertificateControllerHelper.TryGetCertificateWithLogging(thumbprint, _logger, out var problemDetails);

            if ((problemDetails != null) || (cert == null))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ServiceResponse<ProblemDetails>
                {
                    IsSuccess = false,
                    Message = "Błąd certyfikatu klienta",
                    ResponseCode = "CERTIFICATE_ERROR"
                });
            }
            _logger.LogInformation("Certyfikat klienta załadowany pomyślnie: {Thumbprint}", thumbprint);
            #endregion

            #region Sprawdzenie zestawienie połączenia do endpointu
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(cert!);

            // Opcjonalnie — wyłączenie walidacji certyfikatu serwera, jeśli używasz TrustServerCertificate
            if (_peselSoapSettings.Wyszukiwanie.TrustServerCertificate)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);

            try
            {
                // 2. Wysyłamy żądanie GET na endpoint (nie uruchomi SOAP, ale sprawdzi handshake SSL)
                var response = await client.GetAsync(_peselSoapSettings.Wyszukiwanie.EndpointAddress, CancellationToken.None);

                // Komentarz: Jeśli dotarliśmy tutaj, handshake SSL się udał, certyfikat klienta jest poprawny
                var proxyResponse = new ServiceResponse<object>
                {
                    IsSuccess = true,
                    Message = "Test handshake: POWODZENIE – handshake SSL zakończony sukcesem, certyfikat klienta OK.",
                    ResponseCode = "SUCCESS",
                    Data = null // Brak danych do zwrócenia
                };
                _logger.LogInformation("Test handshake: POWODZENIE – handshake SSL zakończony sukcesem, certyfikat klienta OK. StatusCode: {StatusCode}, Reason: {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);

                return Ok($"POWODZENIE – handshake SSL zakończony sukcesem, certyfikat klienta OK. StatusCode: {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                // Komentarz: Handshake SSL NIE POWIÓDŁ SIĘ – najczęściej błąd certyfikatu, łączności lub niepoprawnego endpointu
               var problemDeails = new ProblemDetails
                {
                    Status = 500,
                    Title = "Błąd podczas testowania połączenia HTTPS z endpointem PESEL.",
                    Detail = ex.Message,
                    Type = "https_handshake_error"
                };

                var proxyResponse = new ServiceResponse<ProblemDetails>
                {
                    IsSuccess = false,
                    Message = "NIEPOWODZENIE – błąd podczas testowania połączenia HTTPS z endpointem PESEL.",
                    ResponseCode = "HANDSHAKE_ERROR",
                    Data = problemDeails
                };
                _logger.LogError(ex, "Test handshake: NIEPOWODZENIE – błąd podczas testowania połączenia HTTPS z endpointem PESEL.");
                return StatusCode(500, $"NIEPOWODZENIE – błąd podczas testowania połączenia HTTPS: {ex.Message}");
            }
            #endregion
        }

        /// <summary>
        /// Sprawdza dostępność oraz podstawowe informacje o certyfikacie PESEL.
        /// Zwraca atrybuty certyfikatu jeśli udało się go poprawnie załadować, lub szczegóły błędu w przeciwnym wypadku.
        /// </summary>
        [HttpGet("check-pesel-certificate")]
        public IActionResult CheckPeselCertificate()
        {

            #region Sprawdzenie, czy ustawienia są poprawnie skonfigurowane
            if (_peselSoapSettings == null)
            {
                _logger.LogError("Brak ustawień dla PeselSoapSettings appsettings.json sekcja SoapServices\\Pesel\\ClientCertificateThumbprint. Nie wiadomo który certyfikat pobrać.");
                
                return BadRequest(new ServiceResponse<ProblemDetails>
                {
                    IsSuccess = false,
                    Message = "Błąd certyfikatu klienta",
                    ResponseCode = "CERTIFICATE_ERROR",
                    Data = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Błąd konfiguracji",
                        Detail = "Brak ustawień dla PeselSoapSettings w pliku konfiguracyjnym. Nie wiadomo, który certyfikat pobrać.",
                        Type = "configuration_error"
                    }
                });
            }
            #endregion
            #region Sprawdzenie, pobrania certyfikatu
            string thumbprint = _peselSoapSettings.ClientCertificateThumbprint;
            var certInfo = CertificateService.GetCertificateInfo(thumbprint, _logger, out var problemDetails);

            if ((problemDetails != null) || (certInfo == null))
            { 
                _logger.LogError("Nie udało się pobrać certyfikatu klienta o thumbprincie {Thumbprint}.", thumbprint);
                return StatusCode(StatusCodes.Status403Forbidden, new ServiceResponse<ProblemDetails>
                {
                    IsSuccess = false,
                    Message = "Błąd certyfikatu klienta",
                    ResponseCode = "CERTIFICATE_ERROR",
                    Data = problemDetails
                });
            }
            #endregion
            return Ok(new ServiceResponse<CertificateInfo>
            {
                IsSuccess = true,
                Message = "Certyfikat klienta poprawnie załadowany.",
                ResponseCode = "CERTIFICATE_LOADED",
                Data = certInfo
            });
        }


        /// <summary>
        /// Wyszukuje dane osobowe na podstawie numeru PESEL.
        /// </summary>
        /// <param name="inputModel">
        /// Model wejściowy zawierający kryteria wyszukiwania osoby.
        /// <br/>
        /// <b>Wymagane pola:</b><br/>
        /// • <c>requestId</c> – unikalny identyfikator żądania (ciąg znaków)<br/>
        /// • <c>badgeNumber</c> – numer służbowy użytkownika wykonującego zapytanie<br/>
        /// • <c>unitName</c> – nazwa jednostki organizacyjnej (np. <c>KGŻW</c>)<br/>
        /// • <c>pesel</c> – numer PESEL osoby wyszukiwanej<br/>
        /// Opcjonalnie: <c>czyZyje</c> (bool) – domyślnie true.
        /// </param>
        /// <remarks>
        /// Przykład wywołania:
        /// <code>
        /// POST /SRP/search-person-by-pesel
        /// Content-Type: application/json
        ///
        /// {
        ///   "requestId": "bde65ac3-8eaa-4d93-8217-cda1342fb730",
        ///   "badgeNumber": "123456",
        ///   "unitName": "KGŻW",
        ///   "pesel": "73020916558"
        /// }
        /// 
        /// albo
        /// 
        /// {
        ///   "requestId": "bde65ac3-8eaa-4d93-8217-cda1342fb730",
        ///   "badgeNumber": "123456",
        ///   "unitName": "KGŻW",
        ///   "imiePierwsze": "JACEK",
        ///   "nazwisko": "KORPUSIK"
        /// }
        /// 
        /// 
        /// 
        /// </code>
        /// </remarks>
        /// <response code="200">Operacja wyszukiwania zakończyła się poprawnie. Znaleziono osoby na podstawie zadanych kryteriów.</response>
        /// <response code="400">Błąd walidacji danych wejściowych. Na przykład, brak wymaganych pól w modelu wejściowym.</response>
        /// <response code="401">Błąd autoryzacji. Użytkownik nie ma odpowiednich uprawnień do wykonania tej operacji po stronie Integration Hub API.</response>
        /// <response code="403">Błąd certyfikatu klienta. Brak lub niepoprawny certyfikat klienta.</response>
        /// <response code="404">Nie znaleziono żadnej osoby na podstawie zadanych kryteriów.</response>
        /// <response code="422">Błąd biznesowy z serwisu zdalnego np. znaleziono więcej niż dozwolona liczba osób. Należy podać dodatkowe kryteria wyszukiwania.</response>
        /// <response code="500">Błąd techniczny podczas wywołania metody serwisu zdalnego.</response>
        [HttpPost("search-person-by-pesel")]
        public async Task<IActionResult> SearchPersonByPesel([FromBody] SearchPersonInputModel inputModel)
        {

            #region Sprawdzenie, czy model wejściowy jest poprawny
            if (inputModel == null ||
                string.IsNullOrWhiteSpace(inputModel.RequestId) ||
                string.IsNullOrWhiteSpace(inputModel.BadgeNumber) ||
                string.IsNullOrWhiteSpace(inputModel.UnitName) ||
                (
                    string.IsNullOrWhiteSpace(inputModel.Pesel) && 
                    (string.IsNullOrEmpty(inputModel.ImiePierwsze) || 
                    string.IsNullOrEmpty(inputModel.Nazwisko))
                ))
            {
                return BadRequest(new ServiceResponse<object>
                {
                    IsSuccess = false,
                    Message = "Pola RequestId, BadgeNumber, UnitName oraz Pesel albo ImięPierwsze i Nazwisko są wymagane.",
                    ResponseCode = "VALIDATION_ERROR"
                });
            }
            #endregion

            #region Obsługa ładowania certyfikatu klienta
            
            string thumbprint = _peselSoapSettings.ClientCertificateThumbprint;
            var cert = CertificateControllerHelper.TryGetCertificateWithLogging(thumbprint, _logger, out var problemDetails);

            if ((problemDetails != null)||(cert==null))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ServiceResponse<ProblemDetails>
                {
                    IsSuccess = false,
                    Message = "Błąd certyfikatu klienta",
                    ResponseCode = "CERTIFICATE_ERROR",
                    Data = problemDetails
                });
            }
            _logger.LogInformation("Certyfikat klienta załadowany pomyślnie: {Thumbprint}", thumbprint);
            #endregion


            try
            {
                // Wywołanie serwisu SOAP
                var serviceResponse = await _peselService.SearchBasePersonalDataByPesel(inputModel, cert);

                if (serviceResponse.IsSuccess)
                {
                   return Ok(serviceResponse);
                }
                else
                {
                    // Obsługa błędu biznesowego z serwisu zdalnego
                    if (serviceResponse.ResponseCode == "PERSON_NOT_FOUND")
                    {
                        return NotFound(new ServiceResponse<ProblemDetails>
                        {
                            IsSuccess = false,
                            Message = "Nie znaleziono osoby dla podanego PESEL.",
                            ResponseCode = "PERSON_NOT_FOUND",
                            Data = new ProblemDetails
                            {
                                Status = StatusCodes.Status404NotFound,
                                Title = "Osoba nie znaleziona",
                                Detail = "Brak danych osobowych dla podanego PESEL.",
                                Type = "person_not_found"
                            }
                        });
                    }
                    // Inne błędy biznesowe
                    _logger.LogWarning("Błąd biznesowy podczas wyszukiwania osoby: {Message}", serviceResponse.Message);
                    return UnprocessableEntity(new ServiceResponse<ProblemDetails>
                    {
                        IsSuccess = false,
                        Message = serviceResponse.Message,
                        ResponseCode = serviceResponse.ResponseCode,
                        Data = new ProblemDetails
                        {
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Title = "Błąd biznesowy",
                            Detail = serviceResponse.Message,
                            Type = "business_rule_violation"
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Wystąpił błąd podczas wyszukiwania osoby na podstawie PESEL. {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ServiceResponse<ProblemDetails>
                    {
                        IsSuccess = false,
                        Message = "Wystąpił błąd podczas wyszukiwania osoby na podstawie PESEL.",
                        ResponseCode = "SEARCH_ERROR",
                        Data = new ProblemDetails
                        {
                            Status = StatusCodes.Status500InternalServerError,
                            Title = "Błąd wyszukiwania osoby",
                            Detail = ex.Message,
                            Type = "search_error"
                        }
                    });
                
            }
        }

    }
}
    
