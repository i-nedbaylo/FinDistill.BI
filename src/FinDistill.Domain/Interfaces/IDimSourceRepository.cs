using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for the DimSources dimension table.
/// </summary>
public interface IDimSourceRepository
{
    /// <summary>Finds a data source by its name, or null if not found.</summary>
    Task<DimSource?> GetByNameAsync(string sourceName, CancellationToken ct);

    /// <summary>Inserts a new data source or updates an existing one by name.</summary>
    Task<DimSource> UpsertAsync(DimSource source, CancellationToken ct);
}
