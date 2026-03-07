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
}
