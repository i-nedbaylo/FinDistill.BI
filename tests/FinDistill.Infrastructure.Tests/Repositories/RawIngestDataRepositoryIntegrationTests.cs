using FinDistill.Domain.Entities;
using FinDistill.Infrastructure.Repositories;
using FinDistill.Infrastructure.Tests.Fixtures;

namespace FinDistill.Infrastructure.Tests.Repositories;

[Collection("SqlServer")]
public class RawIngestDataRepositoryIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;

    public RawIngestDataRepositoryIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task AddRangeAsync_And_GetUnprocessedAsync_ReturnsRecords()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new RawIngestDataRepository(context);

        var uniqueSource = $"TestSource_{Guid.NewGuid():N}";

        var records = new List<RawIngestData>
        {
            new()
            {
                Source = uniqueSource,
                Endpoint = "test-endpoint",
                RawContent = """{"test": 1}""",
                LoadedAt = DateTime.UtcNow,
                IsProcessed = false
            },
            new()
            {
                Source = uniqueSource,
                Endpoint = "test-endpoint",
                RawContent = """{"test": 2}""",
                LoadedAt = DateTime.UtcNow,
                IsProcessed = false
            }
        };

        await repo.AddRangeAsync(records, CancellationToken.None);

        var unprocessed = await repo.GetUnprocessedAsync(CancellationToken.None);

        var inserted = unprocessed.Where(r => r.Source == uniqueSource).ToList();
        Assert.Equal(2, inserted.Count);
        Assert.All(inserted, r => Assert.False(r.IsProcessed));
    }

    [DockerAvailableFact]
    public async Task MarkAsProcessedAsync_SetsIsProcessedTrue()
    {
        await using var context = _fixture.CreateDbContext();
        var repo = new RawIngestDataRepository(context);

        var record = new RawIngestData
        {
            Source = $"MarkTest_{Guid.NewGuid():N}",
            Endpoint = "test",
            RawContent = """{"mark": true}""",
            LoadedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        await repo.AddAsync(record, CancellationToken.None);
        Assert.True(record.Id > 0);

        await repo.MarkAsProcessedAsync([record.Id], CancellationToken.None);

        // Reload from a fresh context to verify
        await using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.RawIngestData.FindAsync(record.Id);

        Assert.NotNull(updated);
        Assert.True(updated.IsProcessed);
    }
}
