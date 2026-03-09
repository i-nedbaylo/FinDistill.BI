using FinDistill.Application.DTOs;
using FinDistill.Application.Services;
using FinDistill.Domain.Entities;
using FinDistill.Domain.Enums;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinDistill.Application.Tests.Services;

public class LoaderServiceTests
{
    private readonly Mock<IDimAssetRepository> _assetRepoMock = new();
    private readonly Mock<IDimDateRepository> _dateRepoMock = new();
    private readonly Mock<IDimSourceRepository> _sourceRepoMock = new();
    private readonly Mock<IFactQuoteRepository> _factRepoMock = new();
    private readonly Mock<ILogger<LoaderService>> _loggerMock = new();

    private LoaderService CreateSut() => new(
        _assetRepoMock.Object, _dateRepoMock.Object,
        _sourceRepoMock.Object, _factRepoMock.Object, _loggerMock.Object);

    private void SetupDimensions()
    {
        _assetRepoMock.Setup(r => r.UpsertAsync(It.IsAny<DimAsset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DimAsset a, CancellationToken _) => new DimAsset { AssetKey = 1, Ticker = a.Ticker, AssetType = a.AssetType });
        _dateRepoMock.Setup(r => r.EnsureDateExistsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateOnly d, CancellationToken _) => new DimDate { DateKey = d.Year * 10000 + d.Month * 100 + d.Day, FullDate = d });
        _sourceRepoMock.Setup(r => r.UpsertAsync(It.IsAny<DimSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DimSource s, CancellationToken _) => new DimSource { SourceKey = 1, SourceName = s.SourceName });
        // Default: no existing keys in DB
        _factRepoMock.Setup(r => r.GetExistingKeysAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(int, int, int)>());
    }

    [Fact]
    public async Task LoadAsync_NewQuotes_InsertsViaAddRangeAsync()
    {
        SetupDimensions();

        var quotes = new List<ParsedQuoteDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2024, 1, 15), Close = 150, SourceType = DataSourceType.YahooFinance }
        };

        var sut = CreateSut();
        var result = await sut.LoadAsync(quotes, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _factRepoMock.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<FactQuote>>(q => q.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_DuplicateQuotes_SkipsExisting()
    {
        SetupDimensions();
        // Return the key as already existing in DB
        _factRepoMock.Setup(r => r.GetExistingKeysAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(int, int, int)> { (1, 20240115, 1) });

        var quotes = new List<ParsedQuoteDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2024, 1, 15), Close = 150, SourceType = DataSourceType.YahooFinance }
        };

        var sut = CreateSut();
        var result = await sut.LoadAsync(quotes, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _factRepoMock.Verify(r => r.AddRangeAsync(
            It.IsAny<IEnumerable<FactQuote>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_YahooFinance_SetsAssetTypeToStock()
    {
        SetupDimensions();

        var quotes = new List<ParsedQuoteDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2024, 1, 15), SourceType = DataSourceType.YahooFinance }
        };

        var sut = CreateSut();
        var result = await sut.LoadAsync(quotes, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _assetRepoMock.Verify(r => r.UpsertAsync(
            It.Is<DimAsset>(a => a.AssetType == "Stock"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_CoinGecko_SetsAssetTypeToCrypto()
    {
        SetupDimensions();

        var quotes = new List<ParsedQuoteDto>
        {
            new() { Ticker = "BTC", Date = new DateOnly(2024, 1, 15), SourceType = DataSourceType.CoinGecko }
        };

        var sut = CreateSut();
        var result = await sut.LoadAsync(quotes, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _assetRepoMock.Verify(r => r.UpsertAsync(
            It.Is<DimAsset>(a => a.AssetType == "Crypto"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_SameTickerMultipleDates_CachesDimensionLookup()
    {
        SetupDimensions();

        var quotes = new List<ParsedQuoteDto>
        {
            new() { Ticker = "AAPL", Date = new DateOnly(2024, 1, 15), SourceType = DataSourceType.YahooFinance },
            new() { Ticker = "AAPL", Date = new DateOnly(2024, 1, 16), SourceType = DataSourceType.YahooFinance },
            new() { Ticker = "AAPL", Date = new DateOnly(2024, 1, 17), SourceType = DataSourceType.YahooFinance }
        };

        var sut = CreateSut();
        var result = await sut.LoadAsync(quotes, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Asset and Source UpsertAsync called only once each (pre-resolved and cached)
        _assetRepoMock.Verify(r => r.UpsertAsync(It.IsAny<DimAsset>(), It.IsAny<CancellationToken>()), Times.Once);
        _sourceRepoMock.Verify(r => r.UpsertAsync(It.IsAny<DimSource>(), It.IsAny<CancellationToken>()), Times.Once);
        // Date called 3 times (different dates)
        _dateRepoMock.Verify(r => r.EnsureDateExistsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        // GetExistingKeysAsync called once (one asset/source pair)
        _factRepoMock.Verify(r => r.GetExistingKeysAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        // All 3 facts in one batch
        _factRepoMock.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<FactQuote>>(q => q.Count() == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadAsync_EmptyQuotes_DoesNotCallRepositories()
    {
        var sut = CreateSut();
        var result = await sut.LoadAsync([], CancellationToken.None);

        Assert.True(result.IsSuccess);
        _assetRepoMock.Verify(r => r.UpsertAsync(It.IsAny<DimAsset>(), It.IsAny<CancellationToken>()), Times.Never);
        _factRepoMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<FactQuote>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_IntraBatchDuplicates_InsertsOnlyFirst()
    {
        SetupDimensions();

        // Two quotes with same (Ticker, Date, SourceType) in one batch
        var quotes = new List<ParsedQuoteDto>
        {
            new() { Ticker = "BTC", Date = new DateOnly(2024, 3, 7), Close = 100, SourceType = DataSourceType.CoinGecko },
            new() { Ticker = "BTC", Date = new DateOnly(2024, 3, 7), Close = 101, SourceType = DataSourceType.CoinGecko }
        };

        var sut = CreateSut();
        var result = await sut.LoadAsync(quotes, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Only one fact inserted despite two input quotes with the same key
        _factRepoMock.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<FactQuote>>(q => q.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        // GetExistingKeysAsync called once (bulk check), not per row
        _factRepoMock.Verify(r => r.GetExistingKeysAsync(
            It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
        // ExistsAsync never called (replaced by bulk check)
        _factRepoMock.Verify(r => r.ExistsAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
