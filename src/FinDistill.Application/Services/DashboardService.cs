using FinDistill.Application.DTOs;
using FinDistill.Application.Interfaces;
using FinDistill.Domain.Common;
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
    private readonly ICryptoMarketProvider _cryptoMarketProvider;
    private readonly ILogger<DashboardService> _logger;

    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(5);

    public DashboardService(
        IDataMartReader martReader,
        ICacheService cache,
        ICryptoMarketProvider cryptoMarketProvider,
        ILogger<DashboardService> logger)
    {
        _martReader = martReader;
        _cache = cache;
        _cryptoMarketProvider = cryptoMarketProvider;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<DailyPerformanceDto>>> GetDailyPerformanceAsync(CancellationToken ct)
    {
        try
        {
            const string cacheKey = "mart:daily:all";

            var cached = await _cache.GetAsync<List<DailyPerformanceDto>>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return Result.Success<IReadOnlyList<DailyPerformanceDto>>(cached);
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
            return Result.Success<IReadOnlyList<DailyPerformanceDto>>(dtos);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get daily performance data");
            return Result.Failure<IReadOnlyList<DailyPerformanceDto>>(
                new Error("Dashboard.DailyPerformance", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<AssetHistoryDto>>> GetAssetHistoryAsync(string ticker, int days, CancellationToken ct)
    {
        try
        {
            var cacheKey = $"mart:history:{ticker}:{days}";

            var cached = await _cache.GetAsync<List<AssetHistoryDto>>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return Result.Success<IReadOnlyList<AssetHistoryDto>>(cached);
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
            return Result.Success<IReadOnlyList<AssetHistoryDto>>(dtos);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get asset history for {Ticker}", ticker);
            return Result.Failure<IReadOnlyList<AssetHistoryDto>>(
                new Error("Dashboard.AssetHistory", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<PortfolioSummaryDto>>> GetPortfolioSummaryAsync(CancellationToken ct)
    {
        try
        {
            const string cacheKey = "mart:portfolio";

            var cached = await _cache.GetAsync<List<PortfolioSummaryDto>>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return Result.Success<IReadOnlyList<PortfolioSummaryDto>>(cached);
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
            return Result.Success<IReadOnlyList<PortfolioSummaryDto>>(dtos);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get portfolio summary");
            return Result.Failure<IReadOnlyList<PortfolioSummaryDto>>(
                new Error("Dashboard.PortfolioSummary", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<ComparativeReturnDto>>> GetComparativeReturnsAsync(int days, CancellationToken ct)
    {
        try
        {
            var cacheKey = $"mart:compare:{days}";

            var cached = await _cache.GetAsync<List<ComparativeReturnDto>>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return Result.Success<IReadOnlyList<ComparativeReturnDto>>(cached);
            }

            var records = await _martReader.GetComparativeReturnsAsync(days, ct);

            var dtos = records.Select(r => new ComparativeReturnDto
            {
                Ticker = r.Ticker,
                Date = r.Date,
                Close = r.Close,
                NormalizedReturn = r.NormalizedReturn
            }).ToList();

            await _cache.SetAsync(cacheKey, dtos, DefaultCacheTtl, ct);
            return Result.Success<IReadOnlyList<ComparativeReturnDto>>(dtos);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get comparative returns");
            return Result.Failure<IReadOnlyList<ComparativeReturnDto>>(
                new Error("Dashboard.ComparativeReturns", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<Week52HighLowDto>>> GetWeek52HighLowAsync(CancellationToken ct)
    {
        try
        {
            const string cacheKey = "mart:52whl";

            var cached = await _cache.GetAsync<List<Week52HighLowDto>>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return Result.Success<IReadOnlyList<Week52HighLowDto>>(cached);
            }

            var records = await _martReader.GetWeek52HighLowAsync(ct);

            var dtos = records.Select(r => new Week52HighLowDto
            {
                Ticker = r.Ticker,
                Name = r.Name,
                AssetType = r.AssetType,
                LastClose = r.LastClose,
                High52W = r.High52W,
                Low52W = r.Low52W,
                PctFromHigh = r.PctFromHigh,
                PctFromLow = r.PctFromLow
            }).ToList();

            await _cache.SetAsync(cacheKey, dtos, DefaultCacheTtl, ct);
            return Result.Success<IReadOnlyList<Week52HighLowDto>>(dtos);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get 52-week high/low data");
            return Result.Failure<IReadOnlyList<Week52HighLowDto>>(
                new Error("Dashboard.Week52HighLow", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<CryptoMarketDto>>> GetCryptoMarketOverviewAsync(int limit, CancellationToken ct)
    {
        try
        {
            var clampedLimit = Math.Clamp(limit, 1, 250);
            var cacheKey = $"market:crypto:{clampedLimit}";

            var cached = await _cache.GetAsync<List<CryptoMarketDto>>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return Result.Success<IReadOnlyList<CryptoMarketDto>>(cached);
            }

            var records = await _cryptoMarketProvider.GetMarketOverviewAsync("usd", clampedLimit, ct);

            var dtos = records.Select(r => new CryptoMarketDto
            {
                Id = r.Id,
                Symbol = r.Symbol,
                Name = r.Name,
                Image = r.Image,
                CurrentPrice = r.CurrentPrice,
                MarketCap = r.MarketCap,
                MarketCapRank = r.MarketCapRank,
                TotalVolume = r.TotalVolume,
                PriceChangePercent24H = r.PriceChangePercent24H,
                Ath = r.Ath,
                AthChangePercent = r.AthChangePercent,
                CirculatingSupply = r.CirculatingSupply
            }).ToList();

            // Shorter TTL for live market data
            await _cache.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(2), ct);
            return Result.Success<IReadOnlyList<CryptoMarketDto>>(dtos);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get crypto market overview");
            return Result.Failure<IReadOnlyList<CryptoMarketDto>>(
                new Error("Dashboard.CryptoMarketOverview", ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<RiskMetricsDto>>> GetRiskMetricsAsync(int days, CancellationToken ct)
    {
        try
        {
            var clampedDays = Math.Clamp(days, 30, 365);
            var cacheKey = $"mart:risk:{clampedDays}";

            var cached = await _cache.GetAsync<List<RiskMetricsDto>>(cacheKey, ct);
            if (cached is not null)
            {
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return Result.Success<IReadOnlyList<RiskMetricsDto>>(cached);
            }

            var records = await _martReader.GetRiskMetricsAsync(clampedDays, ct);

            var dtos = records.Select(r => new RiskMetricsDto
            {
                Ticker = r.Ticker,
                Name = r.Name,
                AssetType = r.AssetType,
                SharpeRatio = r.SharpeRatio,
                MaxDrawdown = r.MaxDrawdown,
                AnnualisedVolatility = r.AnnualisedVolatility,
                MeanDailyReturn = r.MeanDailyReturn,
                TradingDays = r.TradingDays
            }).ToList();

            await _cache.SetAsync(cacheKey, dtos, DefaultCacheTtl, ct);
            return Result.Success<IReadOnlyList<RiskMetricsDto>>(dtos);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get risk metrics");
            return Result.Failure<IReadOnlyList<RiskMetricsDto>>(
                new Error("Dashboard.RiskMetrics", ex.Message));
        }
    }
}
