using FinDistill.Domain.Models;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Reads pre-aggregated data from Data Marts.
/// Default implementation uses Dapper; can be swapped to ClickHouse via configuration.
/// </summary>
public interface IDataMartReader
{
    /// <summary>Reads daily price change performance for all active assets.</summary>
    Task<IReadOnlyList<DailyPerformanceRecord>> GetDailyPerformanceAsync(CancellationToken ct);

    /// <summary>Reads OHLCV history for a specific asset over a number of days.</summary>
    /// <param name="ticker">Asset ticker symbol.</param>
    /// <param name="days">Number of historical days to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<AssetHistoryRecord>> GetAssetHistoryAsync(string ticker, int days, CancellationToken ct);

    /// <summary>Reads portfolio summary with last/previous close and change percentage.</summary>
    Task<IReadOnlyList<PortfolioSummaryRecord>> GetPortfolioSummaryAsync(CancellationToken ct);

    /// <summary>Reads normalized comparative returns for all active assets over a number of days.</summary>
    /// <param name="days">Number of calendar days to look back.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<ComparativeReturnRecord>> GetComparativeReturnsAsync(int days, CancellationToken ct);

    /// <summary>Reads 52-week high/low screener data for all active assets.</summary>
    Task<IReadOnlyList<Week52HighLowRecord>> GetWeek52HighLowAsync(CancellationToken ct);

    /// <summary>
    /// Calculates Sharpe Ratio, Maximum Drawdown and annualised volatility
    /// from stored FactQuotes for all active assets.
    /// </summary>
    /// <param name="days">Number of calendar days to include in the calculation window.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<RiskMetricsRecord>> GetRiskMetricsAsync(int days, CancellationToken ct);
}
