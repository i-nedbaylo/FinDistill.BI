using FinDistill.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinDistill.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for <see cref="RawIngestData"/>.
/// Maps to the <c>lake.RawIngestData</c> table with a filtered index on <c>IsProcessed</c>.
/// </summary>
public class RawIngestDataConfiguration : IEntityTypeConfiguration<RawIngestData>
{
    public void Configure(EntityTypeBuilder<RawIngestData> builder)
    {
        builder.ToTable("RawIngestData", "lake");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Source).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Endpoint).HasMaxLength(256).IsRequired();
        builder.Property(e => e.RawContent).IsRequired();
        builder.Property(e => e.LoadedAt).IsRequired();
        builder.Property(e => e.IsProcessed).HasDefaultValue(false);

        builder.HasIndex(e => e.IsProcessed)
            .HasDatabaseName("IX_RawIngestData_Unprocessed");
    }
}
