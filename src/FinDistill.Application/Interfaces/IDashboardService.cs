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
}
