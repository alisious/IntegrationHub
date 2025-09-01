using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using IntegrationHub.Common.Services;
using IntegrationHub.SRP.Configuration;
using IntegrationHub.SRP.PESEL.SoapClient;
using Microsoft.Extensions.Logging;

namespace IntegrationHub.SRP.Services
{
    public static class PeselWyszukiwanieClientFactory
    {
        public static PeselWyszukiwanieClient Create(PeselServiceSettings settings, X509Certificate2 clientCert, ILogger logger)
        {
            var endpoint = new EndpointAddress(settings.Wyszukiwanie.EndpointAddress);
            

            var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
            {
                Security = 
                {
                    Mode = BasicHttpSecurityMode.Transport,
                    Transport = new HttpTransportSecurity
                    {
                        ClientCredentialType = HttpClientCredentialType.Certificate
                    }
                },
                OpenTimeout = TimeSpan.Parse(settings.Wyszukiwanie.Binding.OpenTimeout),
                CloseTimeout = TimeSpan.Parse(settings.Wyszukiwanie.Binding.CloseTimeout),
                SendTimeout = TimeSpan.Parse(settings.Wyszukiwanie.Binding.SendTimeout),
                ReceiveTimeout = TimeSpan.Parse(settings.Wyszukiwanie.Binding.ReceiveTimeout),
                MaxReceivedMessageSize = settings.Wyszukiwanie.Binding.MaxReceivedMessageSize
            };
                    

            
            var client = new PeselWyszukiwanieClient(binding, endpoint);
            client.ClientCredentials.ClientCertificate.Certificate = clientCert;
            logger.LogInformation("Utworzenie PeselWyszukiwanieClient: {endpoint} z certyfikatem klienta {clientCertThumbprint}", endpoint, clientCert.Thumbprint);

            if (settings.Wyszukiwanie.TrustServerCertificate)
            {
                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode =
                    X509CertificateValidationMode.None;
                logger.LogInformation("Wymuszenie zaufania do certyfikatu serwera {enpoint}.", endpoint);
            }

            
            return client;
        }

        
    }
}