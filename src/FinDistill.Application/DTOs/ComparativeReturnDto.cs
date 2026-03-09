namespace FinDistill.Application.DTOs;

/// <summary>
/// Comparative normalized return for a single asset on a specific date.
/// Base = 100 on the earliest date in the range.
/// </summary>
public class ComparativeReturnDto
{
    /// <summary>Ticker symbol or coin ID.</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>The trading date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Closing price on this date.</summary>
    public decimal Close { get; set; }

    /// <summary>Normalized return (base = 100 on the first date).</summary>
    public decimal NormalizedReturn { get; set; }
}
