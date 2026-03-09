using FinDistill.Application.DTOs;

namespace FinDistill.Web.ViewModels;

/// <summary>
/// View model for the main dashboard page showing portfolio and daily performance.
/// </summary>
public class DashboardViewModel
{
    /// <summary>Daily close price and change percentage for each active asset.</summary>
    public IReadOnlyList<DailyPerformanceDto> DailyPerformance { get; set; } = [];

    /// <summary>Portfolio summary with last/previous close and change for each asset.</summary>
    public IReadOnlyList<PortfolioSummaryDto> PortfolioSummary { get; set; } = [];
}
