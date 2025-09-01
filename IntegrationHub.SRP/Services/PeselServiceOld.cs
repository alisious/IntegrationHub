using IntegrationHub.Common.Services;
using IntegrationHub.SRP.Configuration;
using IntegrationHub.SRP.PESEL.SoapClient;
using IntegrationHub.SRP.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using IntegrationHub.Common.Models;
using IntegrationHub.SRP.Interfaces;
using IntegrationHub.Common.Helpers;

namespace IntegrationHub.SRP.Services
{


    public class PeselServiceOld
    {
        private readonly PeselServiceSettings _peselSoapSettings;
        private readonly IPeselSoapMapper _mapper;
        private readonly IPeselSoapClient _peselSoapClient;
        private readonly ILogger<PeselServiceOld> _logger;


        public PeselServiceOld(IOptions<PeselServiceSettings> peselSoapSettings, IPeselSoapMapper mapper, IPeselSoapClient peselSoapClient, ILogger<PeselServiceOld> logger)
        {
            _peselSoapSettings = peselSoapSettings.Value;
            _mapper = mapper;
            _peselSoapClient = peselSoapClient;
            _logger = logger;
        }

        public async Task<ServiceResponse<BasicPersonDataResponse>> SearchBasePersonalDataByPesel(
            SearchPersonInputModel inputModel,
            X509Certificate2 clientCertificate)
        {



            var soapClientResponse = new ServiceResponse<string>();

            //Mapowanie ¿¹dania do envelope SOAP (XML)
            var peselSoapEnvelopeXml = _mapper.BuildSearchRequestEnvelopeXml(inputModel);
            _logger.LogInformation("Envelope SOAP do wys³ania do PESEL (RequestId: {RequestId}):\n{Envelope}", inputModel.RequestId, peselSoapEnvelopeXml);

            #region Pobieranie danych testowych
            if (_peselSoapSettings.Wyszukiwanie.UseTestData)
            {
                // Zwracamy przyk³adow¹ odpowiedŸ testow¹, jeœli PESEL to 73020916558
                _logger.LogInformation("Zwracanie przyk³adowej odpowiedzi testowej dla PESEL: {Pesel}", inputModel.Pesel);
                try
                {

                    //var data = GetTestBasePersonalDataByPesel(inputModel);
                    //return CreateProxyResponse(data);
                    soapClientResponse = GetTestBaseDataPersonalByPeselXml(inputModel);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "B³¹d pobierania danych testowych.");
                    return new ServiceResponse<BasicPersonDataResponse>
                    {
                        IsSuccess = false,
                        Message = "B³¹d podczas pobierania danych testowych.",
                        ResponseCode = "TEST_DATA_ERROR",
                        Data = null,
                        ProblemDetails = new ProblemDetails
                        {
                            Title = "B³¹d pobierania danych testowych",
                            Detail = ex.Message,
                            Status = 500
                        }
                    };
                }

            }
            else
            {
                //Wys³anie ¿¹dania SOAP do serwisu PESEL
                _logger.LogInformation("Wysy³anie ¿¹dania do serwisu PESEL {endpoint})", _peselSoapSettings.Wyszukiwanie.EndpointAddress);
                soapClientResponse = await _peselSoapClient.SendSoapRequestAsync(peselSoapEnvelopeXml, clientCertificate);

            }
            #endregion

            if (!soapClientResponse.IsSuccess)
            {
                return new ServiceResponse<BasicPersonDataResponse>
                {
                    IsSuccess = false,
                    Message = "B³¹d podczas komunikacji z serwisem PESEL.",
                    ResponseCode = "SOAP_CLIENT_ERROR",
                    Data = null,
                    ProblemDetails = new ProblemDetails
                    {
                        Title = "B³¹d komunikacji z serwisem PESEL",
                        Detail = soapClientResponse.Message,
                        Status = 500
                    }
                };
            }

            //Deserializacja odpowiedzi SOAP
            _logger.LogInformation("Deserializacja odpowiedzi SOAP z serwisu PESEL (RequestId: {RequestId}):\n{Response}", inputModel.RequestId, soapClientResponse);
            var soapResponse = _mapper.DeserializeSoapResponse(soapClientResponse.Data!);
            //Mapowanie odpowiedzi SOAP do modelu DTO
            _logger.LogInformation("Mapowanie odpowiedzi SOAP do modelu DTO (RequestId: {RequestId})", inputModel.RequestId);
            var basicPersonDataResponse = _mapper.MapToDto(soapResponse);

