using FinDistill.Domain.Entities;
using FinDistill.Domain.Interfaces;
using FinDistill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinDistill.Infrastructure.Repositories;

public class DimSourceRepository : IDimSourceRepository
{
    private readonly FinDistillDbContext _context;

    public DimSourceRepository(FinDistillDbContext context)
    {
        _context = context;
    }

    public async Task<DimSource?> GetByNameAsync(string sourceName, CancellationToken ct)
    {
        return await _context.DimSources
            .FirstOrDefaultAsync(s => s.SourceName == sourceName, ct);
    }

    public async Task<DimSource> UpsertAsync(DimSource source, CancellationToken ct)
    {
        var existing = await GetByNameAsync(source.SourceName, ct);

        if (existing is not null)
        {
            existing.BaseUrl = source.BaseUrl;
            existing.IsActive = source.IsActive;
            await _context.SaveChangesAsync(ct);
            return existing;
        }

        _context.DimSources.Add(source);
        await _context.SaveChangesAsync(ct);
        return source;
    }
}
