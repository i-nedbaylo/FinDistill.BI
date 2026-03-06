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

        var history = await _dashboardService.GetAssetHistoryAsync(ticker, days, ct);
        var portfolio = await _dashboardService.GetPortfolioSummaryAsync(ct);
        var asset = portfolio.FirstOrDefault(p => p.Ticker == ticker);

        var viewModel = new AssetDetailViewModel
        {
            Ticker = ticker,
            Name = asset?.Name ?? ticker,
            AssetType = asset?.AssetType ?? string.Empty,
            LastClose = asset?.LastClose ?? 0,
            ChangePercent = asset?.ChangePercent ?? 0,
            History = history
        };

        return View(viewModel);
    }
}
