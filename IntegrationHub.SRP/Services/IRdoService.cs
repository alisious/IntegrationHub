using IntegrationHub.Common.Contracts;
using IntegrationHub.SRP.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Services
{
    public interface IRdoService
    {
        Task<ProxyResponse<GetCurrentPhotoResponse>> GetCurrentPhotoAsync(GetCurrentPhotoRequest body, string? requestId = null, CancellationToken ct = default);
        Task<ProxyResponse<GetIdCardResponse>> ShareIdCardDataAsync(GetIdCardRequest body, string? requestId = null, CancellationToken ct = default);
    }
}