            //Tworzenie odpowiedzi ProxyResponse
            _logger.LogInformation("Tworzenie odpowiedzi ProxyResponse (RequestId: {RequestId})", inputModel.RequestId);
            if (basicPersonDataResponse != null && basicPersonDataResponse.Persons.Any())
            {
                _logger.LogInformation("Znaleziono dane osobowe dla PESEL: {Pesel}", inputModel.Pesel);
                return new ServiceResponse<BasicPersonDataResponse>
                {
                    IsSuccess = true,
                    Message = "Znaleziono dane osobowe na podstawie zadanych kryteriów.",
                    ResponseCode = "PERSON_FOUND",
                    Data = basicPersonDataResponse
                };
            }
            else
            {
                _logger.LogInformation("Brak danych osobowych dla PESEL: {Pesel}", inputModel.Pesel);
                return new ServiceResponse<BasicPersonDataResponse>
                {
                    IsSuccess = false,
                    Message = "Brak danych osobowych dla podanych kryteriów.",
                    ResponseCode = "PERSON_NOT_FOUND",
                    Data = null
                };
            }




            //var endpoint = _peselSoapSettings.Wyszukiwanie.EndpointAddress;
            //var trustServerCertificate = _peselSoapSettings.Wyszukiwanie.TrustServerCertificate;
            //_logger.LogInformation("Otwarcie po³¹czenia do serwisu PESEL: RequestId: {RequestId}, URL: {endpoint}", inputModel.RequestId, endpoint);

            //using var handler = new HttpClientHandler();
            //handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            //handler.ClientCertificates.Add(clientCertificate);
            //if (trustServerCertificate)
            //{
            //    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            //    _logger.LogWarning("Zaufanie do certyfikatu serwera SOAP zosta³o wymuszone (TrustServerCertificate = true)");
            //}

            //using var httpClient = new HttpClient(handler);
            //httpClient.Timeout = TimeSpan.Parse(_peselSoapSettings.Wyszukiwanie.Binding.SendTimeout);

            //// Tworzenie ¿¹dania
            //var soapAction = @"http://msw.gov.pl/srp/v3_0/uslugi/pesel/Wyszukiwanie/wyszukajOsoby/";
            //var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            //{
            //    Content = new StringContent(xmlEnvelope, Encoding.UTF8, "text/xml")
            //};
            //request.Headers.Add("SOAPAction", soapAction);

            //try
            //{
            //    _logger.LogInformation("Wysy³anie ¿¹dania do serwisu PESEL (RequestId: {RequestId}, PESEL: {Pesel})", inputModel.RequestId, inputModel.Pesel);

            //    var response = await httpClient.SendAsync(request);
            //    response.EnsureSuccessStatusCode();

            //    string responseXml = await response.Content.ReadAsStringAsync();

            //    // Parsowanie odpowiedzi i obs³uga SOAP Fault
            //    if (responseXml.Contains("<soap:Fault>"))
            //    {
            //        string? faultMessage = GetSoapFaultString(responseXml);
            //        _logger.LogError("SOAP Fault zwrócony przez serwis PESEL (RequestId: {RequestId}): {Fault}", inputModel.RequestId, faultMessage ?? responseXml);
            //        throw new Exception("SOAP Fault: " + (faultMessage ?? responseXml));
            //    }

            //    // Deserializacja odpowiedzi
            //    var serializer = new XmlSerializer(typeof(wyszukajPodstawoweDaneOsobyResponse), "http://msw.gov.pl/srp/v3_0/uslugi/pesel/");
            //    using var stringReader = new StringReader(responseXml);
            //    using var xmlReader = XmlReader.Create(stringReader);

            //    var result = (wyszukajPodstawoweDaneOsobyResponse?)serializer.Deserialize(xmlReader);

            //    _logger.LogInformation("Pobranie danych z serwisu PESEL zakoñczone sukcesem (RequestId: {RequestId})", inputModel.RequestId);

            //    return CreateProxyResponse(result!);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "B³¹d podczas komunikacji z serwisem PESEL (RequestId: {RequestId})", inputModel.RequestId);
            //    throw;
            //}
        }



        private ServiceResponse<BasicPersonDataResponse> CreateProxyResponse(wyszukajPodstawoweDaneOsobyResponse data)
        {
            if (data.znalezioneOsoby.Length > 0)
            {
                _logger.LogInformation("Znaleziono dane osobowe. ");
                var jsonDto = BasicPersonDataMapper.MapToJsonDto(data);
                return new ServiceResponse<BasicPersonDataResponse>
                {
                    IsSuccess = true,
                    Message = "Znaleziono dane osobowe na podstawie zadanych kryteriów.",
                    ResponseCode = "PERSON_FOUND",
                    Data = jsonDto
                };
            }
            else
            {
                _logger.LogInformation("Brak danych dla podanych kryteriów.");
                return new ServiceResponse<BasicPersonDataResponse>
                {
                    IsSuccess = false,
                    Message = "Brak danych dla podanych kryteriów.",
                    ResponseCode = "PERSON_NOT_FOUND",
                    Data = null
                };
            }
        }

