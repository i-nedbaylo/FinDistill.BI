using FinDistill.Application.DTOs;

namespace FinDistill.Web.ViewModels;

/// <summary>
/// View model for the comparative returns chart page.
/// </summary>
public class ComparativeReturnViewModel
{
    /// <summary>Number of days displayed.</summary>
    public int Days { get; set; }

    /// <summary>Comparative return data grouped by ticker for multi-series chart.</summary>
    public IReadOnlyList<ComparativeReturnDto> Returns { get; set; } = [];

    /// <summary>Distinct tickers in the dataset.</summary>
    public IReadOnlyList<string> Tickers => Returns.Select(r => r.Ticker).Distinct().OrderBy(t => t).ToList();
}
