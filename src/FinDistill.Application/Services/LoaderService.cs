using FinDistill.Application.DTOs;
using FinDistill.Application.Interfaces;
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

    public async Task LoadAsync(IEnumerable<ParsedQuoteDto> quotes, CancellationToken ct)
    {
        var quoteList = quotes.ToList();
        _logger.LogInformation("ETL Load started, quotes to process: {Count}", quoteList.Count);

        // In-memory caches to avoid repeated DB lookups within this batch
        var assetCache = new Dictionary<string, DimAsset>();
        var dateCache = new Dictionary<DateOnly, DimDate>();
        var sourceCache = new Dictionary<DataSourceType, DimSource>();

        var factsToInsert = new List<FactQuote>();
        var skipped = 0;

        foreach (var dto in quoteList)
        {
            try
            {
                // Resolve asset (cached)
                if (!assetCache.TryGetValue(dto.Ticker, out var asset))
                {
                    asset = await _assetRepo.UpsertAsync(new DimAsset
                    {
                        Ticker = dto.Ticker,
                        Name = dto.Ticker,
                        AssetType = ResolveAssetType(dto.SourceType),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }, ct);
                    assetCache[dto.Ticker] = asset;
                }

                // Resolve date (cached)
                if (!dateCache.TryGetValue(dto.Date, out var dimDate))
                {
                    dimDate = await _dateRepo.EnsureDateExistsAsync(dto.Date, ct);
                    dateCache[dto.Date] = dimDate;
                }

                // Resolve source (cached)
                if (!sourceCache.TryGetValue(dto.SourceType, out var source))
                {
                    source = await _sourceRepo.UpsertAsync(new DimSource
                    {
                        SourceName = dto.SourceType.ToString(),
                        BaseUrl = string.Empty,
                        IsActive = true
                    }, ct);
                    sourceCache[dto.SourceType] = source;
                }

                // Skip if this fact already exists
                if (await _factRepo.ExistsAsync(asset.AssetKey, dimDate.DateKey, source.SourceKey, ct))
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
