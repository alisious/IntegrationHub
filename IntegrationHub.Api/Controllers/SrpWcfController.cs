using Azure;
using IntegrationHub.Common.Configs;
using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.Helpers;
using IntegrationHub.Common.Interfaces;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Dowody.SoapClient;
using IntegrationHub.SRP.Helpers;
using IntegrationHub.SRP.Models;
using IntegrationHub.SRP.PESEL.SoapClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;

namespace IntegrationHub.Api.Controllers
{
    [ApiController]
    [Route("SrpWcf")]
    public class SrpWcfController : Controller
    {
        private readonly IClientCertificateProvider _clientCertificateProvider;
        private readonly SrpConfig _srpConfig;
        private readonly ILogger<SrpWcfController> _logger;


        public SrpWcfController(IClientCertificateProvider clientCertificateProvider, IOptions<SrpConfig> srpConfig, ILogger<SrpWcfController> logger)
        {
            _clientCertificateProvider = clientCertificateProvider;
            _srpConfig = srpConfig.Value;
            _logger = logger;

        }


        


            [HttpPost("search-person-base-data")]
        public async Task<ProxyResponse<SearchPersonResponse>> SearchBasePersonData([FromBody] SearchPersonRequest body)
        {
            var requestId = Guid.NewGuid().ToString();

            // Walidacja: PESEL albo (Nazwisko + Imie)
            var hasPesel = !string.IsNullOrWhiteSpace(body.Pesel);
            var hasNamePair = !string.IsNullOrWhiteSpace(body.Nazwisko) && !string.IsNullOrWhiteSpace(body.ImiePierwsze);
            if (!hasPesel && !hasNamePair)
            {
                return new ProxyResponse<SearchPersonResponse>
                {
                    RequestId = requestId,
                    Source = "SRP",
                    Status = ProxyStatus.BusinessError,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    ErrorMessage = "Obowiazkowo podaj PESEL albo zestaw: nazwisko i imie."
                };
            }

            //Walidacja: dataUrodzenia jeśli podana, to w formacie yyyyMMdd
            var hasDataUrodzenia = !string.IsNullOrWhiteSpace(body.DataUrodzenia);
            if (hasDataUrodzenia)
            {
                var formatted = DateStringFormatHelper.FormatYyyyMmDd(body.DataUrodzenia);
                if (formatted == null)
                {
                    return new ProxyResponse<SearchPersonResponse>
                    {
                        RequestId = requestId,
                        Source = "SRP",
                        Status = ProxyStatus.BusinessError,
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        ErrorMessage = "Niepoprawny format parametru dataUrodzenia. Wymagany format: yyyyMMdd lub yyyy-MM-dd."
                    };
                }
                body.DataUrodzenia = formatted;
            }

            var kryteria = SoapHelper.GetCriteriaFromRequest(body);


            var endpointUrl = _srpConfig.PeselSearchServiceUrl;

            try
            {
                // np. pobierasz cert z własnego serwisu/sekretów:
                X509Certificate2 clientCert = _clientCertificateProvider.GetClientCertificate(_srpConfig);

                using var peselClient = SrpPeselClientFactory.Create(
                            endpointUrl: endpointUrl,
                            clientCertificate: clientCert,
                            revocationMode: X509RevocationMode.NoCheck // albo NoCheck w środowisku offline
                );

                _logger.LogInformation("SRP wyszukajPodstawoweDaneOsoby start. RequestId={RequestId}. Kryteria: {Kryteria}",
                               requestId, System.Text.Json.JsonSerializer.Serialize(body));


                
                var response = await peselClient.wyszukajPodstawoweDaneOsobyAsync(requestId, kryteria);

                
                var personDataResponse = BasicPersonSearchResponseMapper.MapFromResponse(response);

                var c = personDataResponse.Persons.Count;

                _logger.LogInformation("SRP wyszukajPodstawoweDaneOsoby success. RequestId={RequestId}. Liczba wyników: {Count}",
                               requestId, c);

                
                //Usunięcie z listy osób nieżyjących
               personDataResponse.Persons.RemoveAll(p => p.CzyZyje.HasValue && !p.CzyZyje.Value);
               
                //Pobranie i dopisanie zdjęć
                var pesele = new List<string>();
                foreach (var person in personDataResponse.Persons)
                    pesele.Add(person.NumerPesel!);   

                _logger.LogInformation("RDO udostepnijAktualneZdjecie start. RequestId={RequestId}. Liczba PESELi: {Count}",
                               requestId, pesele.Count);

                


                return new ProxyResponse<SearchPersonResponse>
                {
                    RequestId = requestId,
                    Data = personDataResponse,
                    Source = "SRP",
                    Status = ProxyStatus.Success,
                    StatusCode = (int)HttpStatusCode.OK
                };

            }
            catch (FaultException fe) // SOAP Fault (jeśli kontrakt/stack go zmaterializuje)
            {
                _logger.LogWarning(fe, "SOAP Fault wyszukajPodstawoweDaneOsoby. RequestId={RequestId}", requestId);
                return new ProxyResponse<SearchPersonResponse>
                {
                    RequestId = requestId,
                    Source = "SRP",
                    Status = ProxyStatus.BusinessError,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    ErrorMessage = fe.Message
                };
            }
            catch (TimeoutException tex)
            {
                _logger.LogError(tex, "Timeout wyszukajPodstawoweDaneOsoby. RequestId={RequestId}", requestId);
                return new ProxyResponse<SearchPersonResponse>
                {
                    RequestId = requestId,
                    Source = "SRP",
                    Status = ProxyStatus.TechnicalError,
                    StatusCode = (int)HttpStatusCode.RequestTimeout,
                    ErrorMessage = "Przekroczono czas oczekiwania na odpowiedz uslugi SRP."
                };
            }
            catch (CommunicationException cex)
            {
                _logger.LogError(cex, "Communication error wyszukajPodstawoweDaneOsoby. RequestId={RequestId}", requestId);
                return new ProxyResponse<SearchPersonResponse>
                {
                    RequestId = requestId,
                    Source = "SRP",
                    Status = ProxyStatus.TechnicalError,
                    StatusCode = (int)HttpStatusCode.BadGateway,
                    ErrorMessage = $"Blad komunikacji z usluga SRP. {cex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error wyszukajPodstawoweDaneOsoby. RequestId={RequestId}", requestId);
                return new ProxyResponse<SearchPersonResponse>
                {
                    RequestId = requestId,
                    Source = "SRP",
                    Status = ProxyStatus.TechnicalError,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }


        }
    }
}
