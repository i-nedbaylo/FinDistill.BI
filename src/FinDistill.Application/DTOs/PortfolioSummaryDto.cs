namespace FinDistill.Application.DTOs;

/// <summary>
/// Portfolio summary showing last close, previous close, and change for each asset.
/// </summary>
public class PortfolioSummaryDto
{
    /// <summary>Ticker symbol or coin ID.</summary>
    public string Ticker { get; set; } = string.Empty;
    /// <summary>Human-readable asset name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Asset type string (e.g. "Stock", "Crypto").</summary>
    public string AssetType { get; set; } = string.Empty;
    /// <summary>Most recent closing price.</summary>
    public decimal LastClose { get; set; }
    /// <summary>Previous day's closing price.</summary>
    public decimal PreviousClose { get; set; }
    /// <summary>Percentage change between previous and last close.</summary>
    public decimal ChangePercent { get; set; }
}
