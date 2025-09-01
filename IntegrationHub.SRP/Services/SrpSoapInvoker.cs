using IntegrationHub.Common.Exceptions;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Services
{
    public sealed class SrpSoapInvoker : ISrpSoapInvoker
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SrpSoapInvoker> _logger;

        public SrpSoapInvoker(IHttpClientFactory httpClientFactory, ILogger<SrpSoapInvoker> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<SoapInvokeResult> InvokeAsync(string endpointUrl, string soapAction, string soapEnvelope,
                                                         string requestId, CancellationToken ct = default)
        {
            var client = _httpClientFactory.CreateClient("SrpServiceClient");
            using var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", soapAction);

            _logger.LogInformation("SRP SOAP request start: {Action}. RequestId={RequestId}. Endpoint={Endpoint}",
                                   soapAction, requestId, endpointUrl);

            try
            {
                using var response = await client.PostAsync(endpointUrl, content, ct);
                var xml = await response.Content.ReadAsStringAsync(ct);

                SoapFaultResponse? fault = null;
                try { SoapHelper.TryParseSoapFault(xml, out fault); } catch { /* best-effort */ }

                _logger.LogInformation("SRP SOAP request done: {Action}. RequestId={RequestId}. HTTP={Status}",
                                       soapAction, requestId, (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                    throw new SrpSoapException($"HTTP {(int)response.StatusCode} from SRP",
                        endpointUrl, soapAction, requestId, response.StatusCode);

                return new SoapInvokeResult(response.StatusCode, xml, fault);
            }
            catch (TimeoutException tex)
            {
                _logger.LogError(tex, "Timeout during SOAP call: {Action}. RequestId={RequestId}", soapAction, requestId);
                throw new SrpSoapException("Timeout calling SRP", endpointUrl, soapAction, requestId, null, tex);
            }
            catch (CommunicationException cex)
            {
                _logger.LogError(cex, "Communication error during SOAP call: {Action}. RequestId={RequestId}", soapAction, requestId);
                throw new SrpSoapException("Communication error calling SRP", endpointUrl, soapAction, requestId, null, cex);
            }
        }
    }
}
