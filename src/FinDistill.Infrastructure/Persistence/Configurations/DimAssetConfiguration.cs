using FinDistill.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinDistill.Infrastructure.Persistence.Configurations;

public class DimAssetConfiguration : IEntityTypeConfiguration<DimAsset>
{
    public void Configure(EntityTypeBuilder<DimAsset> builder)
    {
        builder.ToTable("DimAssets", "dwh");

        builder.HasKey(e => e.AssetKey);
        builder.Property(e => e.AssetKey).ValueGeneratedOnAdd();

        builder.Property(e => e.Ticker).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.AssetType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Exchange).HasMaxLength(50);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.Ticker).IsUnique()
            .HasDatabaseName("IX_DimAssets_Ticker");
    }
}
