using FinDistill.Application.DTOs;

namespace FinDistill.Web.ViewModels;

/// <summary>ViewModel for the Crypto Market Overview page.</summary>
public class CryptoMarketViewModel
{
    /// <summary>Top N coins requested.</summary>
    public int Limit { get; set; } = 20;
    /// <summary>Live market data rows.</summary>
    public IReadOnlyList<CryptoMarketDto> Coins { get; set; } = [];
    /// <summary>Total market cap of all displayed coins (for dominance calculation).</summary>
    public decimal TotalMarketCap => Coins.Sum(c => c.MarketCap);
}
