using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for raw ingested data (Data Lake layer).
/// </summary>
public interface IRawIngestDataRepository
{
    /// <summary>Adds a single raw ingest record to the Data Lake.</summary>
    /// <param name="record">The raw ingest record to be added.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(RawIngestData record, CancellationToken ct);

    /// <summary>Adds multiple raw ingest records to the Data Lake in a single batch.</summary>
    /// <param name="records">The collection of raw ingest records to be added.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddRangeAsync(IEnumerable<RawIngestData> records, CancellationToken ct);

    /// <summary>Returns all records that have not yet been processed by the Transformer.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<RawIngestData>> GetUnprocessedAsync(CancellationToken ct);

    /// <summary>Marks the specified records as processed (IsProcessed = true).</summary>
    /// <param name="ids">Primary key IDs of records to mark.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkAsProcessedAsync(IEnumerable<long> ids, CancellationToken ct);
}
