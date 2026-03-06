using FinDistill.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinDistill.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for Data Lake and DWH tables.
/// Configurations are applied from IEntityTypeConfiguration classes.
/// </summary>
public class FinDistillDbContext : DbContext
{
    public FinDistillDbContext(DbContextOptions<FinDistillDbContext> options)
        : base(options)
    {
    }

    // Data Lake
    public DbSet<RawIngestData> RawIngestData => Set<RawIngestData>();

    // DWH — Dimensions
    public DbSet<DimAsset> DimAssets => Set<DimAsset>();
    public DbSet<DimDate> DimDates => Set<DimDate>();
    public DbSet<DimSource> DimSources => Set<DimSource>();

    // DWH — Facts
    public DbSet<FactQuote> FactQuotes => Set<FactQuote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinDistillDbContext).Assembly);
    }
}
