namespace FinDistill.Domain.Models;

/// <summary>
/// Read model for historical asset data from Data Marts (OHLCV per date).
/// </summary>
public class AssetHistoryRecord
{
    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
