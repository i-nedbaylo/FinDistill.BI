using FinDistill.Domain.Enums;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Provides the list of tickers/coin IDs to fetch for each data source.
/// Configured via appsettings DataSources section.
/// </summary>
public interface ITickerProvider
{
    /// <summary>Returns the list of tickers or coin IDs configured for the given source.</summary>
    /// <param name="sourceType">The data source to retrieve tickers for.</param>
    /// <returns>A read-only list of ticker or coin ID strings.</returns>
    IReadOnlyList<string> GetTickers(DataSourceType sourceType);

    /// <summary>Returns whether the given data source is enabled in configuration.</summary>
    /// <param name="sourceType">The data source to check.</param>
    /// <returns>True if the data source is enabled, otherwise false.</returns>
    bool IsEnabled(DataSourceType sourceType);
}
