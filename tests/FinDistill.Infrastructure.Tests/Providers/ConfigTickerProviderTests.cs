using FinDistill.Domain.Enums;
using FinDistill.Infrastructure.Configuration;
using FinDistill.Infrastructure.Providers;
using Microsoft.Extensions.Options;
using Moq;

namespace FinDistill.Infrastructure.Tests.Providers;

public class ConfigTickerProviderTests
{
    [Fact]
    public void GetTickers_YahooFinance_ReturnsConfiguredTickers()
    {
        var options = Options.Create(new DataSourcesOptions
        {
            YahooFinance = new YahooFinanceOptions { Tickers = ["AAPL", "MSFT"] }
        });

        var sut = new ConfigTickerProvider(options);
        var tickers = sut.GetTickers(DataSourceType.YahooFinance);

        Assert.Equal(["AAPL", "MSFT"], tickers);
    }

    [Fact]
    public void GetTickers_CoinGecko_ReturnsCoinIds()
    {
        var options = Options.Create(new DataSourcesOptions
        {
            CoinGecko = new CoinGeckoOptions { CoinIds = ["bitcoin", "ethereum"] }
        });

        var sut = new ConfigTickerProvider(options);
        var tickers = sut.GetTickers(DataSourceType.CoinGecko);

        Assert.Equal(["bitcoin", "ethereum"], tickers);
    }

    [Fact]
    public void IsEnabled_EnabledSource_ReturnsTrue()
    {
        var options = Options.Create(new DataSourcesOptions
        {
            YahooFinance = new YahooFinanceOptions { Enabled = true }
        });

        var sut = new ConfigTickerProvider(options);

        Assert.True(sut.IsEnabled(DataSourceType.YahooFinance));
    }

    [Fact]
    public void IsEnabled_DisabledSource_ReturnsFalse()
    {
        var options = Options.Create(new DataSourcesOptions
        {
            CoinGecko = new CoinGeckoOptions { Enabled = false }
        });

        var sut = new ConfigTickerProvider(options);

        Assert.False(sut.IsEnabled(DataSourceType.CoinGecko));
    }
}
