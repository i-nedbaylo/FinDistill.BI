namespace FinDistill.Domain.Models;

/// <summary>
/// Read model for daily performance data from Data Marts.
/// </summary>
public class DailyPerformanceRecord
{
    /// <summary>Ticker symbol or coin ID.</summary>
    public string Ticker { get; set; } = string.Empty;
    /// <summary>Human-readable asset name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Asset type string (e.g. "Stock", "Crypto").</summary>
    public string AssetType { get; set; } = string.Empty;
    /// <summary>Latest closing price.</summary>
    public decimal ClosePrice { get; set; }
    /// <summary>Percentage change from the previous day's close.</summary>
    public decimal ChangePercent { get; set; }
}
