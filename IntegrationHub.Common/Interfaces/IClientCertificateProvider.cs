using IntegrationHub.Common.Configs;
using System.Security.Cryptography.X509Certificates;

namespace IntegrationHub.Common.Interfaces
{
    // This interface defines a contract for providing client certificates
    public interface IClientCertificateProvider
    {
        public X509Certificate2 GetClientCertificate(ExternalServiceConfigBase config);
    }
}
