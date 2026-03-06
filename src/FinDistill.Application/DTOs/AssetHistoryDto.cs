namespace FinDistill.Application.DTOs;

/// <summary>
/// Historical OHLCV data for a single date (used for charting).
/// </summary>
public class AssetHistoryDto
{
    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
