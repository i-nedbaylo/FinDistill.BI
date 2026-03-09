using FinDistill.Application.DTOs;
using FinDistill.Domain.Common;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Provides data for the web dashboard by reading from Data Marts.
/// </summary>
public interface IDashboardService
{
    /// <summary>Returns daily close price and change percentage for all active assets.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<DailyPerformanceDto>>> GetDailyPerformanceAsync(CancellationToken ct);

    /// <summary>Returns historical OHLCV data for a single asset over the specified number of days.</summary>
    /// <param name="ticker">Asset ticker or coin ID.</param>
    /// <param name="days">Number of calendar days to look back (clamped to 1–365).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<AssetHistoryDto>>> GetAssetHistoryAsync(string ticker, int days, CancellationToken ct);

    /// <summary>Returns a portfolio summary with last close, previous close, and change for each asset.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<PortfolioSummaryDto>>> GetPortfolioSummaryAsync(CancellationToken ct);

    /// <summary>Returns normalized comparative returns for all active assets over the specified number of days.</summary>
    Task<Result<IReadOnlyList<ComparativeReturnDto>>> GetComparativeReturnsAsync(int days, CancellationToken ct);

    /// <summary>Returns 52-week high/low screener data for all active assets.</summary>
    Task<Result<IReadOnlyList<Week52HighLowDto>>> GetWeek52HighLowAsync(CancellationToken ct);

    /// <summary>
    /// Fetches live cryptocurrency market overview (top coins by market cap).
    /// Data comes directly from CoinGecko /coins/markets — not from DWH.
    /// </summary>
    /// <param name="limit">Maximum number of coins to return (1–250).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<CryptoMarketDto>>> GetCryptoMarketOverviewAsync(int limit, CancellationToken ct);

    /// <summary>
    /// Returns Sharpe Ratio, Max Drawdown and annualised volatility for all active assets.
    /// Calculated from stored FactQuotes — no API call required.
    /// </summary>
    /// <param name="days">Number of calendar days to include in the calculation window.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IReadOnlyList<RiskMetricsDto>>> GetRiskMetricsAsync(int days, CancellationToken ct);
}
