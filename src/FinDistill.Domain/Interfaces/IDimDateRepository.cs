using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for the DimDates dimension table.
/// </summary>
public interface IDimDateRepository
{
    /// <summary>Ensures a DimDate record exists for the given date, creating it if necessary.</summary>
    Task<DimDate> EnsureDateExistsAsync(DateOnly date, CancellationToken ct);

    /// <summary>Finds a DimDate by its calendar date, or null if not found.</summary>
    Task<DimDate?> GetByDateAsync(DateOnly date, CancellationToken ct);
}
