namespace FinDistill.Domain.Entities;

/// <summary>
/// Dimension table for tracked financial assets (DWH layer).
/// </summary>
public class DimAsset
{
    /// <summary>
    /// Surrogate key.
    /// </summary>
    public int AssetKey { get; set; }

    public string Ticker { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stored as string for DB compatibility; maps to <see cref="Enums.AssetType"/>.
    /// </summary>
    public string AssetType { get; set; } = string.Empty;

    /// <summary>
    /// Exchange where the asset is traded (nullable for crypto).
    /// </summary>
    public string? Exchange { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<FactQuote> FactQuotes { get; set; } = [];
}
