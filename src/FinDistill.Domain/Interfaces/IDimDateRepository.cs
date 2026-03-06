using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for the DimDates dimension table.
/// </summary>
public interface IDimDateRepository
{
    Task<DimDate> EnsureDateExistsAsync(DateOnly date, CancellationToken ct);

    Task<DimDate?> GetByDateAsync(DateOnly date, CancellationToken ct);
}
