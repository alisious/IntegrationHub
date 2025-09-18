using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trentum.Horkos;

public interface IHorkosDictionaryService
{
    Task<IReadOnlyList<string>> GetRankReferenceListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUnitNameReferenceListAsync(CancellationToken ct = default);
}
