using FinDistill.Domain.Entities;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Repository for the FactQuotes fact table.
/// </summary>
public interface IFactQuoteRepository
{
    Task AddRangeAsync(IEnumerable<FactQuote> quotes, CancellationToken ct);

    Task<bool> ExistsAsync(int assetKey, int dateKey, int sourceKey, CancellationToken ct);
}
