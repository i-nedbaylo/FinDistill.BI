using FinDistill.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinDistill.Web.Controllers;

public class SyncController : Controller
{
    private readonly IEtlOrchestrator _etlOrchestrator;

    public SyncController(IEtlOrchestrator etlOrchestrator)
    {
        _etlOrchestrator = etlOrchestrator;
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
                TempData["SyncMessage"] = $"Sync failed: {result.Error.Message}";
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
