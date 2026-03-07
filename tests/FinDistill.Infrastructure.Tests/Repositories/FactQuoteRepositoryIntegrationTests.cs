using FinDistill.Domain.Entities;
using FinDistill.Infrastructure.Repositories;
using FinDistill.Infrastructure.Tests.Fixtures;

namespace FinDistill.Infrastructure.Tests.Repositories;

[Collection("SqlServer")]
public class FactQuoteRepositoryIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;

    public FactQuoteRepositoryIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task AddRangeAsync_And_ExistsAsync_WorkCorrectly()
    {
        await using var context = _fixture.CreateDbContext();

        // Seed dimension data
        var asset = new DimAsset
        {
            Ticker = $"FQ{Guid.NewGuid().ToString("N")[..8]}".ToUpperInvariant(),
            Name = "FactQuote Test",
            AssetType = "Stock",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.DimAssets.Add(asset);
        await context.SaveChangesAsync();

        var dateRepo = new DimDateRepository(context);
        var dimDate = await dateRepo.EnsureDateExistsAsync(new DateOnly(2024, 7, 10), CancellationToken.None);

        var source = new DimSource
        {
            SourceName = $"Src{Guid.NewGuid().ToString("N")[..8]}",
            BaseUrl = "https://test.com",
            IsActive = true
        };
        context.DimSources.Add(source);
        await context.SaveChangesAsync();

        // Act
        var factRepo = new FactQuoteRepository(context);

        var existsBefore = await factRepo.ExistsAsync(asset.AssetKey, dimDate.DateKey, source.SourceKey, CancellationToken.None);
        Assert.False(existsBefore);

        var quotes = new List<FactQuote>
        {
            new()
            {
                AssetKey = asset.AssetKey,
                DateKey = dimDate.DateKey,
                SourceKey = source.SourceKey,
                OpenPrice = 100m,
                HighPrice = 110m,
                LowPrice = 95m,
                ClosePrice = 105m,
                Volume = 1000000m,
                LoadedAt = DateTime.UtcNow
            }
        };

        await factRepo.AddRangeAsync(quotes, CancellationToken.None);

        var existsAfter = await factRepo.ExistsAsync(asset.AssetKey, dimDate.DateKey, source.SourceKey, CancellationToken.None);
        Assert.True(existsAfter);
    }
}
