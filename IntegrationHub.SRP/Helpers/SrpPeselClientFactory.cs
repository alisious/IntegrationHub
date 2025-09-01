using IntegrationHub.SRP.PESEL.SoapClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Helpers
{
    public static class SrpPeselClientFactory
    {
        /// <summary>
        /// Tworzy klienta WCF do wskazanego HTTPS endpointu SOAP (mTLS + walidacja łańcucha).
        /// Przykład endpointu:
        /// https://int-c.obywatel.gov.pl:2443/srp/v3_0/uslugi/pesel/Wyszukiwanie/PeselWyszukiwanie/PeselWyszukiwaniePort
        /// </summary>
        public static PeselWyszukiwanieClient Create(
            string endpointUrl,
            X509Certificate2 clientCertificate,
            X509RevocationMode revocationMode = X509RevocationMode.Online,
            bool setDnsIdentityFromUrlHost = false)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
                throw new ArgumentNullException(nameof(endpointUrl));
            if (clientCertificate is null)
                throw new ArgumentNullException(nameof(clientCertificate));
            if (!clientCertificate.HasPrivateKey)
                throw new InvalidOperationException("Certyfikat klienta nie zawiera klucza prywatnego.");

            var endpointUri = new Uri(endpointUrl, UriKind.Absolute);

            var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
            {
                MaxReceivedMessageSize = 10 * 1024 * 1024,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max
            };
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate; // mTLS

            var endpoint = setDnsIdentityFromUrlHost
                ? new EndpointAddress(endpointUri, new DnsEndpointIdentity(endpointUri.Host))
                : new EndpointAddress(endpointUri);

            var client = new PeselWyszukiwanieClient(binding, endpoint);

            client.ClientCredentials.ClientCertificate.Certificate = clientCertificate; // mTLS (klient)
            client.ClientCredentials.ServiceCertificate.SslCertificateAuthentication =
                new X509ServiceCertificateAuthentication
                {
                    CertificateValidationMode = X509CertificateValidationMode.ChainTrust, // masz zaufany root CA
                    RevocationMode = revocationMode
                };

            return client;
        }
    }

}
