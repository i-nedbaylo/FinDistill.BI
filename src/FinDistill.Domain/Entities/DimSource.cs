namespace FinDistill.Domain.Entities;

/// <summary>
/// Dimension table for data sources (DWH layer).
/// </summary>
public class DimSource
{
    /// <summary>Surrogate key.</summary>
    public int SourceKey { get; set; }

    /// <summary>Unique source identifier (e.g. "YahooFinance", "CoinGecko").</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Base URL of the external API endpoint.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Indicates whether this source is currently active.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<FactQuote> FactQuotes { get; set; } = [];
}
