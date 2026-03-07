using System.Diagnostics;
using FinDistill.Application.Interfaces;
using FinDistill.Domain.Common;
using Microsoft.Extensions.Logging;

namespace FinDistill.Application.Services;

/// <summary>
/// Orchestrates the full ETL pipeline: Extract → Transform → Load → (optional) ClickHouse Sync.
/// Uses Result pattern to propagate errors without exceptions.
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

    public async Task<Result> RunEtlPipelineAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("ETL pipeline started");

        try
        {
            // Extract
            var extractResult = await _extractor.ExtractAsync(ct);
            if (extractResult.IsFailure)
            {
                _logger.LogWarning("ETL Extract reported errors: {Error}", extractResult.Error.Message);
            }

            // Transform
            var transformResult = await _transformer.TransformAsync(ct);
            if (transformResult.IsFailure)
            {
                _logger.LogError("ETL Transform failed: {Error}", transformResult.Error.Message);
                return Result.Failure(transformResult.Error);
            }

            var parsed = transformResult.Value;

            // Load
            if (parsed.Count > 0)
            {
                var loadResult = await _loader.LoadAsync(parsed, ct);
                if (loadResult.IsFailure)
                {
                    _logger.LogError("ETL Load failed: {Error}", loadResult.Error.Message);
                    return Result.Failure(loadResult.Error);
                }

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
            return Result.Failure(new Error("Etl.UnhandledException", ex.Message));
        }

        sw.Stop();
        _logger.LogInformation("ETL pipeline finished in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        return Result.Success();
    }
}
