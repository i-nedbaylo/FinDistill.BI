using FinDistill.Application.Interfaces;
using FinDistill.Worker.Configuration;
using Microsoft.Extensions.Options;

namespace FinDistill.Worker;

/// <summary>
/// Background service that runs the ETL pipeline on a configured schedule.
/// Orchestration logic is delegated to IEtlOrchestrator.
/// </summary>
public class EtlWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EtlWorker> _logger;
    private readonly EtlScheduleOptions _schedule;

    public EtlWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<EtlWorker> logger,
        IOptions<EtlScheduleOptions> schedule)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _schedule = schedule.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _schedule.IntervalMinutes;
        if (intervalMinutes < 1)
        {
            _logger.LogWarning(
                "Invalid ETL interval {ConfiguredInterval} minutes configured. Using minimum interval of 1 minute",
                intervalMinutes);
            intervalMinutes = 1;
        }

        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation("ETL Worker started. Schedule interval: {IntervalMinutes} minutes", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IEtlOrchestrator>();

                _logger.LogInformation("ETL Worker: starting pipeline run");
                await orchestrator.RunEtlPipelineAsync(stoppingToken);
                _logger.LogInformation("ETL Worker: pipeline run completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("ETL Worker: shutdown requested, stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETL Worker: unhandled error during pipeline run");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("ETL Worker stopped");
    }
}
