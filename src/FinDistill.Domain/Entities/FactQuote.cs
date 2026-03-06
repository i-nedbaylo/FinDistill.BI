namespace FinDistill.Domain.Entities;

/// <summary>
/// Fact table storing OHLCV quote data (DWH layer).
/// </summary>
public class FactQuote
{
    public long Id { get; set; }

    // Foreign keys
    public int AssetKey { get; set; }
    public int DateKey { get; set; }
    public int SourceKey { get; set; }

    // OHLCV prices
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Volume { get; set; }

    /// <summary>
    /// UTC timestamp when this record was loaded into the DWH.
    /// </summary>
    public DateTime LoadedAt { get; set; }

    // Navigation properties
    public DimAsset Asset { get; set; } = null!;
    public DimDate Date { get; set; } = null!;
    public DimSource Source { get; set; } = null!;
}
