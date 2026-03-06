namespace FinDistill.Domain.Models;

/// <summary>
/// Read model for daily performance data from Data Marts.
/// </summary>
public class DailyPerformanceRecord
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal ClosePrice { get; set; }
    public decimal ChangePercent { get; set; }
}