        private string GetSoapEnvelope(SearchPersonInputModel inputModel)
        {
            return
                $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:pes=""http://msw.gov.pl/srp/v3_0/uslugi/pesel/"">
                <soapenv:Header/>
                <soapenv:Body>
                    <pes:wyszukajPodstawoweDaneOsoby>
                        <requestId>{inputModel.RequestId}</requestId>
                        <kryteriaWyszukiwania>
                            <numerPesel>{inputModel.Pesel}</numerPesel>
                        </kryteriaWyszukiwania>
                    </pes:wyszukajPodstawoweDaneOsoby>
                </soapenv:Body>
                </soapenv:Envelope>";

        }




        private string? GetSoapFaultString(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var node = doc.SelectSingleNode("//faultstring");
                return node?.InnerText;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Zwraca przyk³adow¹ odpowiedŸ dla testowego numeru PESEL <b>73020916558</b> na podstawie zserializowanego pliku XML
        /// (IntegrationHub.SRP/TestData/PESEL_73020916558_Response.xml).
        /// Jeœli przekazany PESEL ró¿ni siê od testowego, metoda zwraca pust¹ odpowiedŸ: znalezioneOsoby = new znalezioneOsoby() z pust¹ tablic¹ znalezionaOsoba.
        /// Dziêki temu mo¿esz bezpiecznie testowaæ zachowanie serwisu bez odwo³ania do produkcyjnego SOAP.
        /// </summary>
        /// <param name="inputModel">Model wejœciowy z numerem PESEL.</param>
        /// <returns>
        /// Obiekt <c>wyszukajPodstawoweDaneOsobyResponse</c>:
        /// - z danymi testowymi, jeœli PESEL to 73020916558,
        /// - pusty, jeœli inny PESEL (znalezioneOsoby = new znalezioneOsoby() { znalezionaOsoba = pusta tablica }).
        /// </returns>
        /// <exception cref="FileNotFoundException">Gdy plik testowy nie zostanie odnaleziony dla numeru testowego.</exception>
        /// <exception cref="InvalidOperationException">Gdy plik XML nie zawiera oczekiwanego elementu odpowiedzi SOAP.</exception>
        private wyszukajPodstawoweDaneOsobyResponse GetTestBasePersonalDataByPesel(SearchPersonInputModel inputModel)
        {

            var testDataPath = Path.Combine(AppContext.BaseDirectory, "PeselTestData", $"PESEL_{inputModel.Pesel}_Response.xml");


            if (File.Exists(testDataPath))
            {
                using var fileStream = File.OpenRead(testDataPath);
                var xmlSerializer = new XmlSerializer(typeof(wyszukajPodstawoweDaneOsobyResponse), "http://msw.gov.pl/srp/v3_0/uslugi/pesel/");

                // ZnajdŸ element startowy (Body -> wyszukajPodstawoweDaneOsobyResponse) i zdeserializuj tylko jego zawartoœæ
                using var xmlReader = XmlReader.Create(fileStream);
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "wyszukajPodstawoweDaneOsobyResponse")
                    {
                        return (wyszukajPodstawoweDaneOsobyResponse)xmlSerializer.Deserialize(xmlReader);
                    }
                }
                throw new InvalidOperationException("Nie odnaleziono elementu <wyszukajPodstawoweDaneOsobyResponse> w pliku testowym.");
            }
            else
            {
                // Zwracamy pust¹ odpowiedŸ SOAP, zgodnie z kontraktem WSDL/SOAP
                return new wyszukajPodstawoweDaneOsobyResponse(Array.Empty<PodstawoweDaneOsobyZDanymiPobytu>());

            }
        }

        private ServiceResponse<string> GetTestBaseDataPersonalByPeselXml(SearchPersonInputModel inputModel)
        {
            var testDataPath = Path.Combine(AppContext.BaseDirectory, "PeselTestData", $"PESEL_{inputModel.Pesel}_Response.xml");
            if (File.Exists(testDataPath))
            {
                var responseXml = File.ReadAllText(testDataPath);
                return new ServiceResponse<string>
                {
                    IsSuccess = true,
                    Message = "Zwrócono przyk³adow¹ odpowiedŸ testow¹.",
                    Data = responseXml,
                    ResponseCode = "TEST_DATA_SUCCESS"
                };
            }
            else
            {
                return new ServiceResponse<string>
                {
                    IsSuccess = false,
                    Message = "Brak danych testowych dla podanego PESEL.",
                    ResponseCode = "TEST_DATA_NOT_FOUND",
                    Data = null
                };
            }


        }
    }
}
