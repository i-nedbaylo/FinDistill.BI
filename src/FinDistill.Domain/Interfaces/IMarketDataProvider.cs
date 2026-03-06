using FinDistill.Domain.Enums;

namespace FinDistill.Domain.Interfaces;

/// <summary>
/// Strategy interface for external market data providers.
/// Each data source (Yahoo Finance, CoinGecko, etc.) implements this interface.
/// </summary>
public interface IMarketDataProvider
{
    DataSourceType SourceType { get; }

    Task<string> FetchRawDataAsync(string ticker, CancellationToken ct);

    Task<IEnumerable<string>> FetchBulkDataAsync(IEnumerable<string> tickers, CancellationToken ct);
}
