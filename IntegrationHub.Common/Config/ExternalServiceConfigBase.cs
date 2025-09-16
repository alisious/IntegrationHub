namespace IntegrationHub.Common.Config
{
    /// <summary>
    /// Konfiguracja dostępu do systemu SRP.
    /// </summary>
    public class ExternalServiceConfigBase
    {
        public int? HttpMaxConnectionsPerServer { get; set; } = 16;
        public string ServiceName { get; set; } = default!;
        public string EndpointUrl { get; set; } = default!;
        public string ClientCertificateThumbprint { get; set; } = default!;
        public int TimeoutSeconds { get; set; } = 30;
        public bool TestMode { get; set; } = false;
        public bool TrustServerCerificate { get; set; } = true;
        

    }
}
