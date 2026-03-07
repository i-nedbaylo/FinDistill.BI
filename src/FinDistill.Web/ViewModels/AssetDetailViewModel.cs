using FinDistill.Application.DTOs;

namespace FinDistill.Web.ViewModels;

/// <summary>
/// View model for the asset detail page with price chart and OHLCV history table.
/// </summary>
public class AssetDetailViewModel
{
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public decimal LastClose { get; set; }
    public decimal ChangePercent { get; set; }
    public IReadOnlyList<AssetHistoryDto> History { get; set; } = [];
}
