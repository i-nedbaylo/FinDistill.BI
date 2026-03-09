using FinDistill.Domain.Entities;
using FinDistill.Domain.Interfaces;
using FinDistill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinDistill.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFactQuoteRepository"/>.
/// </summary>
public class FactQuoteRepository : IFactQuoteRepository
{
    private readonly FinDistillDbContext _context;

    public FactQuoteRepository(FinDistillDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<FactQuote> quotes, CancellationToken ct)
    {
        _context.FactQuotes.AddRange(quotes);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(int assetKey, int dateKey, int sourceKey, CancellationToken ct)
    {
        return await _context.FactQuotes
            .AnyAsync(q => q.AssetKey == assetKey && q.DateKey == dateKey && q.SourceKey == sourceKey, ct);
    }

    public async Task<HashSet<(int AssetKey, int DateKey, int SourceKey)>> GetExistingKeysAsync(
        int assetKey, int sourceKey, CancellationToken ct)
    {
        var keys = await _context.FactQuotes
            .Where(q => q.AssetKey == assetKey && q.SourceKey == sourceKey)
            .Select(q => new { q.AssetKey, q.DateKey, q.SourceKey })
            .ToListAsync(ct);

        return keys.Select(k => (k.AssetKey, k.DateKey, k.SourceKey)).ToHashSet();
    }
}
