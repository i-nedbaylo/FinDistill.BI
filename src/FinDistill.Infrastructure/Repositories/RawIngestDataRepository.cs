using FinDistill.Domain.Entities;
using FinDistill.Domain.Interfaces;
using FinDistill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinDistill.Infrastructure.Repositories;

public class RawIngestDataRepository : IRawIngestDataRepository
{
    private readonly FinDistillDbContext _context;

    public RawIngestDataRepository(FinDistillDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RawIngestData record, CancellationToken ct)
    {
        _context.RawIngestData.Add(record);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<RawIngestData> records, CancellationToken ct)
    {
        _context.RawIngestData.AddRange(records);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RawIngestData>> GetUnprocessedAsync(CancellationToken ct)
    {
        return await _context.RawIngestData
            .Where(r => !r.IsProcessed)
            .OrderBy(r => r.LoadedAt)
            .ToListAsync(ct);
    }

    public async Task MarkAsProcessedAsync(IEnumerable<long> ids, CancellationToken ct)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return;

        await _context.RawIngestData
            .Where(r => idList.Contains(r.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsProcessed, true), ct);
    }
}
