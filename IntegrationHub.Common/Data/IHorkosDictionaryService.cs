using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationHub.Common.Data;

public interface IHorkosDictionaryService
{
    Task<IReadOnlyList<string>> GetRankReferenceListAsync(CancellationToken ct = default);      // HORKOS_STOPIEN_NAZWA
    Task<IReadOnlyList<string>> GetUnitNameReferenceListAsync(CancellationToken ct = default);  // HORKOS_NAZWA
}
