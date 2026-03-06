using Dapper;
using FinDistill.Domain.Interfaces;
using FinDistill.Domain.Models;
using FinDistill.Infrastructure.Persistence;

namespace FinDistill.Infrastructure.DataMarts;

/// <summary>
/// Reads pre-aggregated data from Data Mart SQL views using Dapper.
/// All queries are parameterized for SQL injection protection.
/// </summary>
public class DapperDataMartReader : IDataMartReader
{
    private readonly DapperConnectionFactory _connectionFactory;

    public DapperDataMartReader(DapperConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
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
        const string sql = """
            SELECT TOP(@Days) Date, [Open], High, Low, [Close], Volume
            FROM mart.v_AssetHistory
            WHERE Ticker = @Ticker
            ORDER BY Date DESC
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
