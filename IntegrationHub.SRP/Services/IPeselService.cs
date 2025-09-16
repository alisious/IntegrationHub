using IntegrationHub.Common.Contracts;
using IntegrationHub.SRP.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Services
{
    public interface IPeselService
    {
        Task<ProxyResponse<SearchPersonResponse>> SearchPersonAsync(SearchPersonRequest body, string? requestId = null, CancellationToken ct = default);
        Task<ProxyResponse<GetPersonByPeselResponse>> GetPersonByPeselAsync(GetPersonByPeselRequest body, string? requestId = null, CancellationToken ct = default);
    }
}
