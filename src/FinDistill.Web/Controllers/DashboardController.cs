using FinDistill.Application.Interfaces;
using FinDistill.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinDistill.Web.Controllers;

public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var portfolioResult = await _dashboardService.GetPortfolioSummaryAsync(ct);

        if (portfolioResult.IsFailure)
        {
            _logger.LogWarning("Dashboard failed to load portfolio: {ErrorCode}: {ErrorMessage}",
                portfolioResult.Error.Code, portfolioResult.Error.Message);
            TempData["ErrorMessage"] = "Failed to load portfolio data. Check logs for details.";
            return View(new DashboardViewModel());
        }

        var viewModel = new DashboardViewModel
        {
            PortfolioSummary = portfolioResult.Value
        };

        return View(viewModel);
    }
}
