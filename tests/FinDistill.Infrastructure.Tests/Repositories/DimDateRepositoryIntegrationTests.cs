using FinDistill.Infrastructure.Repositories;
using FinDistill.Infrastructure.Tests.Fixtures;

namespace FinDistill.Infrastructure.Tests.Repositories;

[Collection("SqlServer")]
public class DimDateRepositoryIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;

    public DimDateRepositoryIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task EnsureDateExistsAsync_NewDate_CreatesWithCorrectFields()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimDateRepository(context);

        var date = new DateOnly(2024, 3, 15);
        var result = await repo.EnsureDateExistsAsync(date, CancellationToken.None);

        Assert.Equal(20240315, result.DateKey);
        Assert.Equal(date, result.FullDate);
        Assert.Equal(2024, result.Year);
        Assert.Equal(1, result.Quarter);
        Assert.Equal(3, result.Month);
        Assert.Equal(15, result.Day);
        Assert.False(result.IsWeekend); // 2024-03-15 is Friday
    }

    [DockerAvailableFact]
    public async Task EnsureDateExistsAsync_Weekend_SetsIsWeekendTrue()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimDateRepository(context);

        var saturday = new DateOnly(2024, 3, 16);
        var result = await repo.EnsureDateExistsAsync(saturday, CancellationToken.None);

        Assert.True(result.IsWeekend);
    }

    [DockerAvailableFact]
    public async Task EnsureDateExistsAsync_CalledTwice_IsIdempotent()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimDateRepository(context);

        var date = new DateOnly(2024, 6, 20);
        var first = await repo.EnsureDateExistsAsync(date, CancellationToken.None);
        var second = await repo.EnsureDateExistsAsync(date, CancellationToken.None);

        Assert.Equal(first.DateKey, second.DateKey);
    }

    [DockerAvailableFact]
    public async Task GetByDateAsync_NonExistent_ReturnsNull()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimDateRepository(context);

        var result = await repo.GetByDateAsync(new DateOnly(1900, 1, 1), CancellationToken.None);

        Assert.Null(result);
    }

    [DockerAvailableFact]
    public async Task GetByDateAsync_AfterEnsure_ReturnsDate()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new DimDateRepository(context);

        var date = new DateOnly(2024, 12, 25);
        await repo.EnsureDateExistsAsync(date, CancellationToken.None);

        var result = await repo.GetByDateAsync(date, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(20241225, result.DateKey);
    }
}
