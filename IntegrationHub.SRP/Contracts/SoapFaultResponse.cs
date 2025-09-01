using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{
    public record SoapFaultResponse(
        string FaultCode,
        string FaultString,
        string? DetailCode,
        string? DetailOpis,
        string? DetailOpisTechniczny
    );
}
