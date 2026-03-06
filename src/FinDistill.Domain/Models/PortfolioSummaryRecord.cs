namespace FinDistill.Domain.Models;

/// <summary>
/// Read model for portfolio summary data from Data Marts.
/// </summary>
public class PortfolioSummaryRecord
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal LastClose { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal ChangePercent { get; set; }
}
