using FinDistill.Application.Interfaces;
using FinDistill.Domain.Entities;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinDistill.Application.Services;

/// <summary>
/// Extracts raw market data from all registered API providers and stores it in the Data Lake.
/// </summary>
public class ExtractorService : IExtractorService
{
    private readonly IEnumerable<IMarketDataProvider> _providers;
    private readonly IRawIngestDataRepository _rawRepo;
    private readonly ITickerProvider _tickerProvider;
    private readonly ILogger<ExtractorService> _logger;

    public ExtractorService(
        IEnumerable<IMarketDataProvider> providers,
        IRawIngestDataRepository rawRepo,
        ITickerProvider tickerProvider,
        ILogger<ExtractorService> logger)
    {
        _providers = providers;
        _rawRepo = rawRepo;
        _tickerProvider = tickerProvider;
        _logger = logger;
    }

    public async Task ExtractAsync(CancellationToken ct)
    {
        foreach (var provider in _providers)
        {
            var sourceName = provider.SourceType.ToString();

            if (!_tickerProvider.IsEnabled(provider.SourceType))
            {
                _logger.LogInformation("ETL Extract skipped for {Source} (disabled)", sourceName);
                continue;
            }

            var tickers = _tickerProvider.GetTickers(provider.SourceType);
            if (tickers.Count == 0)
            {
                _logger.LogWarning("ETL Extract: no tickers configured for {Source}", sourceName);
                continue;
            }

            _logger.LogInformation("ETL Extract started for {Source}, tickers: {Count}", sourceName, tickers.Count);

            try
            {
                var results = await provider.FetchBulkDataAsync(tickers, ct);

                var records = new List<RawIngestData>();
                foreach (var rawJson in results)
                {
                    records.Add(new RawIngestData
                    {
                        Source = sourceName,
                        Endpoint = sourceName,
                        RawContent = rawJson,
                        LoadedAt = DateTime.UtcNow,
                        IsProcessed = false
                    });
                }

                if (records.Count > 0)
                {
                    await _rawRepo.AddRangeAsync(records, ct);
                }

                _logger.LogInformation("ETL Extract completed for {Source}, records saved: {Count}", sourceName, records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETL Extract failed for {Source}", sourceName);
            }
        }
    }
}
