using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Common.Exceptions
{
    public sealed class SrpSoapException : Exception
    {
        public string Endpoint { get; }
        public string Action { get; }
        public string? RequestId { get; }
        public HttpStatusCode? HttpStatus { get; }

        public SrpSoapException(string message, string endpoint, string action,
                                string? requestId = null, HttpStatusCode? httpStatus = null, Exception? inner = null)
            : base(message, inner)
        {
            Endpoint = endpoint;
            Action = action;
            RequestId = requestId;
            HttpStatus = httpStatus;
        }
    }
}
