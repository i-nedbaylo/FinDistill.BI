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
        var portfolio = await _dashboardService.GetPortfolioSummaryAsync(ct);

        var viewModel = new DashboardViewModel
        {
            PortfolioSummary = portfolio
        };

        return View(viewModel);
    }
}
