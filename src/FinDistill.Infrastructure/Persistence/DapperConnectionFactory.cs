using System.Data;
using FinDistill.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FinDistill.Infrastructure.Persistence;

/// <summary>
/// Factory for creating IDbConnection instances for Dapper queries.
/// Returns SqlConnection or NpgsqlConnection based on DatabaseOptions.Provider.
/// </summary>
public class DapperConnectionFactory
{
    private readonly string _connectionString;
    private readonly string _provider;

    public DapperConnectionFactory(IConfiguration configuration, IOptions<DatabaseOptions> options)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        _provider = options.Value.Provider is { Length: > 0 } p
            ? p
            : throw new InvalidOperationException(
                "Database provider is not configured. Set 'Database:Provider' to 'SqlServer' or 'PostgreSQL'.");
    }

    public IDbConnection CreateConnection()
    {
        return _provider switch
        {
            "SqlServer" => new SqlConnection(_connectionString),
            "PostgreSQL" => new NpgsqlConnection(_connectionString),
            _ => throw new InvalidOperationException(
                $"Unsupported database provider: '{_provider}'. Use 'SqlServer' or 'PostgreSQL'.")
        };
    }
}
