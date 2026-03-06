namespace FinDistill.Domain.Entities;

/// <summary>
/// Dimension table for data sources (DWH layer).
/// </summary>
public class DimSource
{
    /// <summary>
    /// Surrogate key.
    /// </summary>
    public int SourceKey { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<FactQuote> FactQuotes { get; set; } = [];
}
