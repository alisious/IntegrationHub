using Microsoft.Extensions.Logging;


namespace IntegrationHub.SRP.Extensions
{
    public static class SrpLogEvents
    {
        public static readonly EventId InvalidBase64Photo = new(21001, nameof(InvalidBase64Photo));
        public static readonly EventId ParseSummary = new(21002, nameof(ParseSummary));
    }
}
