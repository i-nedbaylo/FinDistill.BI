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
            await _etlOrchestrator.RunEtlPipelineAsync(ct);
            TempData["SyncMessage"] = "Sync completed successfully.";
            TempData["SyncStatus"] = "success";
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
