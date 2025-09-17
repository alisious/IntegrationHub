using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationHub.Common.Data;

public sealed class HorkosDictionaryService : IHorkosDictionaryService
{
    private readonly string _connString;

    // Odczyt connection stringa o nazwie "IntegrationHubDB"
    public HorkosDictionaryService(Microsoft.Extensions.Configuration.IConfiguration cfg)
    {
        _connString = cfg.GetConnectionString("IntegrationHubDB")
            ?? throw new InvalidOperationException("Brak connection stringa 'IntegrationHubDB' w konfiguracji.");
    }

    public async Task<IReadOnlyList<string>> GetRankReferenceListAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT HORKOS_STOPIEN_NAZWA FROM dbo.HORKOS_STOPIEN;";
        using var conn = new SqlConnection(_connString);
        var rows = await conn.QueryAsync<string>(new CommandDefinition(sql, cancellationToken: ct, commandType: CommandType.Text));
        // Trim + Distinct case-insensitive
        return rows
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetUnitNameReferenceListAsync(CancellationToken ct = default)
    {
        const string sql = @"SELECT HORKOS_NAZWA FROM dbo.HORKOS_JW;";
        using var conn = new SqlConnection(_connString);
        var rows = await conn.QueryAsync<string>(new CommandDefinition(sql, cancellationToken: ct, commandType: CommandType.Text));
        return rows
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
