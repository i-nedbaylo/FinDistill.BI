namespace FinDistill.Domain.Entities;

/// <summary>
/// Dimension table for tracked financial assets (DWH layer).
/// </summary>
public class DimAsset
{
    /// <summary>Surrogate key.</summary>
    public int AssetKey { get; set; }

    /// <summary>Ticker symbol or coin ID (e.g. "AAPL", "bitcoin").</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Human-readable asset name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stored as string for DB compatibility; maps to <see cref="Enums.AssetType"/>.
    /// </summary>
    public string AssetType { get; set; } = string.Empty;

    /// <summary>Exchange where the asset is traded (nullable for crypto).</summary>
    public string? Exchange { get; set; }

    /// <summary>Indicates whether the asset is currently being tracked.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp when the record was first created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the last update to this record.</summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<FactQuote> FactQuotes { get; set; } = [];
}
