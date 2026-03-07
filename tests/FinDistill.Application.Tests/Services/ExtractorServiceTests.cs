using FinDistill.Application.Interfaces;
using FinDistill.Application.Services;
using FinDistill.Domain.Entities;
using FinDistill.Domain.Enums;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinDistill.Application.Tests.Services;

public class ExtractorServiceTests
{
    private readonly Mock<IRawIngestDataRepository> _rawRepoMock = new();
    private readonly Mock<ITickerProvider> _tickerProviderMock = new();
    private readonly Mock<ILogger<ExtractorService>> _loggerMock = new();

    private ExtractorService CreateSut(params IMarketDataProvider[] providers)
        => new(providers, _rawRepoMock.Object, _tickerProviderMock.Object, _loggerMock.Object);

    [Fact]
    public async Task ExtractAsync_EnabledSource_FetchesAndSavesData()
    {
        var providerMock = new Mock<IMarketDataProvider>();
        providerMock.Setup(p => p.SourceType).Returns(DataSourceType.YahooFinance);
        providerMock.Setup(p => p.FetchBulkDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { """[{"ticker":"AAPL"}]""" });

        _tickerProviderMock.Setup(t => t.IsEnabled(DataSourceType.YahooFinance)).Returns(true);
        _tickerProviderMock.Setup(t => t.GetTickers(DataSourceType.YahooFinance)).Returns(["AAPL"]);

        var sut = CreateSut(providerMock.Object);
        var result = await sut.ExtractAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        _rawRepoMock.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<RawIngestData>>(records => records.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExtractAsync_DisabledSource_SkipsFetching()
    {
        var providerMock = new Mock<IMarketDataProvider>();
        providerMock.Setup(p => p.SourceType).Returns(DataSourceType.YahooFinance);

        _tickerProviderMock.Setup(t => t.IsEnabled(DataSourceType.YahooFinance)).Returns(false);

        var sut = CreateSut(providerMock.Object);
        var result = await sut.ExtractAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        providerMock.Verify(p => p.FetchBulkDataAsync(
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExtractAsync_NoTickers_SkipsFetching()
    {
        var providerMock = new Mock<IMarketDataProvider>();
        providerMock.Setup(p => p.SourceType).Returns(DataSourceType.YahooFinance);

        _tickerProviderMock.Setup(t => t.IsEnabled(DataSourceType.YahooFinance)).Returns(true);
        _tickerProviderMock.Setup(t => t.GetTickers(DataSourceType.YahooFinance)).Returns([]);

        var sut = CreateSut(providerMock.Object);
        var result = await sut.ExtractAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        providerMock.Verify(p => p.FetchBulkDataAsync(
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExtractAsync_ProviderThrows_ReturnsPartialFailure()
    {
        var failingProvider = new Mock<IMarketDataProvider>();
        failingProvider.Setup(p => p.SourceType).Returns(DataSourceType.YahooFinance);
        failingProvider.Setup(p => p.FetchBulkDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API down"));

        var workingProvider = new Mock<IMarketDataProvider>();
        workingProvider.Setup(p => p.SourceType).Returns(DataSourceType.CoinGecko);
        workingProvider.Setup(p => p.FetchBulkDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { """[{"ticker":"BTC"}]""" });

        _tickerProviderMock.Setup(t => t.IsEnabled(It.IsAny<DataSourceType>())).Returns(true);
        _tickerProviderMock.Setup(t => t.GetTickers(DataSourceType.YahooFinance)).Returns(["AAPL"]);
        _tickerProviderMock.Setup(t => t.GetTickers(DataSourceType.CoinGecko)).Returns(["bitcoin"]);

        var sut = CreateSut(failingProvider.Object, workingProvider.Object);
        var result = await sut.ExtractAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Extract.PartialFailure", result.Error.Code);
        _rawRepoMock.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<RawIngestData>>(records => records.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExtractAsync_PassesCorrectTickersToProvider()
    {
        var providerMock = new Mock<IMarketDataProvider>();
        providerMock.Setup(p => p.SourceType).Returns(DataSourceType.YahooFinance);
        providerMock.Setup(p => p.FetchBulkDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        _tickerProviderMock.Setup(t => t.IsEnabled(DataSourceType.YahooFinance)).Returns(true);
        _tickerProviderMock.Setup(t => t.GetTickers(DataSourceType.YahooFinance))
            .Returns(["AAPL", "MSFT", "SPY"]);

        var sut = CreateSut(providerMock.Object);
        var result = await sut.ExtractAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        providerMock.Verify(p => p.FetchBulkDataAsync(
            It.Is<IEnumerable<string>>(tickers => tickers.SequenceEqual(new[] { "AAPL", "MSFT", "SPY" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
