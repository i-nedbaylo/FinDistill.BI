using FinDistill.Domain.Entities;
using FinDistill.Domain.Interfaces;
using FinDistill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinDistill.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDimAssetRepository"/>.
/// </summary>
public class DimAssetRepository : IDimAssetRepository
{
    private readonly FinDistillDbContext _context;

    public DimAssetRepository(FinDistillDbContext context)
    {
        _context = context;
    }

    public async Task<DimAsset?> GetByTickerAsync(string ticker, CancellationToken ct)
    {
        return await _context.DimAssets
            .FirstOrDefaultAsync(a => a.Ticker == ticker, ct);
    }

    public async Task<DimAsset> UpsertAsync(DimAsset asset, CancellationToken ct)
    {
        var existing = await GetByTickerAsync(asset.Ticker, ct);

        if (existing is not null)
        {
            existing.Name = asset.Name;
            existing.AssetType = asset.AssetType;
            existing.Exchange = asset.Exchange;
            existing.IsActive = asset.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return existing;
        }

        asset.CreatedAt = DateTime.UtcNow;
        asset.UpdatedAt = DateTime.UtcNow;
        _context.DimAssets.Add(asset);
        await _context.SaveChangesAsync(ct);
        return asset;
    }

    public async Task<IReadOnlyList<DimAsset>> GetAllActiveAsync(CancellationToken ct)
    {
        return await _context.DimAssets
            .Where(a => a.IsActive)
            .OrderBy(a => a.Ticker)
            .ToListAsync(ct);
    }
}
