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
    private readonly ILogger<ExtractorService> _logger;

    public ExtractorService(
        IEnumerable<IMarketDataProvider> providers,
        IRawIngestDataRepository rawRepo,
        ILogger<ExtractorService> logger)
    {
        _providers = providers;
        _rawRepo = rawRepo;
        _logger = logger;
    }

    public async Task ExtractAsync(CancellationToken ct)
    {
        foreach (var provider in _providers)
        {
            var sourceName = provider.SourceType.ToString();
            _logger.LogInformation("ETL Extract started for {Source}", sourceName);

            try
            {
                var results = await provider.FetchBulkDataAsync([], ct);

                var count = 0;
                foreach (var rawJson in results)
                {
                    var record = new RawIngestData
                    {
                        Source = sourceName,
                        Endpoint = sourceName,
                        RawContent = rawJson,
                        LoadedAt = DateTime.UtcNow,
                        IsProcessed = false
                    };

                    await _rawRepo.AddAsync(record, ct);
                    count++;
                }

                _logger.LogInformation("ETL Extract completed for {Source}, records saved: {Count}", sourceName, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETL Extract failed for {Source}", sourceName);
            }
        }
    }
}
