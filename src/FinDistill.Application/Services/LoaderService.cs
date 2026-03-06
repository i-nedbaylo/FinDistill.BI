using FinDistill.Application.DTOs;
using FinDistill.Application.Interfaces;
using FinDistill.Domain.Entities;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinDistill.Application.Services;

/// <summary>
/// Loads transformed quote data into DWH: upserts dimensions, inserts facts (skipping duplicates).
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

        var loaded = 0;
        var skipped = 0;

        foreach (var dto in quoteList)
        {
            try
            {
                // Ensure dimension records exist
                var asset = await _assetRepo.UpsertAsync(new DimAsset
                {
                    Ticker = dto.Ticker,
                    Name = dto.Ticker,
                    AssetType = dto.SourceType.ToString(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }, ct);

                var dimDate = await _dateRepo.EnsureDateExistsAsync(dto.Date, ct);

                var source = await _sourceRepo.UpsertAsync(new DimSource
                {
                    SourceName = dto.SourceType.ToString(),
                    BaseUrl = string.Empty,
                    IsActive = true
                }, ct);

                // Skip if this fact already exists
                if (await _factRepo.ExistsAsync(asset.AssetKey, dimDate.DateKey, source.SourceKey, ct))
                {
                    skipped++;
                    continue;
                }

                var fact = new FactQuote
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
                };

                await _factRepo.AddRangeAsync([fact], ct);
                loaded++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ETL Load failed for ticker {Ticker} on {Date}", dto.Ticker, dto.Date);
            }
        }

        _logger.LogInformation("ETL Load completed, loaded: {Loaded}, skipped duplicates: {Skipped}", loaded, skipped);
    }
}
