using System.Collections.Concurrent;
using IntegrationHub.Common.Contracts;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Services;
using System.Net;

namespace IntegrationHub.SRP.Services;
public static class RdoBulkHelpers
{
    /// <summary>
    /// Wykonuje masowo GetCurrentPhotoAsync z ograniczeniem równoległości.
    /// Zwraca wyniki 1:1 z żądaniami, nie rzuca wyjątków – porażki zwraca jako ProxyResponse z błędem.
    /// </summary>
    public static async Task<IReadOnlyList<(GetCurrentPhotoRequest Request, ProxyResponse<GetCurrentPhotoResponse> Result)>>
        BulkGetCurrentPhotosAsync(
            IRdoService rdo,
            IEnumerable<GetCurrentPhotoRequest> requests,
            int maxParallel = 6,
            CancellationToken ct = default)
    {
        var bag = new ConcurrentBag<(GetCurrentPhotoRequest, ProxyResponse<GetCurrentPhotoResponse>)>();

        // .NET 6+: prosty bounded parallelism
        await Parallel.ForEachAsync(requests, new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallel,
            CancellationToken = ct
        },
        async (req, token) =>
        {
            try
            {
                // Osobny requestId per wywołanie (lub przekaż wspólny prefix)
                var res = await rdo.GetCurrentPhotoAsync(req, requestId: Guid.NewGuid().ToString(), token);
                bag.Add((req, res));
            }
            catch (OperationCanceledException)
            {
                bag.Add((req, new ProxyResponse<GetCurrentPhotoResponse>
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Source = "SRP",
                    Status = ProxyStatus.TechnicalError,
                    StatusCode = (int)HttpStatusCode.RequestTimeout,
                    ErrorMessage = "Przerwano (CancellationToken)."
                }));
            }
            catch (Exception ex)
            {
                bag.Add((req, new ProxyResponse<GetCurrentPhotoResponse>
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Source = "SRP",
                    Status = ProxyStatus.TechnicalError,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                }));
            }
        });

        return bag.ToList();
    }
}

