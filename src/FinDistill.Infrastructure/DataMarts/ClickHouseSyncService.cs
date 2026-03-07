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
/// Uses TRUNCATE + bulk insert (full-refresh) to avoid duplicates from ReplacingMergeTree merge lag.
/// FactQuotes are synced in batched pages to bound memory usage for large tables.
/// </summary>
public class ClickHouseSyncService : IClickHouseSyncService
{
    private const int FactPageSize = 10_000;

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

    private async Task TruncateTableAsync(ClickHouseConnection connection, string tableName, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"TRUNCATE TABLE IF EXISTS {tableName}";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task SyncDimAssetsAsync(ClickHouseConnection connection, CancellationToken ct)
    {
        var assets = await _dbContext.DimAssets.AsNoTracking().ToListAsync(ct);

        await TruncateTableAsync(connection, "dwh.DimAssets", ct);

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

        await TruncateTableAsync(connection, "dwh.DimDates", ct);

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

        await TruncateTableAsync(connection, "dwh.DimSources", ct);

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
        await TruncateTableAsync(connection, "dwh.FactQuotes", ct);

        long lastId = 0;
        int totalRows = 0;

        while (true)
        {
            var page = await _dbContext.FactQuotes
                .AsNoTracking()
                .Where(f => f.Id > lastId)
                .OrderBy(f => f.Id)
                .Take(FactPageSize)
                .ToListAsync(ct);

            if (page.Count == 0)
                break;

            using var bulkCopy = new ClickHouseBulkCopy(connection)
            {
                DestinationTableName = "dwh.FactQuotes",
                BatchSize = 5000
            };

            var rows = page.Select(f => new object[]
            {
                f.Id, f.AssetKey, f.DateKey, f.SourceKey,
                f.OpenPrice, f.HighPrice, f.LowPrice, f.ClosePrice,
                f.Volume, f.LoadedAt
            });

            await bulkCopy.InitAsync();
            await bulkCopy.WriteToServerAsync(rows, ct);

            totalRows += page.Count;
            lastId = page[^1].Id;

            if (page.Count < FactPageSize)
                break;
        }

        _logger.LogInformation("ClickHouse sync: FactQuotes — {Count} rows", totalRows);
    }
}
