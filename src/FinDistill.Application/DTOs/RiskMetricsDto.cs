namespace FinDistill.Application.DTOs;

/// <summary>DTO for risk analytics row: Sharpe Ratio + Max Drawdown per asset.</summary>
public class RiskMetricsDto
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal AnnualisedVolatility { get; set; }
    public decimal MeanDailyReturn { get; set; }
    public int TradingDays { get; set; }
}
