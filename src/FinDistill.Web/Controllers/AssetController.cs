using FinDistill.Application.Interfaces;
using FinDistill.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinDistill.Web.Controllers;

/// <summary>
/// Displays historical OHLCV data and Chart.js price chart for a single asset.
/// </summary>
public class AssetController : Controller
{
    private readonly IDashboardService _dashboardService;

    public AssetController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Renders the asset detail view with price chart and history table.</summary>
    /// <param name="ticker">Asset ticker symbol or coin ID.</param>
    /// <param name="days">Number of calendar days to display (1–365, default 30).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Detail(string ticker, int days = 30, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            return BadRequest("Ticker is required.");

        var clampedDays = Math.Clamp(days, 1, 365);

        var historyResult = await _dashboardService.GetAssetHistoryAsync(ticker, clampedDays, ct);
        var portfolioResult = await _dashboardService.GetPortfolioSummaryAsync(ct);

        var asset = portfolioResult.IsSuccess
            ? portfolioResult.Value.FirstOrDefault(p => p.Ticker == ticker)
            : null;

        var viewModel = new AssetDetailViewModel
        {
            Ticker = ticker,
            Name = asset?.Name ?? ticker,
            AssetType = asset?.AssetType ?? string.Empty,
            LastClose = asset?.LastClose ?? 0,
            ChangePercent = asset?.ChangePercent ?? 0,
            History = historyResult.IsSuccess ? historyResult.Value : []
        };

        return View(viewModel);
    }
}
