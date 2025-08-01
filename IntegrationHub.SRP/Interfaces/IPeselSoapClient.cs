using IntegrationHub.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Interfaces
{
    public interface IPeselSoapClient
    {
        public Task<ServiceResponse<string>> SendSoapRequestAsync(string envelope, X509Certificate2 cert);
    }
}
