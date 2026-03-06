using FinDistill.Domain.Enums;

namespace FinDistill.Application.DTOs;

/// <summary>
/// Parsed and validated quote data ready for loading into DWH.
/// Produced by the Transformer stage of the ETL pipeline.
/// </summary>
public class ParsedQuoteDto
{
    public string Ticker { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public DataSourceType SourceType { get; set; }
}
