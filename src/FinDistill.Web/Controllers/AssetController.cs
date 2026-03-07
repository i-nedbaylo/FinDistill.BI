using FinDistill.Application.Interfaces;
using FinDistill.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinDistill.Web.Controllers;

public class AssetController : Controller
{
    private readonly IDashboardService _dashboardService;

    public AssetController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

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
