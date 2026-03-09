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

    public async Task<IReadOnlyList<RiskMetricsRecord>> GetRiskMetricsAsync(int days, CancellationToken ct)
    {
        // Pull raw daily closes per asset within the window, then compute
        // Sharpe Ratio and Max Drawdown in application memory.
        // This avoids complex SQL STDDEV / LAG expressions that differ across dialects,
        // and the dataset (≤ 365 rows × N assets) fits comfortably in memory.
        var sql = _isPostgreSql
            ? """
              SELECT
                  a."Ticker",
                  a."Name",
                  a."AssetType",
                  d."FullDate" AS "Date",
                  fq."ClosePrice" AS "Close"
              FROM dwh."FactQuotes" fq
              INNER JOIN dwh."DimAssets" a  ON a."AssetKey"  = fq."AssetKey"
              INNER JOIN dwh."DimDates"  d  ON d."DateKey"   = fq."DateKey"
              WHERE a."IsActive" = true
                AND d."FullDate" >= CURRENT_DATE - @Days
              ORDER BY a."Ticker", d."FullDate"
              """
            : """
              SELECT
                  a.Ticker,
                  a.Name,
                  a.AssetType,
                  d.FullDate AS [Date],
                  fq.ClosePrice AS [Close]
              FROM dwh.FactQuotes fq
              INNER JOIN dwh.DimAssets a  ON a.AssetKey  = fq.AssetKey
              INNER JOIN dwh.DimDates  d  ON d.DateKey   = fq.DateKey
              WHERE a.IsActive = 1
                AND d.FullDate >= DATEADD(DAY, -@Days, GETDATE())
              ORDER BY a.Ticker, d.FullDate
              """;

        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<(string Ticker, string Name, string AssetType, DateOnly Date, decimal Close)>(
            new CommandDefinition(sql, new { Days = days }, cancellationToken: ct));

        return rows
            .GroupBy(r => r.Ticker)
            .Select(g => CalculateRiskMetrics(g.Key, g.First().Name, g.First().AssetType, g.Select(r => r.Close).ToList()))
            .OrderBy(r => r.Ticker)
            .ToList();
    }

    private static RiskMetricsRecord CalculateRiskMetrics(
        string ticker, string name, string assetType, IList<decimal> closes)
    {
        return CalculateRiskMetricsPublic(ticker, name, assetType, closes);
    }

    internal static RiskMetricsRecord CalculateRiskMetricsPublic(
        string ticker, string name, string assetType, IList<decimal> closes)
    {
        if (closes.Count < 2)
        {
            return new RiskMetricsRecord { Ticker = ticker, Name = name, AssetType = assetType, TradingDays = closes.Count };
        }

        // Daily log returns
        var returns = new List<double>(closes.Count - 1);
        for (var i = 1; i < closes.Count; i++)
        {
            if (closes[i - 1] != 0)
                returns.Add((double)(closes[i] / closes[i - 1]) - 1.0);
        }

        if (returns.Count == 0)
            return new RiskMetricsRecord { Ticker = ticker, Name = name, AssetType = assetType, TradingDays = closes.Count };

        var mean = returns.Average();
        var variance = returns.Sum(r => Math.Pow(r - mean, 2)) / returns.Count;
        var stdDev = Math.Sqrt(variance);

        // Annualise: multiply by √252 (trading days per year)
        var annualisedVol = stdDev * Math.Sqrt(252);
        var annualisedReturn = mean * 252;
        var sharpe = annualisedVol > 0 ? annualisedReturn / annualisedVol : 0;

        // Max Drawdown: largest peak-to-trough decline
        var peak = (double)closes[0];
        var maxDrawdown = 0.0;
        foreach (var close in closes)
        {
            var c = (double)close;
            if (c > peak) peak = c;
            var drawdown = peak > 0 ? (c - peak) / peak : 0;
            if (drawdown < maxDrawdown) maxDrawdown = drawdown;
        }

        return new RiskMetricsRecord
        {
            Ticker = ticker,
            Name = name,
            AssetType = assetType,
            SharpeRatio = Math.Round((decimal)sharpe, 2),
            MaxDrawdown = Math.Round((decimal)(maxDrawdown * 100), 2),
            AnnualisedVolatility = Math.Round((decimal)(annualisedVol * 100), 2),
            MeanDailyReturn = Math.Round((decimal)(mean * 100), 4),
            TradingDays = closes.Count
        };
    }
}
