using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for raw ingested data (Data Lake layer).
/// </summary>
public interface IRawIngestDataRepository
{
    Task AddAsync(RawIngestData record, CancellationToken ct);

    Task<IReadOnlyList<RawIngestData>> GetUnprocessedAsync(CancellationToken ct);

    Task MarkAsProcessedAsync(IEnumerable<long> ids, CancellationToken ct);
}
