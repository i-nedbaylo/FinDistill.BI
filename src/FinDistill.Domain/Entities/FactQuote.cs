namespace FinDistill.Domain.Entities;

/// <summary>
/// Fact table storing OHLCV quote data (DWH layer).
/// </summary>
public class FactQuote
{
    /// <summary>Surrogate primary key.</summary>
    public long Id { get; set; }

    /// <summary>Foreign key to <see cref="DimAsset"/>.</summary>
    public int AssetKey { get; set; }

    /// <summary>Foreign key to <see cref="DimDate"/> (YYYYMMDD format).</summary>
    public int DateKey { get; set; }

    /// <summary>Foreign key to <see cref="DimSource"/>.</summary>
    public int SourceKey { get; set; }

    /// <summary>Opening price of the period.</summary>
    public decimal OpenPrice { get; set; }

    /// <summary>Highest price of the period.</summary>
    public decimal HighPrice { get; set; }

    /// <summary>Lowest price of the period.</summary>
    public decimal LowPrice { get; set; }

    /// <summary>Closing price of the period.</summary>
    public decimal ClosePrice { get; set; }

    /// <summary>Trading volume for the period.</summary>
    public decimal Volume { get; set; }

    /// <summary>UTC timestamp when this record was loaded into the DWH.</summary>
    public DateTime LoadedAt { get; set; }

    // Navigation properties
    public DimAsset Asset { get; set; } = null!;
    public DimDate Date { get; set; } = null!;
    public DimSource Source { get; set; } = null!;
}
