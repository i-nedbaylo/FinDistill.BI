using System.Globalization;
using FinDistill.Domain.Entities;
using FinDistill.Domain.Interfaces;
using FinDistill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinDistill.Infrastructure.Repositories;

public class DimDateRepository : IDimDateRepository
{
    private readonly FinDistillDbContext _context;

    public DimDateRepository(FinDistillDbContext context)
    {
        _context = context;
    }

    public async Task<DimDate?> GetByDateAsync(DateOnly date, CancellationToken ct)
    {
        var dateKey = ToDateKey(date);
        return await _context.DimDates.FindAsync([dateKey], ct);
    }

    public async Task<DimDate> EnsureDateExistsAsync(DateOnly date, CancellationToken ct)
    {
        var dateKey = ToDateKey(date);
        var existing = await _context.DimDates.FindAsync([dateKey], ct);

        if (existing is not null)
            return existing;

        var dt = date.ToDateTime(TimeOnly.MinValue);
        var dimDate = new DimDate
        {
            DateKey = dateKey,
            FullDate = date,
            Year = date.Year,
            Quarter = (byte)((date.Month - 1) / 3 + 1),
            Month = (byte)date.Month,
            Day = (byte)date.Day,
            DayOfWeek = (byte)dt.DayOfWeek,
            WeekOfYear = (byte)ISOWeek.GetWeekOfYear(dt),
            IsWeekend = dt.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday
        };

        _context.DimDates.Add(dimDate);
        await _context.SaveChangesAsync(ct);
        return dimDate;
    }

    private static int ToDateKey(DateOnly date) => date.Year * 10000 + date.Month * 100 + date.Day;
}
