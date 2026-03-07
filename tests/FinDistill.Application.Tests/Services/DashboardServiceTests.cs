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

        Assert.Single(result);
        Assert.Equal("AAPL", result[0].Ticker);
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

        Assert.Single(result);
        _martReaderMock.Verify(m => m.GetAssetHistoryAsync("AAPL", 30, It.IsAny<CancellationToken>()), Times.Once);
    }
}
