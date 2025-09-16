


using Microsoft.Extensions.Logging;

namespace IntegrationHub.Common.Providers
{
    public sealed class SrpHttpLoggingHandler : DelegatingHandler
    {
        private readonly ILogger<SrpHttpLoggingHandler> _logger;
        public SrpHttpLoggingHandler(ILogger<SrpHttpLoggingHandler> logger) => _logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            _logger.LogInformation("SRP >> {Method} {Url} SOAPAction={Action}",
                request.Method, request.RequestUri, request.Headers.TryGetValues("SOAPAction", out var a) ? string.Join(",", a) : "-");

            var response = await base.SendAsync(request, ct);

            _logger.LogInformation("SRP << {Status} {Reason}", (int)response.StatusCode, response.ReasonPhrase);
            return response;
        }
    }

}
