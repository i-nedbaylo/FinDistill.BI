namespace FinDistill.Domain.Models;

/// <summary>
/// Read model for historical asset data from Data Marts (OHLCV per date).
/// </summary>
public class AssetHistoryRecord
{
    /// <summary>The trading date.</summary>
    public DateOnly Date { get; set; }
    /// <summary>Opening price.</summary>
    public decimal Open { get; set; }
    /// <summary>Highest price of the day.</summary>
    public decimal High { get; set; }
    /// <summary>Lowest price of the day.</summary>
    public decimal Low { get; set; }
    /// <summary>Closing price.</summary>
    public decimal Close { get; set; }
    /// <summary>Trading volume.</summary>
    public decimal Volume { get; set; }
}
