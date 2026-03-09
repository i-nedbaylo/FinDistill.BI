using FinDistill.Application.DTOs;

namespace FinDistill.Web.ViewModels;

/// <summary>
/// View model for the asset detail page with price chart and OHLCV history table.
/// </summary>
public class AssetDetailViewModel
{
    /// <summary>Ticker symbol or coin ID.</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Human-readable asset name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Asset type string (e.g. "Stock", "Crypto").</summary>
    public string AssetType { get; set; } = string.Empty;

    /// <summary>Most recent closing price.</summary>
    public decimal LastClose { get; set; }

    /// <summary>Percentage change from the previous day's close.</summary>
    public decimal ChangePercent { get; set; }

    /// <summary>Historical OHLCV records for the requested time range, ordered by date descending.</summary>
    public IReadOnlyList<AssetHistoryDto> History { get; set; } = [];
}
