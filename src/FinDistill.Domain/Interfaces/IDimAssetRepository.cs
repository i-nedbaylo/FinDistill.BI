using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for the DimAssets dimension table.
/// </summary>
public interface IDimAssetRepository
{
    /// <summary>Finds an asset by its ticker symbol, or null if not found.</summary>
    Task<DimAsset?> GetByTickerAsync(string ticker, CancellationToken ct);

    /// <summary>Inserts a new asset or updates an existing one by ticker.</summary>
    Task<DimAsset> UpsertAsync(DimAsset asset, CancellationToken ct);

    /// <summary>Returns all assets with IsActive = true.</summary>
    Task<IReadOnlyList<DimAsset>> GetAllActiveAsync(CancellationToken ct);
}
