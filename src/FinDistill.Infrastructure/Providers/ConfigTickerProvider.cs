using FinDistill.Application.Interfaces;
using FinDistill.Domain.Enums;
using FinDistill.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace FinDistill.Infrastructure.Providers;

/// <summary>
/// Provides configured tickers/coin IDs for each data source from appsettings.
/// </summary>
public class ConfigTickerProvider : ITickerProvider
{
    private readonly DataSourcesOptions _options;

    public ConfigTickerProvider(IOptions<DataSourcesOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<string> GetTickers(DataSourceType sourceType)
    {
        return sourceType switch
        {
            DataSourceType.YahooFinance => _options.YahooFinance.Tickers,
            DataSourceType.CoinGecko => _options.CoinGecko.CoinIds,
            _ => []
        };
    }

    public bool IsEnabled(DataSourceType sourceType)
    {
        return sourceType switch
        {
            DataSourceType.YahooFinance => _options.YahooFinance.Enabled,
            DataSourceType.CoinGecko => _options.CoinGecko.Enabled,
            _ => false
        };
    }
}
