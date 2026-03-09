using FinDistill.Application.Configuration;
using FinDistill.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace FinDistill.Web;

/// <summary>
/// In-process ETL background worker for single-process deployments (e.g. free hosting tiers).
/// Functionally identical to the standalone EtlWorker; activated via Features:RunEtlInProcess = true.
/// </summary>
public class InProcessEtlWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InProcessEtlWorker> _logger;
    private readonly EtlScheduleOptions _schedule;

    public InProcessEtlWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<InProcessEtlWorker> logger,
        IOptions<EtlScheduleOptions> schedule)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _schedule = schedule.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _schedule.IntervalMinutes < 1 ? 1 : _schedule.IntervalMinutes;
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation(
            "In-process ETL worker started. Interval: {IntervalMinutes} minutes", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IEtlOrchestrator>();

                _logger.LogInformation("In-process ETL: starting pipeline run");
                var result = await orchestrator.RunEtlPipelineAsync(stoppingToken);

                if (result.IsSuccess)
                    _logger.LogInformation("In-process ETL: pipeline run completed successfully");
                else
                    _logger.LogWarning(
                        "In-process ETL: pipeline run finished with errors. Code: {Code}, Message: {Message}",
                        result.Error?.Code, result.Error?.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "In-process ETL: unhandled exception during pipeline run");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("In-process ETL worker stopped");
    }
}
