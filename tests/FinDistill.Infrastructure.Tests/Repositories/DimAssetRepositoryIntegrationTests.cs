using FinDistill.Domain.Entities;
using FinDistill.Infrastructure.Repositories;
using FinDistill.Infrastructure.Tests.Fixtures;

namespace FinDistill.Infrastructure.Tests.Repositories;

[Collection("SqlServer")]
public class DimAssetRepositoryIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;

    public DimAssetRepositoryIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private static string UniqueTicker(string prefix = "T") =>
        $"{prefix}{Guid.NewGuid().ToString("N")[..8]}".ToUpperInvariant();

    [DockerAvailableFact]
    public async Task UpsertAsync_NewAsset_InsertsAndReturnsWithKey()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimAssetRepository(context);

        var asset = new DimAsset
        {
            Ticker = UniqueTicker("NEW"),
            Name = "Test Asset",
            AssetType = "Stock",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await repo.UpsertAsync(asset, CancellationToken.None);

        Assert.True(result.AssetKey > 0);
        Assert.Equal(asset.Ticker, result.Ticker);
    }

    [DockerAvailableFact]
    public async Task UpsertAsync_ExistingAsset_UpdatesFields()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimAssetRepository(context);

        var ticker = UniqueTicker("UPD");

        var original = await repo.UpsertAsync(new DimAsset
        {
            Ticker = ticker,
            Name = "Original",
            AssetType = "Stock",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, CancellationToken.None);

        var updated = await repo.UpsertAsync(new DimAsset
        {
            Ticker = ticker,
            Name = "Updated",
            AssetType = "ETF",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, CancellationToken.None);

        Assert.Equal(original.AssetKey, updated.AssetKey);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal("ETF", updated.AssetType);
        Assert.False(updated.IsActive);
    }

    [DockerAvailableFact]
    public async Task GetByTickerAsync_NonExistent_ReturnsNull()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimAssetRepository(context);

        var result = await repo.GetByTickerAsync("ZZZNOTEXIST", CancellationToken.None);

        Assert.Null(result);
    }

    [DockerAvailableFact]
    public async Task GetByTickerAsync_Existing_ReturnsAsset()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimAssetRepository(context);

        var ticker = UniqueTicker("GET");
        await repo.UpsertAsync(new DimAsset
        {
            Ticker = ticker,
            Name = "Get Test",
            AssetType = "Crypto",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, CancellationToken.None);

        var result = await repo.GetByTickerAsync(ticker, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ticker, result.Ticker);
    }
}
