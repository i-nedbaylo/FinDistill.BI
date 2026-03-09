namespace FinDistill.Domain.Models;

/// <summary>
/// Read model for comparative normalized return data from Data Marts.
/// Each record represents one asset's close price on a specific date,
/// normalized to a base of 100 from the earliest date in the range.
/// </summary>
public class ComparativeReturnRecord
{
    /// <summary>Ticker symbol or coin ID.</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>The trading date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Closing price on this date.</summary>
    public decimal Close { get; set; }

    /// <summary>Normalized return (base = 100 on the first date in the range).</summary>
    public decimal NormalizedReturn { get; set; }
}
