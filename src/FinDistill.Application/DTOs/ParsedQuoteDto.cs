using FinDistill.Domain.Enums;

namespace FinDistill.Application.DTOs;

/// <summary>
/// Parsed and validated quote data ready for loading into DWH.
/// Produced by the Transformer stage of the ETL pipeline.
/// </summary>
public class ParsedQuoteDto
{
    /// <summary>Ticker symbol or coin ID.</summary>
    public string Ticker { get; set; } = string.Empty;
    /// <summary>The trading date of the quote.</summary>
    public DateOnly Date { get; set; }
    /// <summary>Opening price.</summary>
    public decimal Open { get; set; }
    /// <summary>Highest price of the period.</summary>
    public decimal High { get; set; }
    /// <summary>Lowest price of the period.</summary>
    public decimal Low { get; set; }
    /// <summary>Closing price.</summary>
    public decimal Close { get; set; }
    /// <summary>Trading volume.</summary>
    public decimal Volume { get; set; }
    /// <summary>The data source this quote was fetched from.</summary>
    public DataSourceType SourceType { get; set; }
}
