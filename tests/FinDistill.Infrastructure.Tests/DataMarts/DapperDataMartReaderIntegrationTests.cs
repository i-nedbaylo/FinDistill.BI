using FinDistill.Domain.Entities;
using FinDistill.Infrastructure.Configuration;
using FinDistill.Infrastructure.DataMarts;
using FinDistill.Infrastructure.Persistence;
using FinDistill.Infrastructure.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace FinDistill.Infrastructure.Tests.DataMarts;

[Collection("SqlServer")]
public class DapperDataMartReaderIntegrationTests
{
    private readonly SqlServerContainerFixture _fixture;

    public DapperDataMartReaderIntegrationTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [DockerAvailableFact]
    public async Task GetPortfolioSummaryAsync_WithSeededData_ReturnsRecords()
    {
        await using var context = _fixture.CreateDbContext();
        await SeedTestDataAsync(context);

        var reader = CreateReader();

        var results = await reader.GetPortfolioSummaryAsync(CancellationToken.None);

        Assert.NotEmpty(results);
        var aapl = results.FirstOrDefault(r => r.Ticker == "INTG_AAPL");
        Assert.NotNull(aapl);
        Assert.Equal("Integration AAPL", aapl.Name);
        Assert.True(aapl.LastClose > 0);
    }

    [DockerAvailableFact]
    public async Task GetDailyPerformanceAsync_WithSeededData_ReturnsRecords()
    {
        await using var context = _fixture.CreateDbContext();
        await SeedTestDataAsync(context);

        var reader = CreateReader();

        var results = await reader.GetDailyPerformanceAsync(CancellationToken.None);

        Assert.NotEmpty(results);
        var aapl = results.FirstOrDefault(r => r.Ticker == "INTG_AAPL");
        Assert.NotNull(aapl);
        Assert.True(aapl.ClosePrice > 0);
    }

    [DockerAvailableFact]
    public async Task GetAssetHistoryAsync_WithSeededData_ReturnsRecords()
    {
        await using var context = _fixture.CreateDbContext();
        await SeedTestDataAsync(context);

        var reader = CreateReader();

        var results = await reader.GetAssetHistoryAsync("INTG_AAPL", 30, CancellationToken.None);

        Assert.NotEmpty(results);
        Assert.All(results, r =>
        {
            Assert.True(r.Close > 0);
            Assert.True(r.Volume > 0);
        });
    }

    private DapperDataMartReader CreateReader()
    {
        // Ensure Dapper type handlers are registered
        Dapper.SqlMapper.AddTypeHandler(new FinDistill.Infrastructure.Persistence.DateOnlyTypeHandler());

        var configDict = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _fixture.ConnectionString,
            ["Database:Provider"] = "SqlServer"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var dbOptions = Options.Create(new DatabaseOptions { Provider = "SqlServer" });
        var factory = new DapperConnectionFactory(configuration, dbOptions);
        return new DapperDataMartReader(factory, dbOptions);
    }

    private static async Task SeedTestDataAsync(FinDistillDbContext context)
    {
        // Ensure asset exists (idempotent)
        var asset = await context.DimAssets.SingleOrDefaultAsync(a => a.Ticker == "INTG_AAPL");
        if (asset is null)
        {
            asset = new DimAsset
            {
                Ticker = "INTG_AAPL",
                Name = "Integration AAPL",
                AssetType = "Stock",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.DimAssets.Add(asset);
            await context.SaveChangesAsync();
        }

        // Ensure source exists (idempotent)
        var source = await context.DimSources.SingleOrDefaultAsync(s => s.SourceName == "IntegrationTest");
        if (source is null)
        {
            source = new DimSource
            {
                SourceName = "IntegrationTest",
                BaseUrl = "https://test.local",
                IsActive = true
            };
            context.DimSources.Add(source);
            await context.SaveChangesAsync();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var dates = new[] { yesterday, today };
        foreach (var date in dates)
        {
            var dateKey = date.Year * 10000 + date.Month * 100 + date.Day;
            if (await context.DimDates.FindAsync(dateKey) is null)
            {
                var dt = date.ToDateTime(TimeOnly.MinValue);
                context.DimDates.Add(new DimDate
                {
                    DateKey = dateKey,
                    FullDate = date,
                    Year = date.Year,
                    Quarter = (byte)((date.Month - 1) / 3 + 1),
                    Month = (byte)date.Month,
                    Day = (byte)date.Day,
                    DayOfWeek = (byte)dt.DayOfWeek,
                    WeekOfYear = (byte)System.Globalization.ISOWeek.GetWeekOfYear(dt),
                    IsWeekend = dt.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday
                });
            }
        }
        await context.SaveChangesAsync();

        var yesterdayKey = yesterday.Year * 10000 + yesterday.Month * 100 + yesterday.Day;
        var todayKey = today.Year * 10000 + today.Month * 100 + today.Day;

        // Ensure fact quotes exist (idempotent)
        if (!await context.FactQuotes.AnyAsync(f => f.AssetKey == asset.AssetKey && f.DateKey == yesterdayKey && f.SourceKey == source.SourceKey))
        {
            context.FactQuotes.Add(new FactQuote
            {
                AssetKey = asset.AssetKey,
                DateKey = yesterdayKey,
                SourceKey = source.SourceKey,
                OpenPrice = 150m,
                HighPrice = 155m,
                LowPrice = 148m,
                ClosePrice = 152m,
                Volume = 5000000m,
                LoadedAt = DateTime.UtcNow
            });
        }

        if (!await context.FactQuotes.AnyAsync(f => f.AssetKey == asset.AssetKey && f.DateKey == todayKey && f.SourceKey == source.SourceKey))
        {
            context.FactQuotes.Add(new FactQuote
            {
                AssetKey = asset.AssetKey,
                DateKey = todayKey,
                SourceKey = source.SourceKey,
                OpenPrice = 152m,
                HighPrice = 158m,
                LowPrice = 151m,
                ClosePrice = 156m,
                Volume = 6000000m,
                LoadedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }
}
