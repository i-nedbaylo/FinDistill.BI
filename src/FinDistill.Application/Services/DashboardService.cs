using FinDistill.Application.DTOs;
using FinDistill.Application.Interfaces;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinDistill.Application.Services;

/// <summary>
/// Provides dashboard data by delegating reads to IDataMartReader.
/// Wraps calls with ICacheService for optional caching.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IDataMartReader _martReader;
    private readonly ICacheService _cache;
    private readonly ILogger<DashboardService> _logger;

    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    public DashboardService(
        IDataMartReader martReader,
        ICacheService cache,
        ILogger<DashboardService> logger)
    {
        _martReader = martReader;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DailyPerformanceDto>> GetDailyPerformanceAsync(CancellationToken ct)
    {
        const string cacheKey = "mart:daily:all";

        var cached = await _cache.GetAsync<List<DailyPerformanceDto>>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        var records = await _martReader.GetDailyPerformanceAsync(ct);

        var dtos = records.Select(r => new DailyPerformanceDto
        {
            Ticker = r.Ticker,
            Name = r.Name,
            AssetType = r.AssetType,
            ClosePrice = r.ClosePrice,
            ChangePercent = r.ChangePercent
        }).ToList();

        await _cache.SetAsync(cacheKey, dtos, DefaultCacheTtl, ct);
        return dtos;
    }

    public async Task<IReadOnlyList<AssetHistoryDto>> GetAssetHistoryAsync(string ticker, int days, CancellationToken ct)
    {
        var cacheKey = $"mart:history:{ticker}:{days}";

        var cached = await _cache.GetAsync<List<AssetHistoryDto>>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        var records = await _martReader.GetAssetHistoryAsync(ticker, days, ct);

        var dtos = records.Select(r => new AssetHistoryDto
        {
            Date = r.Date,
            Open = r.Open,
            High = r.High,
            Low = r.Low,
            Close = r.Close,
            Volume = r.Volume
        }).ToList();

        await _cache.SetAsync(cacheKey, dtos, DefaultCacheTtl, ct);
        return dtos;
    }

    public async Task<IReadOnlyList<PortfolioSummaryDto>> GetPortfolioSummaryAsync(CancellationToken ct)
    {
        const string cacheKey = "mart:portfolio";

        var cached = await _cache.GetAsync<List<PortfolioSummaryDto>>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        var records = await _martReader.GetPortfolioSummaryAsync(ct);

        var dtos = records.Select(r => new PortfolioSummaryDto
        {
            Ticker = r.Ticker,
            Name = r.Name,
            AssetType = r.AssetType,
            LastClose = r.LastClose,
            PreviousClose = r.PreviousClose,
            ChangePercent = r.ChangePercent
        }).ToList();

        await _cache.SetAsync(cacheKey, dtos, DefaultCacheTtl, ct);
        return dtos;
    }
}
