using FinDistill.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinDistill.Infrastructure.Persistence.Configurations;

public class DimSourceConfiguration : IEntityTypeConfiguration<DimSource>
{
    public void Configure(EntityTypeBuilder<DimSource> builder)
    {
        builder.ToTable("DimSources", "dwh");

        builder.HasKey(e => e.SourceKey);
        builder.Property(e => e.SourceKey).UseIdentityColumn();

        builder.Property(e => e.SourceName).HasMaxLength(50).IsRequired();
        builder.Property(e => e.BaseUrl).HasMaxLength(256).IsRequired();
        builder.Property(e => e.IsActive).HasDefaultValue(true);

        builder.HasIndex(e => e.SourceName).IsUnique()
            .HasDatabaseName("IX_DimSources_SourceName");
    }
}
