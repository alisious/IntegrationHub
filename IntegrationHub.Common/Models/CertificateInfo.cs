
namespace IntegrationHub.Common.Models
{
    public class CertificateInfo
    {
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Thumbprint { get; set; } = string.Empty;
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
        public bool HasPrivateKey { get; set; }
    }

}
