using FinDistill.Application.DTOs;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Provides data for the web dashboard by reading from Data Marts.
/// </summary>
public interface IDashboardService
{
    Task<IReadOnlyList<DailyPerformanceDto>> GetDailyPerformanceAsync(CancellationToken ct);

    Task<IReadOnlyList<AssetHistoryDto>> GetAssetHistoryAsync(string ticker, int days, CancellationToken ct);

    Task<IReadOnlyList<PortfolioSummaryDto>> GetPortfolioSummaryAsync(CancellationToken ct);
}
