using IntegrationHub.Common.Models;
using IntegrationHub.SRP.Configuration;
using IntegrationHub.SRP.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Services
{
    public class PeselSoapClient : IPeselSoapClient
    {
        private readonly string _endpoint;
        private readonly bool _trustServerCertificate;
        private readonly ILogger<PeselSoapClient> _logger;
        private int _timeout = 30; // Default timeout in seconds

        public PeselSoapClient(IOptions<PeselServiceSettings> peselServiceSettings,ILogger<PeselSoapClient> logger)
        {
            _endpoint = peselServiceSettings.Value.Wyszukiwanie.EndpointAddress;
            if (!int.TryParse(peselServiceSettings.Value.Wyszukiwanie.Binding.OpenTimeout, out _timeout))             {
                _timeout = 30; // Default to 30 seconds if not set
            }
            _trustServerCertificate = peselServiceSettings.Value.Wyszukiwanie.TrustServerCertificate;
            _logger = logger;
        }

        public async Task<ServiceResponse<string>> SendSoapRequestAsync(string xmlEnvelope, X509Certificate2 cert)
        {
            try
            {
                using var handler = new HttpClientHandler();
                handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                handler.ClientCertificates.Add(cert);
                if (_trustServerCertificate)
                {
                    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    _logger.LogWarning("Zaufanie do certyfikatu serwera SOAP zostało wymuszone (TrustServerCertificate = true)");
                }
                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(30); // Set a reasonable timeout
                var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Content = new StringContent(xmlEnvelope, Encoding.UTF8, "text/xml")
                };
                request.Headers.Add("SOAPAction", @"http://msw.gov.pl/srp/v3_0/uslugi/pesel/Wyszukiwanie/wyszukajOsoby/");

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseXml = await response.Content.ReadAsStringAsync();
                var serviceResponse = new ServiceResponse<string>
                {
                    IsSuccess = true,
                    Message = "Otrzymano poprawną odpowiedź na żadanie.",
                    Data = responseXml,
                    ResponseCode = "Success"
                };
                
                return serviceResponse;
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wysyłania żądania SOAP do {Endpoint}", _endpoint);
                var serviceResponse = new ServiceResponse<string>
                {
                    IsSuccess = false,
                    Message = "Wystąpił błąd podczas wysyłania żądania SOAP.",
                    ResponseCode = "Error",
                    ProblemDetails = new ProblemDetails
                    {
                        Title = $"Błąd podczas komunikacji z serwisem SOAP {_endpoint}",
                        Detail = ex.Message,
                        Status = 500
                    }
                };
                return serviceResponse;

            }
        }
    }
}
