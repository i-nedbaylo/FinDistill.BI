using FinDistill.Application.DTOs;

namespace FinDistill.Web.ViewModels;

/// <summary>
/// View model for the 52-week high/low screener page.
/// </summary>
public class Week52HighLowViewModel
{
    /// <summary>52-week high/low data for each active asset.</summary>
    public IReadOnlyList<Week52HighLowDto> Assets { get; set; } = [];
}
