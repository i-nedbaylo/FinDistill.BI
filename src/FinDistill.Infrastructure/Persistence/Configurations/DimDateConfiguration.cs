using FinDistill.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinDistill.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for <see cref="DimDate"/>.
/// Maps to the <c>dwh.DimDates</c> table. DateKey uses <c>ValueGeneratedNever</c> (YYYYMMDD integer PK).
/// </summary>
public class DimDateConfiguration : IEntityTypeConfiguration<DimDate>
{
    public void Configure(EntityTypeBuilder<DimDate> builder)
    {
        builder.ToTable("DimDates", "dwh");

        builder.HasKey(e => e.DateKey);
        builder.Property(e => e.DateKey).ValueGeneratedNever();

        builder.Property(e => e.FullDate).IsRequired();
        builder.Property(e => e.Year).IsRequired();
        builder.Property(e => e.Quarter).IsRequired();
        builder.Property(e => e.Month).IsRequired();
        builder.Property(e => e.Day).IsRequired();
        builder.Property(e => e.DayOfWeek).IsRequired();
        builder.Property(e => e.WeekOfYear).IsRequired();
        builder.Property(e => e.IsWeekend).IsRequired();

        builder.HasIndex(e => e.FullDate).IsUnique()
            .HasDatabaseName("IX_DimDates_FullDate");
    }
}
