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
            WITH ranked AS (
                SELECT
                    AssetKey,
                    ClosePrice,
                    ROW_NUMBER() OVER (PARTITION BY AssetKey ORDER BY DateKey DESC) AS rn
                FROM dwh.FactQuotes
            ),
            pivoted AS (
                SELECT
                    AssetKey,
                    maxIf(ClosePrice, rn = 1) AS LatestClose,
                    maxIf(ClosePrice, rn = 2) AS PrevClose
                FROM ranked
                WHERE rn <= 2
                GROUP BY AssetKey
            )
            SELECT
                a.Ticker,
                a.Name,
                a.AssetType,
                p.LatestClose AS ClosePrice,
                if(p.PrevClose = 0 OR p.PrevClose IS NULL, 0,
                   round((p.LatestClose - p.PrevClose) / p.PrevClose * 100, 2)) AS ChangePercent
            FROM dwh.DimAssets a
            INNER JOIN pivoted p ON p.AssetKey = a.AssetKey
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
            WITH ranked AS (
                SELECT
                    AssetKey,
                    ClosePrice,
                    ROW_NUMBER() OVER (PARTITION BY AssetKey ORDER BY DateKey DESC) AS rn
                FROM dwh.FactQuotes
            ),
            pivoted AS (
                SELECT
                    AssetKey,
                    maxIf(ClosePrice, rn = 1) AS LatestClose,
                    maxIf(ClosePrice, rn = 2) AS PrevClose
                FROM ranked
                WHERE rn <= 2
                GROUP BY AssetKey
            )
            SELECT
                a.Ticker,
                a.Name,
                a.AssetType,
                p.LatestClose AS LastClose,
                if(p.PrevClose IS NULL, 0, p.PrevClose) AS PreviousClose,
                if(p.PrevClose = 0 OR p.PrevClose IS NULL, 0,
                   round((p.LatestClose - p.PrevClose) / p.PrevClose * 100, 2)) AS ChangePercent
            FROM dwh.DimAssets a
            INNER JOIN pivoted p ON p.AssetKey = a.AssetKey
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
