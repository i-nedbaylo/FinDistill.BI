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
    public async Task<IActionResult> RunSync(CancellationToken ct)
    {
        await _etlOrchestrator.RunEtlPipelineAsync(ct);
        return RedirectToAction("Index", "Dashboard");
    }
}
