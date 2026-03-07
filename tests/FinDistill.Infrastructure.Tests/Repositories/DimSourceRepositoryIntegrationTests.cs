using FinDistill.Domain.Entities;
using FinDistill.Infrastructure.Repositories;
using FinDistill.Infrastructure.Tests.Fixtures;

namespace FinDistill.Infrastructure.Tests.Repositories;

[Collection("SqlServer")]
public class DimSourceRepositoryIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;

    public DimSourceRepositoryIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private static string UniqueName(string prefix = "Src") =>
        $"{prefix}{Guid.NewGuid().ToString("N")[..8]}";

    [DockerAvailableFact]
    public async Task UpsertAsync_NewSource_InsertsAndReturnsWithKey()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimSourceRepository(context);

        var source = new DimSource
        {
            SourceName = UniqueName("New"),
            BaseUrl = "https://api.test.com",
            IsActive = true
        };

        var result = await repo.UpsertAsync(source, CancellationToken.None);

        Assert.True(result.SourceKey > 0);
        Assert.Equal(source.SourceName, result.SourceName);
    }

    [DockerAvailableFact]
    public async Task UpsertAsync_ExistingSource_UpdatesFields()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimSourceRepository(context);

        var name = UniqueName("Upd");

        var original = await repo.UpsertAsync(new DimSource
        {
            SourceName = name,
            BaseUrl = "https://original.com",
            IsActive = true
        }, CancellationToken.None);

        var updated = await repo.UpsertAsync(new DimSource
        {
            SourceName = name,
            BaseUrl = "https://updated.com",
            IsActive = false
        }, CancellationToken.None);

        Assert.Equal(original.SourceKey, updated.SourceKey);
        Assert.Equal("https://updated.com", updated.BaseUrl);
        Assert.False(updated.IsActive);
    }

    [DockerAvailableFact]
    public async Task GetByNameAsync_NonExistent_ReturnsNull()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimSourceRepository(context);

        var result = await repo.GetByNameAsync("ZZZNOTEXIST", CancellationToken.None);

        Assert.Null(result);
    }

    [DockerAvailableFact]
    public async Task GetByNameAsync_Existing_ReturnsSource()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimSourceRepository(context);

        var name = UniqueName("Get");
        await repo.UpsertAsync(new DimSource
        {
            SourceName = name,
            BaseUrl = "https://get.test.com",
            IsActive = true
        }, CancellationToken.None);

        var result = await repo.GetByNameAsync(name, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(name, result.SourceName);
    }
}
