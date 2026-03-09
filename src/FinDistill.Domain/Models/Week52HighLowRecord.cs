namespace FinDistill.Domain.Models;

/// <summary>
/// Read model for 52-week high/low screener data from Data Marts.
/// </summary>
public class Week52HighLowRecord
{
    /// <summary>Ticker symbol or coin ID.</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Human-readable asset name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Asset type string (e.g. "Stock", "Crypto").</summary>
    public string AssetType { get; set; } = string.Empty;

    /// <summary>Most recent closing price.</summary>
    public decimal LastClose { get; set; }

    /// <summary>Highest closing price in the last 52 weeks (252 trading days).</summary>
    public decimal High52W { get; set; }

    /// <summary>Lowest closing price in the last 52 weeks (252 trading days).</summary>
    public decimal Low52W { get; set; }

    /// <summary>Percentage distance from the 52-week high: (LastClose - High52W) / High52W * 100.</summary>
    public decimal PctFromHigh { get; set; }

    /// <summary>Percentage distance from the 52-week low: (LastClose - Low52W) / Low52W * 100.</summary>
    public decimal PctFromLow { get; set; }
}
