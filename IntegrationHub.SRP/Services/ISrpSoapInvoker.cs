using IntegrationHub.SRP.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Services
{
    public sealed record SoapInvokeResult(HttpStatusCode StatusCode, string Body, SoapFaultResponse? Fault);

    public interface ISrpSoapInvoker
    {
        Task<SoapInvokeResult> InvokeAsync(string endpointUrl, string soapAction, string soapEnvelope,
                                            string requestId, CancellationToken ct = default);
    }
}
