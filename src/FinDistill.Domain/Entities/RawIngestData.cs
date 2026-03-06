namespace FinDistill.Domain.Entities;

/// <summary>
/// Raw data record ingested from an external API source (Data Lake layer).
/// </summary>
public class RawIngestData
{
    public long Id { get; set; }

    /// <summary>
    /// Source identifier, e.g. "YahooFinance", "CoinGecko".
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint or request identifier used for ingestion.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Raw JSON response from the API.
    /// </summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the data was loaded into the lake.
    /// </summary>
    public DateTime LoadedAt { get; set; }

    /// <summary>
    /// Flag indicating whether this record has been processed by the Transformer.
    /// </summary>
    public bool IsProcessed { get; set; }
}
