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

    /// <summary>
    /// Full calendar date.
    /// </summary>
    public DateOnly FullDate { get; set; }

    /// <summary>
    /// Calendar year (e.g. 2024).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Quarter of the year (1–4).
    /// </summary>
    public byte Quarter { get; set; }

    /// <summary>
    /// Month of the year (1–12).
    /// </summary>
    public byte Month { get; set; }

    /// <summary>
    /// Day of the month (1–31).
    /// </summary>
    public byte Day { get; set; }

    /// <summary>
    /// Day of the week (0 = Sunday … 6 = Saturday).
    /// </summary>
    public byte DayOfWeek { get; set; }

    /// <summary>
    /// ISO 8601 week number of the year (1–53).
    /// </summary>
    public byte WeekOfYear { get; set; }

    /// <summary>
    /// Indicates whether this date falls on a Saturday or Sunday.
    /// </summary>
    public bool IsWeekend { get; set; }

    // Navigation properties
    public ICollection<FactQuote> FactQuotes { get; set; } = [];
}
