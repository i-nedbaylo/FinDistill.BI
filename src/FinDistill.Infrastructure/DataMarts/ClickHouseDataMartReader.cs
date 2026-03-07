using ClickHouse.Client.ADO;
using ClickHouse.Client.Utility;
using FinDistill.Domain.Interfaces;
using FinDistill.Domain.Models;
using FinDistill.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace FinDistill.Infrastructure.DataMarts;

/// <summary>
/// Reads pre-aggregated data from ClickHouse tables using ClickHouse.Client ADO.NET provider.
/// Implements the same IDataMartReader interface as DapperDataMartReader for seamless swapping.
/// </summary>
public class ClickHouseDataMartReader : IDataMartReader
{
    private readonly string _connectionString;

    public ClickHouseDataMartReader(IOptions<ClickHouseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<IReadOnlyList<DailyPerformanceRecord>> GetDailyPerformanceAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT
                a.Ticker,
                a.Name,
                a.AssetType,
                fq.ClosePrice,
                if(prev.ClosePrice = 0 OR prev.ClosePrice IS NULL, 0,
                   round((fq.ClosePrice - prev.ClosePrice) / prev.ClosePrice * 100, 2)) AS ChangePercent
            FROM dwh.DimAssets a
            INNER JOIN (
                SELECT AssetKey, ClosePrice, DateKey,
                       ROW_NUMBER() OVER (PARTITION BY AssetKey ORDER BY DateKey DESC) AS rn
                FROM dwh.FactQuotes
            ) fq ON fq.AssetKey = a.AssetKey AND fq.rn = 1
            LEFT JOIN (
                SELECT AssetKey, ClosePrice, DateKey,
                       ROW_NUMBER() OVER (PARTITION BY AssetKey ORDER BY DateKey DESC) AS rn
                FROM dwh.FactQuotes
            ) prev ON prev.AssetKey = a.AssetKey AND prev.rn = 2
            WHERE a.IsActive = 1
            ORDER BY a.Ticker
            """;

        var results = new List<DailyPerformanceRecord>();
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync(ct);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new DailyPerformanceRecord
            {
                Ticker = reader.GetString(0),
                Name = reader.GetString(1),
                AssetType = reader.GetString(2),
                ClosePrice = reader.GetDecimal(3),
                ChangePercent = reader.GetDecimal(4)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<AssetHistoryRecord>> GetAssetHistoryAsync(string ticker, int days, CancellationToken ct)
    {
        const string sql = """
            SELECT
                d.FullDate AS Date,
                fq.OpenPrice AS Open,
                fq.HighPrice AS High,
                fq.LowPrice AS Low,
                fq.ClosePrice AS Close,
                fq.Volume
            FROM dwh.FactQuotes fq
            INNER JOIN dwh.DimAssets a ON a.AssetKey = fq.AssetKey
            INNER JOIN dwh.DimDates d ON d.DateKey = fq.DateKey
            WHERE a.Ticker = {ticker:String}
              AND a.IsActive = 1
            ORDER BY d.FullDate DESC
            LIMIT {days:Int32}
            """;

        var results = new List<AssetHistoryRecord>();
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync(ct);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.AddParameter("ticker", ticker);
        cmd.AddParameter("days", days);

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new AssetHistoryRecord
            {
                Date = DateOnly.FromDateTime(reader.GetDateTime(0)),
                Open = reader.GetDecimal(1),
                High = reader.GetDecimal(2),
                Low = reader.GetDecimal(3),
                Close = reader.GetDecimal(4),
                Volume = reader.GetDecimal(5)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<PortfolioSummaryRecord>> GetPortfolioSummaryAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT
                a.Ticker,
                a.Name,
                a.AssetType,
                fq.ClosePrice AS LastClose,
                if(prev.ClosePrice IS NULL, 0, prev.ClosePrice) AS PreviousClose,
                if(prev.ClosePrice = 0 OR prev.ClosePrice IS NULL, 0,
                   round((fq.ClosePrice - prev.ClosePrice) / prev.ClosePrice * 100, 2)) AS ChangePercent
            FROM dwh.DimAssets a
            INNER JOIN (
                SELECT AssetKey, ClosePrice, DateKey,
                       ROW_NUMBER() OVER (PARTITION BY AssetKey ORDER BY DateKey DESC) AS rn
                FROM dwh.FactQuotes
            ) fq ON fq.AssetKey = a.AssetKey AND fq.rn = 1
            LEFT JOIN (
                SELECT AssetKey, ClosePrice, DateKey,
                       ROW_NUMBER() OVER (PARTITION BY AssetKey ORDER BY DateKey DESC) AS rn
                FROM dwh.FactQuotes
            ) prev ON prev.AssetKey = a.AssetKey AND prev.rn = 2
            WHERE a.IsActive = 1
            ORDER BY a.Ticker
            """;

        var results = new List<PortfolioSummaryRecord>();
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync(ct);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new PortfolioSummaryRecord
            {
                Ticker = reader.GetString(0),
                Name = reader.GetString(1),
                AssetType = reader.GetString(2),
                LastClose = reader.GetDecimal(3),
                PreviousClose = reader.GetDecimal(4),
                ChangePercent = reader.GetDecimal(5)
            });
        }

        return results;
    }
}
