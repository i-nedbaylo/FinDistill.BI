using FinDistill.Application.Interfaces;
using FinDistill.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

    /// <summary>Renders the live Crypto Market Overview table with dominance chart.</summary>
    public async Task<IActionResult> CryptoMarket(int limit = 20, CancellationToken ct = default)
    {
        var clampedLimit = Math.Clamp(limit, 5, 100);
        var result = await _dashboardService.GetCryptoMarketOverviewAsync(clampedLimit, ct);

        if (result.IsFailure)
            _logger.LogWarning("Failed to load crypto market overview: {Error}", result.Error.Message);

        return View(new CryptoMarketViewModel
        {
            Limit = clampedLimit,
            Coins = result.IsSuccess ? result.Value : []
        });
    }

    /// <summary>Renders the Risk Analytics report: Sharpe Ratio + Max Drawdown.</summary>
    public async Task<IActionResult> Risk(int days = 365, CancellationToken ct = default)
    {
        var clampedDays = Math.Clamp(days, 30, 365);
        var result = await _dashboardService.GetRiskMetricsAsync(clampedDays, ct);

        if (result.IsFailure)
            _logger.LogWarning("Failed to load risk metrics: {Error}", result.Error.Message);

        return View(new RiskMetricsViewModel
        {
            Days = clampedDays,
            Metrics = result.IsSuccess ? result.Value : []
        });
    }
}
