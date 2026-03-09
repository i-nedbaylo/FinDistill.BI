using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for the FactQuotes fact table.
/// </summary>
public interface IFactQuoteRepository
{
    /// <summary>Inserts a batch of fact quote records into the DWH.</summary>
    /// <param name="quotes">Fact quote entities to insert.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddRangeAsync(IEnumerable<FactQuote> quotes, CancellationToken ct);

    /// <summary>Checks whether a quote already exists for the given composite key.</summary>
    /// <param name="assetKey">DimAsset surrogate key.</param>
    /// <param name="dateKey">DimDate key in YYYYMMDD format.</param>
    /// <param name="sourceKey">DimSource surrogate key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> ExistsAsync(int assetKey, int dateKey, int sourceKey, CancellationToken ct);

    /// <summary>
    /// Returns the set of (AssetKey, DateKey, SourceKey) composite keys that already exist
    /// in the database for the supplied asset/source combination.
    /// Used for bulk existence checks to avoid per-row ExistsAsync round-trips.
    /// </summary>
    /// <param name="assetKey">Filter by asset surrogate key.</param>
    /// <param name="sourceKey">Filter by source surrogate key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<HashSet<(int AssetKey, int DateKey, int SourceKey)>> GetExistingKeysAsync(
        int assetKey, int sourceKey, CancellationToken ct);
}
