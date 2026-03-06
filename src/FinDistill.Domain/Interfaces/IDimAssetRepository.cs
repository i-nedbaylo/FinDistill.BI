using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for the DimAssets dimension table.
/// </summary>
public interface IDimAssetRepository
{
    Task<DimAsset?> GetByTickerAsync(string ticker, CancellationToken ct);

    Task<DimAsset> UpsertAsync(DimAsset asset, CancellationToken ct);

    Task<IReadOnlyList<DimAsset>> GetAllActiveAsync(CancellationToken ct);
}
