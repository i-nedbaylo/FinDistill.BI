using FinDistill.Application.DTOs;
using FinDistill.Domain.Common;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Provides data for the web dashboard by reading from Data Marts.
/// </summary>
public interface IDashboardService
{
    Task<Result<IReadOnlyList<DailyPerformanceDto>>> GetDailyPerformanceAsync(CancellationToken ct);

    Task<Result<IReadOnlyList<AssetHistoryDto>>> GetAssetHistoryAsync(string ticker, int days, CancellationToken ct);

    Task<Result<IReadOnlyList<PortfolioSummaryDto>>> GetPortfolioSummaryAsync(CancellationToken ct);
}
