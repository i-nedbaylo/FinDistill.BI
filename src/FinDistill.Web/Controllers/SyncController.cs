using FinDistill.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinDistill.Web.Controllers;

/// <summary>
/// Handles manual ETL pipeline trigger from the dashboard UI.
/// </summary>
public class SyncController : Controller
{
    private readonly IEtlOrchestrator _etlOrchestrator;
    private readonly ILogger<SyncController> _logger;

    public SyncController(IEtlOrchestrator etlOrchestrator, ILogger<SyncController> logger)
    {
        _etlOrchestrator = etlOrchestrator;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunSync(CancellationToken ct)
    {
        try
        {
            var result = await _etlOrchestrator.RunEtlPipelineAsync(ct);

            if (result.IsSuccess)
            {
                TempData["SyncMessage"] = "Sync completed successfully.";
                TempData["SyncStatus"] = "success";
            }
            else
            {
                _logger.LogWarning("Sync failed with error {ErrorCode}: {ErrorMessage}", result.Error.Code, result.Error.Message);
                TempData["SyncMessage"] = $"Sync failed ({result.Error.Code}). Check logs for details.";
                TempData["SyncStatus"] = "danger";
            }
        }
        catch (OperationCanceledException)
        {
            TempData["SyncMessage"] = "Sync was cancelled.";
            TempData["SyncStatus"] = "warning";
        }
        catch (Exception)
        {
            TempData["SyncMessage"] = "Sync failed. Check logs for details.";
            TempData["SyncStatus"] = "danger";
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
