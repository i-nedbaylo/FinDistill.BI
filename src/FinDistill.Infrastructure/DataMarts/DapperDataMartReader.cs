using Dapper;
using FinDistill.Domain.Interfaces;
using FinDistill.Domain.Models;
using FinDistill.Infrastructure.Configuration;
using FinDistill.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace FinDistill.Infrastructure.DataMarts;

/// <summary>
/// Reads pre-aggregated data from Data Mart SQL views using Dapper.
/// All queries are parameterized for SQL injection protection.
/// SQL dialect adapts based on DatabaseOptions.Provider.
/// </summary>
public class DapperDataMartReader : IDataMartReader
{
    private readonly DapperConnectionFactory _connectionFactory;
    private readonly bool _isPostgreSql;

    public DapperDataMartReader(DapperConnectionFactory connectionFactory, IOptions<DatabaseOptions> options)
    {
        _connectionFactory = connectionFactory;
        _isPostgreSql = options.Value.Provider == "PostgreSQL";
    }

    public async Task<IReadOnlyList<DailyPerformanceRecord>> GetDailyPerformanceAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT Ticker, Name, AssetType, ClosePrice, ChangePercent
            FROM mart.v_DailyPerformance
            ORDER BY Ticker
            """;

        using var connection = _connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<DailyPerformanceRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<IReadOnlyList<AssetHistoryRecord>> GetAssetHistoryAsync(string ticker, int days, CancellationToken ct)
    {
        var sql = _isPostgreSql
            ? """
              SELECT "Date", "Open", "High", "Low", "Close", "Volume"
              FROM mart.v_AssetHistory
              WHERE "Ticker" = @Ticker
              ORDER BY "Date" DESC
              LIMIT @Days
              """
            : """
              SELECT TOP(@Days) [Date], [Open], [High], [Low], [Close], [Volume]
              FROM mart.v_AssetHistory
              WHERE Ticker = @Ticker
              ORDER BY [Date] DESC
              """;

        using var connection = _connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<AssetHistoryRecord>(
            new CommandDefinition(sql, new { Ticker = ticker, Days = days }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<IReadOnlyList<PortfolioSummaryRecord>> GetPortfolioSummaryAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT Ticker, Name, AssetType, LastClose, PreviousClose, ChangePercent
            FROM mart.v_PortfolioSummary
            ORDER BY Ticker
            """;

        using var connection = _connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<PortfolioSummaryRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return results.ToList();
    }
}
