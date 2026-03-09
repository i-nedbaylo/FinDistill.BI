using FinDistill.Domain.Models;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Fetches live cryptocurrency market overview data (market cap, 24h change, volume, ATH).
/// Separate from IMarketDataProvider to avoid coupling ETL with real-time market data.
/// </summary>
public interface ICryptoMarketProvider
{
    /// <summary>
    /// Fetches the top coins by market cap from the market overview endpoint.
    /// </summary>
    /// <param name="vsCurrency">Quote currency (e.g. "usd").</param>
    /// <param name="limit">Maximum number of coins to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<CryptoMarketRecord>> GetMarketOverviewAsync(
        string vsCurrency, int limit, CancellationToken ct);
}
