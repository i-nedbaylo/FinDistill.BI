namespace FinDistill.Application.DTOs;

/// <summary>
/// 52-week high/low screener data for a single asset.
/// </summary>
public class Week52HighLowDto
{
    /// <summary>Ticker symbol or coin ID.</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Human-readable asset name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Asset type string (e.g. "Stock", "Crypto").</summary>
    public string AssetType { get; set; } = string.Empty;

    /// <summary>Most recent closing price.</summary>
    public decimal LastClose { get; set; }

    /// <summary>Highest closing price in the last 52 weeks.</summary>
    public decimal High52W { get; set; }

    /// <summary>Lowest closing price in the last 52 weeks.</summary>
    public decimal Low52W { get; set; }

    /// <summary>Percentage distance from the 52-week high.</summary>
    public decimal PctFromHigh { get; set; }

    /// <summary>Percentage distance from the 52-week low.</summary>
    public decimal PctFromLow { get; set; }
}
