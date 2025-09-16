using IntegrationHub.Common.Config;

namespace IntegrationHub.SRP.Config
{
    /// <summary>
    /// Konfiguracja dostępu do usługi SRP: srp.pesel.wyszukiwanie.
    /// </summary>
    public class SrpConfig: ExternalServiceConfigBase
    { 
        public string PeselSearchServiceUrl { get; set; } = string.Empty;
        public string PeselShareServiceUrl { get; set; } = string.Empty;
        public string RdoShareServiceUrl { get; set; } = string.Empty;
    }
}
