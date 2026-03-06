namespace FinDistill.Application.DTOs;

/// <summary>
/// Portfolio summary showing last close, previous close, and change for each asset.
/// </summary>
public class PortfolioSummaryDto
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal LastClose { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal ChangePercent { get; set; }
}
