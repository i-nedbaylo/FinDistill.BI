using FinDistill.Application.Interfaces;
using FinDistill.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinDistill.Web.Controllers;

public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var portfolioResult = await _dashboardService.GetPortfolioSummaryAsync(ct);

        if (portfolioResult.IsFailure)
        {
            TempData["ErrorMessage"] = portfolioResult.Error.Message;
            return View(new DashboardViewModel());
        }

        var viewModel = new DashboardViewModel
        {
            PortfolioSummary = portfolioResult.Value
        };

        return View(viewModel);
    }
}
