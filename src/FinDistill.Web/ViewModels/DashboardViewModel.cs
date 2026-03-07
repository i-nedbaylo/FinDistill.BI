using FinDistill.Application.DTOs;

namespace FinDistill.Web.ViewModels;

/// <summary>
/// View model for the main dashboard page showing portfolio and daily performance.
/// </summary>
public class DashboardViewModel
{
    public IReadOnlyList<DailyPerformanceDto> DailyPerformance { get; set; } = [];
    public IReadOnlyList<PortfolioSummaryDto> PortfolioSummary { get; set; } = [];
}
