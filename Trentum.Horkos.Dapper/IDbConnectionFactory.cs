using System.Data;
using Microsoft.Data.SqlClient;

namespace Trentum.Horkos;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connString;
    public SqlConnectionFactory(string connString) => _connString = connString;
    public IDbConnection Create() => new SqlConnection(_connString);
}
