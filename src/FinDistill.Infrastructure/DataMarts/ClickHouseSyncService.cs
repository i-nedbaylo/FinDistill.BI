using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using FinDistill.Application.Interfaces;
using FinDistill.Infrastructure.Configuration;
using FinDistill.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinDistill.Infrastructure.DataMarts;

/// <summary>
/// Synchronizes dimension and fact data from the OLTP DWH (EF Core) to ClickHouse tables.
/// Performs batch inserts using ClickHouseBulkCopy for efficient data transfer.
/// </summary>
public class ClickHouseSyncService : IClickHouseSyncService
{
    private readonly FinDistillDbContext _dbContext;
    private readonly string _connectionString;
    private readonly ILogger<ClickHouseSyncService> _logger;

    public ClickHouseSyncService(
        FinDistillDbContext dbContext,
        IOptions<ClickHouseOptions> options,
        ILogger<ClickHouseSyncService> logger)
    {
        _dbContext = dbContext;
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("ClickHouse sync started");

        try
        {
            using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync(ct);

            await SyncDimAssetsAsync(connection, ct);
            await SyncDimDatesAsync(connection, ct);
            await SyncDimSourcesAsync(connection, ct);
            await SyncFactQuotesAsync(connection, ct);

            _logger.LogInformation("ClickHouse sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClickHouse sync failed");
            throw;
        }
    }

    private async Task SyncDimAssetsAsync(ClickHouseConnection connection, CancellationToken ct)
    {
        var assets = await _dbContext.DimAssets.AsNoTracking().ToListAsync(ct);

        using var bulkCopy = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "dwh.DimAssets",
            BatchSize = 1000
        };

        var rows = assets.Select(a => new object[]
        {
            a.AssetKey, a.Ticker, a.Name, a.AssetType, a.Exchange ?? string.Empty,
            a.IsActive ? (byte)1 : (byte)0, a.CreatedAt, a.UpdatedAt
        });

        await bulkCopy.InitAsync();
        await bulkCopy.WriteToServerAsync(rows, ct);

        _logger.LogInformation("ClickHouse sync: DimAssets — {Count} rows", assets.Count);
    }

    private async Task SyncDimDatesAsync(ClickHouseConnection connection, CancellationToken ct)
    {
        var dates = await _dbContext.DimDates.AsNoTracking().ToListAsync(ct);

        using var bulkCopy = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "dwh.DimDates",
            BatchSize = 1000
        };

        var rows = dates.Select(d => new object[]
        {
            d.DateKey, d.FullDate.ToDateTime(TimeOnly.MinValue), d.Year, d.Quarter,
            d.Month, d.Day, d.DayOfWeek, d.WeekOfYear, d.IsWeekend ? (byte)1 : (byte)0
        });

        await bulkCopy.InitAsync();
        await bulkCopy.WriteToServerAsync(rows, ct);

        _logger.LogInformation("ClickHouse sync: DimDates — {Count} rows", dates.Count);
    }

    private async Task SyncDimSourcesAsync(ClickHouseConnection connection, CancellationToken ct)
    {
        var sources = await _dbContext.DimSources.AsNoTracking().ToListAsync(ct);

        using var bulkCopy = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "dwh.DimSources",
            BatchSize = 1000
        };

        var rows = sources.Select(s => new object[]
        {
            s.SourceKey, s.SourceName, s.BaseUrl, s.IsActive ? (byte)1 : (byte)0
        });

        await bulkCopy.InitAsync();
        await bulkCopy.WriteToServerAsync(rows, ct);

        _logger.LogInformation("ClickHouse sync: DimSources — {Count} rows", sources.Count);
    }

    private async Task SyncFactQuotesAsync(ClickHouseConnection connection, CancellationToken ct)
    {
        var facts = await _dbContext.FactQuotes.AsNoTracking().ToListAsync(ct);

        using var bulkCopy = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "dwh.FactQuotes",
            BatchSize = 5000
        };

        var rows = facts.Select(f => new object[]
        {
            f.Id, f.AssetKey, f.DateKey, f.SourceKey,
            f.OpenPrice, f.HighPrice, f.LowPrice, f.ClosePrice,
            f.Volume, f.LoadedAt
        });

        await bulkCopy.InitAsync();
        await bulkCopy.WriteToServerAsync(rows, ct);

        _logger.LogInformation("ClickHouse sync: FactQuotes — {Count} rows", facts.Count);
    }
}
