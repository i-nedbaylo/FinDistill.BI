
/// <summary>
/// Read model for risk analytics: Sharpe Ratio and Maximum Drawdown per asset.
/// All metrics are calculated from stored FactQuotes data — no additional API calls required.
/// </summary>
public class RiskMetricsRecord
{
    /// <summary>Asset ticker symbol or coin ID.</summary>
    public string Ticker { get; set; } = string.Empty;
    /// <summary>Human-readable asset name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Asset type string (e.g. "Stock", "Crypto").</summary>
    public string AssetType { get; set; } = string.Empty;
    /// <summary>Annualised Sharpe Ratio (daily returns, risk-free rate assumed 0%).</summary>
    public decimal SharpeRatio { get; set; }
    /// <summary>Maximum Drawdown over the analysis window as a negative percentage (e.g. -23.5).</summary>
    public decimal MaxDrawdown { get; set; }
    /// <summary>Annualised volatility (std dev of daily returns × √252).</summary>
    public decimal AnnualisedVolatility { get; set; }
    /// <summary>Mean daily return over the analysis window.</summary>
    public decimal MeanDailyReturn { get; set; }
    /// <summary>Number of trading days included in the calculation.</summary>
    public int TradingDays { get; set; }
}
