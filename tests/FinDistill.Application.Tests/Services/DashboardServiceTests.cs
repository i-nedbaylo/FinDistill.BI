using FinDistill.Application.DTOs;
using FinDistill.Application.Services;
using FinDistill.Domain.Interfaces;
using FinDistill.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinDistill.Application.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IDataMartReader> _martReaderMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ILogger<DashboardService>> _loggerMock = new();

    private DashboardService CreateSut() => new(_martReaderMock.Object, _cacheMock.Object, _loggerMock.Object);

    [Fact]
    public async Task GetPortfolioSummaryAsync_CacheMiss_ReadsFromMart()
    {
        _cacheMock.Setup(c => c.GetAsync<List<PortfolioSummaryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<PortfolioSummaryDto>?)null);

        _martReaderMock.Setup(m => m.GetPortfolioSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PortfolioSummaryRecord>
            {
                new() { Ticker = "AAPL", Name = "Apple", AssetType = "Stock", LastClose = 150, PreviousClose = 148, ChangePercent = 1.35m }
            });

        var sut = CreateSut();
        var result = await sut.GetPortfolioSummaryAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("AAPL", result.Value[0].Ticker);
        _martReaderMock.Verify(m => m.GetPortfolioSummaryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAssetHistoryAsync_CacheMiss_ReadsFromMart()
    {
        _cacheMock.Setup(c => c.GetAsync<List<AssetHistoryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<AssetHistoryDto>?)null);

        _martReaderMock.Setup(m => m.GetAssetHistoryAsync("AAPL", 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetHistoryRecord>
            {
                new() { Date = new DateOnly(2024, 1, 15), Close = 150 }
            });

        var sut = CreateSut();
        var result = await sut.GetAssetHistoryAsync("AAPL", 30, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        _martReaderMock.Verify(m => m.GetAssetHistoryAsync("AAPL", 30, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetComparativeReturnsAsync_CacheMiss_ReadsFromMart()
    {
        _cacheMock.Setup(c => c.GetAsync<List<ComparativeReturnDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ComparativeReturnDto>?)null);

        _martReaderMock.Setup(m => m.GetComparativeReturnsAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ComparativeReturnRecord>
            {
                new() { Ticker = "AAPL", Date = new DateOnly(2024, 1, 15), Close = 150, NormalizedReturn = 105.5m }
            });

        var sut = CreateSut();
        var result = await sut.GetComparativeReturnsAsync(90, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("AAPL", result.Value[0].Ticker);
        Assert.Equal(105.5m, result.Value[0].NormalizedReturn);
        _martReaderMock.Verify(m => m.GetComparativeReturnsAsync(90, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetComparativeReturnsAsync_CacheHit_SkipsMart()
    {
        var cached = new List<ComparativeReturnDto>
        {
            new() { Ticker = "MSFT", Date = new DateOnly(2024, 1, 15), Close = 400, NormalizedReturn = 102m }
        };
        _cacheMock.Setup(c => c.GetAsync<List<ComparativeReturnDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var sut = CreateSut();
        var result = await sut.GetComparativeReturnsAsync(90, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("MSFT", result.Value[0].Ticker);
        _martReaderMock.Verify(m => m.GetComparativeReturnsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetWeek52HighLowAsync_CacheMiss_ReadsFromMart()
    {
        _cacheMock.Setup(c => c.GetAsync<List<Week52HighLowDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Week52HighLowDto>?)null);

        _martReaderMock.Setup(m => m.GetWeek52HighLowAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Week52HighLowRecord>
            {
                new()
                {
                    Ticker = "SPY", Name = "SPDR S&P 500", AssetType = "ETF",
                    LastClose = 500, High52W = 520, Low52W = 400,
                    PctFromHigh = -3.85m, PctFromLow = 25m
                }
            });

        var sut = CreateSut();
        var result = await sut.GetWeek52HighLowAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        var dto = result.Value[0];
        Assert.Equal("SPY", dto.Ticker);
        Assert.Equal("SPDR S&P 500", dto.Name);
        Assert.Equal(520m, dto.High52W);
        Assert.Equal(400m, dto.Low52W);
        Assert.Equal(-3.85m, dto.PctFromHigh);
        Assert.Equal(25m, dto.PctFromLow);
        _martReaderMock.Verify(m => m.GetWeek52HighLowAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWeek52HighLowAsync_CacheHit_SkipsMart()
    {
        var cached = new List<Week52HighLowDto>
        {
            new() { Ticker = "QQQ", Name = "Invesco QQQ", AssetType = "ETF", LastClose = 450, High52W = 460, Low52W = 350, PctFromHigh = -2.17m, PctFromLow = 28.57m }
        };
        _cacheMock.Setup(c => c.GetAsync<List<Week52HighLowDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var sut = CreateSut();
        var result = await sut.GetWeek52HighLowAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("QQQ", result.Value[0].Ticker);
        _martReaderMock.Verify(m => m.GetWeek52HighLowAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetComparativeReturnsAsync_MartThrows_ReturnsFailure()
    {
        _cacheMock.Setup(c => c.GetAsync<List<ComparativeReturnDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ComparativeReturnDto>?)null);

        _martReaderMock.Setup(m => m.GetComparativeReturnsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        var sut = CreateSut();
        var result = await sut.GetComparativeReturnsAsync(90, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Dashboard.ComparativeReturns", result.Error.Code);
    }

    [Fact]
    public async Task GetWeek52HighLowAsync_MartThrows_ReturnsFailure()
    {
        _cacheMock.Setup(c => c.GetAsync<List<Week52HighLowDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Week52HighLowDto>?)null);

        _martReaderMock.Setup(m => m.GetWeek52HighLowAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failed"));

        var sut = CreateSut();
        var result = await sut.GetWeek52HighLowAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Dashboard.Week52HighLow", result.Error.Code);
    }
}
