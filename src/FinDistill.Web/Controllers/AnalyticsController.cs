using FinDistill.Application.Interfaces;
using FinDistill.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinDistill.Web.Controllers;

/// <summary>
/// Provides analytical reports: comparative returns, 52-week high/low screener.
/// </summary>
public class AnalyticsController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IDashboardService dashboardService, ILogger<AnalyticsController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>Renders the comparative return chart for all assets.</summary>
    public async Task<IActionResult> Compare(int days = 90, CancellationToken ct = default)
    {
        var clampedDays = Math.Clamp(days, 7, 365);

        var result = await _dashboardService.GetComparativeReturnsAsync(clampedDays, ct);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to load comparative returns: {Error}", result.Error.Message);
        }

        var viewModel = new ComparativeReturnViewModel
        {
            Days = clampedDays,
            Returns = result.IsSuccess ? result.Value : []
        };

        return View(viewModel);
    }

    /// <summary>Renders the 52-week high/low screener table.</summary>
    public async Task<IActionResult> Week52(CancellationToken ct)
    {
        var result = await _dashboardService.GetWeek52HighLowAsync(ct);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to load 52-week data: {Error}", result.Error.Message);
        }

        var viewModel = new Week52HighLowViewModel
        {
            Assets = result.IsSuccess ? result.Value : []
        };

        return View(viewModel);
    }
}
