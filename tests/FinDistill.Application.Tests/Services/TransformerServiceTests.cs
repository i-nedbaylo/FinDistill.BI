using FinDistill.Application.Services;
using FinDistill.Domain.Entities;
using FinDistill.Domain.Enums;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinDistill.Application.Tests.Services;

public class TransformerServiceTests
{
    private readonly Mock<IRawIngestDataRepository> _rawRepoMock = new();
    private readonly Mock<ILogger<TransformerService>> _loggerMock = new();

    private TransformerService CreateSut() => new(_rawRepoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task TransformAsync_ValidJson_ReturnsParsedQuotes()
    {
        var json = """
        [
            {"ticker":"AAPL","date":"2024-01-15","open":100.5,"high":102,"low":99,"close":101,"volume":1000000}
        ]
        """;
        
        _rawRepoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawIngestData>
            {
                new() { Id = 1, Source = "YahooFinance", RawContent = json, IsProcessed = false }
            });

        var sut = CreateSut();
        var result = await sut.TransformAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("AAPL", result.Value[0].Ticker);
        Assert.Equal(new DateOnly(2024, 1, 15), result.Value[0].Date);
        Assert.Equal(101m, result.Value[0].Close);
        Assert.Equal(DataSourceType.YahooFinance, result.Value[0].SourceType);
    }

    [Fact]
    public async Task TransformAsync_MultipleQuotesInArray_ReturnsAll()
    {
        var json = """
        [
            {"ticker":"AAPL","date":"2024-01-15","open":100,"high":102,"low":99,"close":101,"volume":1000},
            {"ticker":"MSFT","date":"2024-01-15","open":200,"high":205,"low":198,"close":203,"volume":2000}
        ]
        """;

        _rawRepoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawIngestData>
            {
                new() { Id = 1, Source = "YahooFinance", RawContent = json }
            });

        var sut = CreateSut();
        var result = await sut.TransformAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("AAPL", result.Value[0].Ticker);
        Assert.Equal("MSFT", result.Value[1].Ticker);
    }

    [Fact]
    public async Task TransformAsync_InvalidJson_SkipsRecordAndContinues()
    {
        _rawRepoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawIngestData>
            {
                new() { Id = 1, Source = "YahooFinance", RawContent = "not valid json" },
                new() { Id = 2, Source = "YahooFinance", RawContent = """[{"ticker":"AAPL","date":"2024-01-15","close":100}]""" }
            });

        var sut = CreateSut();
        var result = await sut.TransformAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        _rawRepoMock.Verify(r => r.MarkAsProcessedAsync(
            It.Is<IEnumerable<long>>(ids => ids.Contains(2) && !ids.Contains(1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransformAsync_EmptyTicker_SkipsQuote()
    {
        var json = """[{"ticker":"","date":"2024-01-15","close":100}]""";

        _rawRepoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawIngestData>
            {
                new() { Id = 1, Source = "YahooFinance", RawContent = json }
            });

        var sut = CreateSut();
        var result = await sut.TransformAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task TransformAsync_InvalidDate_SkipsQuote()
    {
        var json = """[{"ticker":"AAPL","date":"not-a-date","close":100}]""";

        _rawRepoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawIngestData>
            {
                new() { Id = 1, Source = "YahooFinance", RawContent = json }
            });

        var sut = CreateSut();
        var result = await sut.TransformAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task TransformAsync_NoUnprocessedRecords_ReturnsEmpty()
    {
        _rawRepoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawIngestData>());

        var sut = CreateSut();
        var result = await sut.TransformAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        _rawRepoMock.Verify(r => r.MarkAsProcessedAsync(
            It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransformAsync_MissingOptionalFields_DefaultsToZero()
    {
        var json = """[{"ticker":"AAPL","date":"2024-01-15"}]""";

        _rawRepoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawIngestData>
            {
                new() { Id = 1, Source = "YahooFinance", RawContent = json }
            });

        var sut = CreateSut();
        var result = await sut.TransformAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(0m, result.Value[0].Open);
        Assert.Equal(0m, result.Value[0].High);
        Assert.Equal(0m, result.Value[0].Low);
        Assert.Equal(0m, result.Value[0].Close);
        Assert.Equal(0m, result.Value[0].Volume);
    }

    [Fact]
    public async Task TransformAsync_SingleObject_NotArray_ParsesCorrectly()
    {
        var json = """{"ticker":"AAPL","date":"2024-01-15","close":150}""";

        _rawRepoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawIngestData>
            {
                new() { Id = 1, Source = "CoinGecko", RawContent = json }
            });

        var sut = CreateSut();
        var result = await sut.TransformAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(DataSourceType.CoinGecko, result.Value[0].SourceType);
    }

    [Fact]
    public async Task TransformAsync_UnknownSource_SkipsRecord()
    {
        var json = """[{"ticker":"AAPL","date":"2024-01-15","close":100}]""";

        _rawRepoMock.Setup(r => r.GetUnprocessedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawIngestData>
            {
                new() { Id = 1, Source = "UnknownSource", RawContent = json }
            });

        var sut = CreateSut();
        var result = await sut.TransformAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
