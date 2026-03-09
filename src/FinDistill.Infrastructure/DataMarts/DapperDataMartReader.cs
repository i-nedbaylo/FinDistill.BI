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

    public async Task<IReadOnlyList<ComparativeReturnRecord>> GetComparativeReturnsAsync(int days, CancellationToken ct)
    {
        var sql = _isPostgreSql
            ? """
              SELECT
                  a."Ticker",
                  d."FullDate" AS "Date",
                  fq."ClosePrice" AS "Close",
                  CASE
                      WHEN FIRST_VALUE(fq."ClosePrice") OVER (PARTITION BY a."AssetKey" ORDER BY d."FullDate") = 0 THEN 0
                      ELSE ROUND(fq."ClosePrice" / FIRST_VALUE(fq."ClosePrice") OVER (PARTITION BY a."AssetKey" ORDER BY d."FullDate") * 100, 2)
                  END AS "NormalizedReturn"
              FROM dwh."FactQuotes" fq
              INNER JOIN dwh."DimAssets" a ON a."AssetKey" = fq."AssetKey"
              INNER JOIN dwh."DimDates" d ON d."DateKey" = fq."DateKey"
              WHERE a."IsActive" = true
                AND d."FullDate" >= CURRENT_DATE - @Days
              ORDER BY a."Ticker", d."FullDate"
              """
            : """
              SELECT
                  a.Ticker,
                  d.FullDate AS [Date],
                  fq.ClosePrice AS [Close],
                  CASE
                      WHEN FIRST_VALUE(fq.ClosePrice) OVER (PARTITION BY a.AssetKey ORDER BY d.FullDate) = 0 THEN 0
                      ELSE ROUND(fq.ClosePrice / FIRST_VALUE(fq.ClosePrice) OVER (PARTITION BY a.AssetKey ORDER BY d.FullDate) * 100, 2)
                  END AS NormalizedReturn
              FROM dwh.FactQuotes fq
              INNER JOIN dwh.DimAssets a ON a.AssetKey = fq.AssetKey
              INNER JOIN dwh.DimDates d ON d.DateKey = fq.DateKey
              WHERE a.IsActive = 1
                AND d.FullDate >= DATEADD(DAY, -@Days, GETDATE())
              ORDER BY a.Ticker, d.FullDate
              """;

        using var connection = _connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<ComparativeReturnRecord>(
            new CommandDefinition(sql, new { Days = days }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<IReadOnlyList<Week52HighLowRecord>> GetWeek52HighLowAsync(CancellationToken ct)
    {
        var sql = _isPostgreSql
            ? """
              WITH latest AS (
                  SELECT DISTINCT ON (a."AssetKey")
                      a."AssetKey",
                      a."Ticker",
                      a."Name",
                      a."AssetType",
                      fq."ClosePrice" AS "LastClose"
                  FROM dwh."DimAssets" a
                  INNER JOIN dwh."FactQuotes" fq ON fq."AssetKey" = a."AssetKey"
                  INNER JOIN dwh."DimDates" d ON d."DateKey" = fq."DateKey"
                  WHERE a."IsActive" = true
                  ORDER BY a."AssetKey", d."FullDate" DESC
              ),
              range52 AS (
                  SELECT
                      fq."AssetKey",
                      MAX(fq."ClosePrice") AS "High52W",
                      MIN(fq."ClosePrice") AS "Low52W"
                  FROM dwh."FactQuotes" fq
                  INNER JOIN dwh."DimDates" d ON d."DateKey" = fq."DateKey"
                  WHERE d."FullDate" >= CURRENT_DATE - 365
                  GROUP BY fq."AssetKey"
              )
              SELECT
                  l."Ticker",
                  l."Name",
                  l."AssetType",
                  l."LastClose",
                  COALESCE(r."High52W", l."LastClose") AS "High52W",
                  COALESCE(r."Low52W", l."LastClose") AS "Low52W",
                  CASE WHEN COALESCE(r."High52W", 0) = 0 THEN 0
                       ELSE ROUND((l."LastClose" - r."High52W") / r."High52W" * 100, 2)
                  END AS "PctFromHigh",
                  CASE WHEN COALESCE(r."Low52W", 0) = 0 THEN 0
                       ELSE ROUND((l."LastClose" - r."Low52W") / r."Low52W" * 100, 2)
                  END AS "PctFromLow"
              FROM latest l
              LEFT JOIN range52 r ON r."AssetKey" = l."AssetKey"
              ORDER BY l."Ticker"
              """
            : """
              WITH latest AS (
                  SELECT
                      a.AssetKey,
                      a.Ticker,
                      a.Name,
                      a.AssetType,
                      fq.ClosePrice AS LastClose,
                      ROW_NUMBER() OVER (PARTITION BY a.AssetKey ORDER BY d.FullDate DESC) AS rn
                  FROM dwh.DimAssets a
                  INNER JOIN dwh.FactQuotes fq ON fq.AssetKey = a.AssetKey
                  INNER JOIN dwh.DimDates d ON d.DateKey = fq.DateKey
                  WHERE a.IsActive = 1
              ),
              range52 AS (
                  SELECT
                      fq.AssetKey,
                      MAX(fq.ClosePrice) AS High52W,
                      MIN(fq.ClosePrice) AS Low52W
                  FROM dwh.FactQuotes fq
                  INNER JOIN dwh.DimDates d ON d.DateKey = fq.DateKey
                  WHERE d.FullDate >= DATEADD(DAY, -365, GETDATE())
                  GROUP BY fq.AssetKey
              )
              SELECT
                  l.Ticker,
                  l.Name,
                  l.AssetType,
                  l.LastClose,
                  COALESCE(r.High52W, l.LastClose) AS High52W,
                  COALESCE(r.Low52W, l.LastClose) AS Low52W,
                  CASE WHEN COALESCE(r.High52W, 0) = 0 THEN 0
                       ELSE ROUND((l.LastClose - r.High52W) / r.High52W * 100, 2)
                  END AS PctFromHigh,
                  CASE WHEN COALESCE(r.Low52W, 0) = 0 THEN 0
                       ELSE ROUND((l.LastClose - r.Low52W) / r.Low52W * 100, 2)
                  END AS PctFromLow
              FROM latest l
              LEFT JOIN range52 r ON r.AssetKey = l.AssetKey
              WHERE l.rn = 1
              ORDER BY l.Ticker
              """;

        using var connection = _connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<Week52HighLowRecord>(
            new CommandDefinition(sql, cancellationToken: ct));
        return results.ToList();
    }
}
