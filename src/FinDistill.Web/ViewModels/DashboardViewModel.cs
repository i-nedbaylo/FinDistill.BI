using FinDistill.Application.DTOs;

namespace FinDistill.Web.ViewModels;

public class DashboardViewModel
{
    public IReadOnlyList<DailyPerformanceDto> DailyPerformance { get; set; } = [];
    public IReadOnlyList<PortfolioSummaryDto> PortfolioSummary { get; set; } = [];
}
