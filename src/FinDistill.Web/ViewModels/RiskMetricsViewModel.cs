using FinDistill.Application.DTOs;

namespace FinDistill.Web.ViewModels;

/// <summary>ViewModel for the Risk Analytics page (Sharpe Ratio + Max Drawdown).</summary>
public class RiskMetricsViewModel
{
    /// <summary>Analysis window in calendar days.</summary>
    public int Days { get; set; } = 365;
    /// <summary>Risk metrics per asset.</summary>
    public IReadOnlyList<RiskMetricsDto> Metrics { get; set; } = [];
}
