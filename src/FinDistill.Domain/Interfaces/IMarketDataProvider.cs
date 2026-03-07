using FinDistill.Domain.Enums;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Strategy interface for external market data providers.
/// Each data source (Yahoo Finance, CoinGecko, etc.) implements this interface.
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>Identifies which external source this provider fetches from.</summary>
    DataSourceType SourceType { get; }

    /// <summary>Fetches raw JSON data for a single ticker/coin ID.</summary>
    /// <param name="ticker">Ticker symbol or coin ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Standardized JSON string with parsed quote data.</returns>
    Task<string> FetchRawDataAsync(string ticker, CancellationToken ct);

    /// <summary>Fetches raw JSON data for multiple tickers/coin IDs with inter-request throttling.</summary>
    /// <param name="tickers">Collection of ticker symbols or coin IDs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of standardized JSON strings, one per successful fetch.</returns>
    Task<IEnumerable<string>> FetchBulkDataAsync(IEnumerable<string> tickers, CancellationToken ct);
}
