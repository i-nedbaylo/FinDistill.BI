namespace FinDistill.Application.DTOs;

/// <summary>
/// Daily performance data for dashboard display.
/// </summary>
public class DailyPerformanceDto
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal ClosePrice { get; set; }
    public decimal ChangePercent { get; set; }
}
