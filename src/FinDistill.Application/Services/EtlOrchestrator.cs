using System.Diagnostics;
using FinDistill.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinDistill.Application.Services;

/// <summary>
/// Orchestrates the full ETL pipeline: Extract → Transform → Load → (optional) ClickHouse Sync.
/// Catches and logs exceptions at each stage without stopping the pipeline.
/// </summary>
public class EtlOrchestrator : IEtlOrchestrator
{
    private readonly IExtractorService _extractor;
    private readonly ITransformerService _transformer;
    private readonly ILoaderService _loader;
    private readonly IClickHouseSyncService? _clickHouseSync;
    private readonly ILogger<EtlOrchestrator> _logger;

    public EtlOrchestrator(
        IExtractorService extractor,
        ITransformerService transformer,
        ILoaderService loader,
        ILogger<EtlOrchestrator> logger,
        IClickHouseSyncService? clickHouseSync = null)
    {
        _extractor = extractor;
        _transformer = transformer;
        _loader = loader;
        _clickHouseSync = clickHouseSync;
        _logger = logger;
    }

    public async Task RunEtlPipelineAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("ETL pipeline started");

        try
        {
            // Extract
            await _extractor.ExtractAsync(ct);

            // Transform
            var parsed = await _transformer.TransformAsync(ct);

            // Load
            if (parsed.Count > 0)
            {
                await _loader.LoadAsync(parsed, ct);

                // Sync to ClickHouse (if enabled)
                if (_clickHouseSync is not null)
                {
                    await _clickHouseSync.SyncAsync(ct);
                }
            }
            else
            {
                _logger.LogInformation("ETL pipeline: no new data to load");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ETL pipeline was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ETL pipeline failed with unhandled exception");
        }

        sw.Stop();
        _logger.LogInformation("ETL pipeline finished in {ElapsedMs} ms", sw.ElapsedMilliseconds);
    }
}
