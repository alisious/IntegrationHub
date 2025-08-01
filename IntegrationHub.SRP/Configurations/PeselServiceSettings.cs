namespace IntegrationHub.SRP.Configuration
{
    public class PeselServiceSettings
    {
        public string ClientCertificateThumbprint { get; set; } = string.Empty;
        public WyszukiwanieSettings Wyszukiwanie { get; set; } = new();

        public class WyszukiwanieSettings
        {
            public bool UseTestData { get; set; } = false;
            public bool TrustServerCertificate { get; set; } = true;
            public string EndpointAddress { get; set; } = string.Empty;
            public BindingSettings Binding { get; set; } = new();
            
        }

        public class BindingSettings
        {
            public string OpenTimeout { get; set; } = "00:01:00";
            public string CloseTimeout { get; set; } = "00:01:00";
            public string SendTimeout { get; set; } = "00:02:00";
            public string ReceiveTimeout { get; set; } = "00:02:00";
            public int MaxReceivedMessageSize { get; set; } = 2097152;
            
        }
    }
}
