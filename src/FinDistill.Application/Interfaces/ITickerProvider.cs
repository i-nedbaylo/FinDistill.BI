using FinDistill.Domain.Enums;

namespace FinDistill.Application.Interfaces;

/// <summary>
/// Provides the list of tickers/coin IDs to fetch for each data source.
/// Configured via appsettings DataSources section.
/// </summary>
public interface ITickerProvider
{
    IReadOnlyList<string> GetTickers(DataSourceType sourceType);
    bool IsEnabled(DataSourceType sourceType);
}
