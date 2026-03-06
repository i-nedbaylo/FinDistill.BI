using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for the DimSources dimension table.
/// </summary>
public interface IDimSourceRepository
{
    Task<DimSource?> GetByNameAsync(string sourceName, CancellationToken ct);

    Task<DimSource> UpsertAsync(DimSource source, CancellationToken ct);
}
