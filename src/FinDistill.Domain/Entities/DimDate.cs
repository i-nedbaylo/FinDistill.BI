namespace FinDistill.Domain.Entities;

/// <summary>
/// Date dimension table for the DWH star schema.
/// DateKey uses YYYYMMDD integer format.
/// </summary>
public class DimDate
{
    /// <summary>
    /// Primary key in YYYYMMDD format (e.g. 20240115).
    /// </summary>
    public int DateKey { get; set; }

    public DateOnly FullDate { get; set; }

    public int Year { get; set; }

    public byte Quarter { get; set; }

    public byte Month { get; set; }

    public byte Day { get; set; }

    public byte DayOfWeek { get; set; }

    public byte WeekOfYear { get; set; }

    public bool IsWeekend { get; set; }

    // Navigation properties
    public ICollection<FactQuote> FactQuotes { get; set; } = [];
}
