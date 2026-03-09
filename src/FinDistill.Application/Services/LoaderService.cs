using FinDistill.Application.DTOs;
using FinDistill.Application.Interfaces;
using FinDistill.Domain.Common;
using FinDistill.Domain.Entities;
using FinDistill.Domain.Enums;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinDistill.Application.Services;

/// <summary>
/// Loads transformed quote data into DWH: upserts dimensions, inserts facts (skipping duplicates).
/// Uses in-memory caches for dimension lookups within a single batch to avoid N+1 queries.
/// </summary>
public class LoaderService : ILoaderService
{
    private readonly IDimAssetRepository _assetRepo;
    private readonly IDimDateRepository _dateRepo;
    private readonly IDimSourceRepository _sourceRepo;
    private readonly IFactQuoteRepository _factRepo;
    private readonly ILogger<LoaderService> _logger;

    public LoaderService(
        IDimAssetRepository assetRepo,
        IDimDateRepository dateRepo,
        IDimSourceRepository sourceRepo,
        IFactQuoteRepository factRepo,
        ILogger<LoaderService> logger)
    {
        _assetRepo = assetRepo;
        _dateRepo = dateRepo;
        _sourceRepo = sourceRepo;
        _factRepo = factRepo;
        _logger = logger;
    }

    public async Task<Result> LoadAsync(IEnumerable<ParsedQuoteDto> quotes, CancellationToken ct)
    {
        try
        {
            var quoteList = quotes.ToList();
            _logger.LogInformation("ETL Load started, quotes to process: {Count}", quoteList.Count);

            // In-memory caches to avoid repeated DB lookups within this batch
            var assetCache = new Dictionary<string, DimAsset>();
            var dateCache = new Dictionary<DateOnly, DimDate>();
            var sourceCache = new Dictionary<DataSourceType, DimSource>();

            // Pre-resolve all unique dimensions first so we can bulk-fetch existing keys
            foreach (var dto in quoteList)
            {
                if (!assetCache.ContainsKey(dto.Ticker))
                {
                    assetCache[dto.Ticker] = await _assetRepo.UpsertAsync(new DimAsset
                    {
                        Ticker = dto.Ticker,
                        Name = dto.Ticker,
                        AssetType = ResolveAssetType(dto.SourceType),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }, ct);
                }

                if (!sourceCache.ContainsKey(dto.SourceType))
                {
                    sourceCache[dto.SourceType] = await _sourceRepo.UpsertAsync(new DimSource
                    {
                        SourceName = dto.SourceType.ToString(),
                        BaseUrl = string.Empty,
                        IsActive = true
                    }, ct);
                }
            }

            // Bulk-fetch existing (AssetKey, DateKey, SourceKey) keys per (asset, source) pair —
            // one query per pair instead of one query per quote row.
            var existingKeys = new HashSet<(int, int, int)>();
            foreach (var (assetKey, asset) in assetCache.Select(kv => (kv.Value.AssetKey, kv.Value)))
            {
                foreach (var source in sourceCache.Values)
                {
                    var keys = await _factRepo.GetExistingKeysAsync(asset.AssetKey, source.SourceKey, ct);
                    existingKeys.UnionWith(keys);
                }
            }

            var factsToInsert = new List<FactQuote>();
            // Track new keys added in this batch to prevent intra-batch duplicates
            var batchKeys = new HashSet<(int AssetKey, int DateKey, int SourceKey)>();
            var skipped = 0;

            foreach (var dto in quoteList)
            {
                try
                {
                    var asset = assetCache[dto.Ticker];
                    var source = sourceCache[dto.SourceType];

                    // Resolve date (cached per batch)
                    if (!dateCache.TryGetValue(dto.Date, out var dimDate))
                    {
                        dimDate = await _dateRepo.EnsureDateExistsAsync(dto.Date, ct);
                        dateCache[dto.Date] = dimDate;
                    }

                    var key = (asset.AssetKey, dimDate.DateKey, source.SourceKey);

                    // Skip if already in current batch or already in DB
                    if (!batchKeys.Add(key) || existingKeys.Contains(key))
                    {
                        skipped++;
                        continue;
                    }

                    factsToInsert.Add(new FactQuote
                    {
                        AssetKey = asset.AssetKey,
                        DateKey = dimDate.DateKey,
                        SourceKey = source.SourceKey,
                        OpenPrice = dto.Open,
                        HighPrice = dto.High,
                        LowPrice = dto.Low,
                        ClosePrice = dto.Close,
                        Volume = dto.Volume,
                        LoadedAt = DateTime.UtcNow
                    });
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ETL Load failed for ticker {Ticker} on {Date}", dto.Ticker, dto.Date);
                }
            }

            // Batch insert all facts in one SaveChanges call
            if (factsToInsert.Count > 0)
            {
                await _factRepo.AddRangeAsync(factsToInsert, ct);
            }

            _logger.LogInformation("ETL Load completed, loaded: {Loaded}, skipped duplicates: {Skipped}",
                factsToInsert.Count, skipped);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL Load failed with unhandled exception");
            return Result.Failure(new Error("Load.Failed", ex.Message));
        }
    }

    private static string ResolveAssetType(DataSourceType sourceType)
    {
        return sourceType switch
        {
            DataSourceType.CoinGecko => AssetType.Crypto.ToString(),
            DataSourceType.YahooFinance => AssetType.Stock.ToString(),
            _ => AssetType.Stock.ToString()
        };
    }
}
