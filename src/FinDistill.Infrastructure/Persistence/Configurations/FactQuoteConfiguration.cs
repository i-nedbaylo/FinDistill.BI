using FinDistill.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinDistill.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for <see cref="FactQuote"/>.
/// Maps to the <c>dwh.FactQuotes</c> table with a composite unique constraint on
/// <c>(AssetKey, DateKey, SourceKey)</c> and Restrict FK behaviour.
/// </summary>
public class FactQuoteConfiguration : IEntityTypeConfiguration<FactQuote>
{
    public void Configure(EntityTypeBuilder<FactQuote> builder)
    {
        builder.ToTable("FactQuotes", "dwh");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Foreign keys
        builder.Property(e => e.AssetKey).IsRequired();
        builder.Property(e => e.DateKey).IsRequired();
        builder.Property(e => e.SourceKey).IsRequired();

        // OHLCV with appropriate precision
        builder.Property(e => e.OpenPrice).HasPrecision(18, 8);
        builder.Property(e => e.HighPrice).HasPrecision(18, 8);
        builder.Property(e => e.LowPrice).HasPrecision(18, 8);
        builder.Property(e => e.ClosePrice).HasPrecision(18, 8);
        builder.Property(e => e.Volume).HasPrecision(18, 4);

        builder.Property(e => e.LoadedAt).IsRequired();

        // Unique constraint: one quote per asset/date/source
        builder.HasIndex(e => new { e.AssetKey, e.DateKey, e.SourceKey })
            .IsUnique()
            .HasDatabaseName("IX_FactQuotes_Asset_Date_Source");

        // Navigation relationships
        builder.HasOne(e => e.Asset)
            .WithMany(a => a.FactQuotes)
            .HasForeignKey(e => e.AssetKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Date)
            .WithMany(d => d.FactQuotes)
            .HasForeignKey(e => e.DateKey)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Source)
            .WithMany(s => s.FactQuotes)
            .HasForeignKey(e => e.SourceKey)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
