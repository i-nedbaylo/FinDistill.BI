using FinDistill.Application.DTOs;
using FinDistill.Application.Interfaces;
using FinDistill.Application.Services;
using FinDistill.Domain.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinDistill.Application.Tests.Services;

public class EtlOrchestratorTests
{
    private readonly Mock<IExtractorService> _extractorMock = new();
    private readonly Mock<ITransformerService> _transformerMock = new();
    private readonly Mock<ILoaderService> _loaderMock = new();
    private readonly Mock<ILogger<EtlOrchestrator>> _loggerMock = new();

    private EtlOrchestrator CreateSut() => new(
        _extractorMock.Object, _transformerMock.Object, _loaderMock.Object, _loggerMock.Object);

    [Fact]
    public async Task RunEtlPipelineAsync_CallsExtractThenTransformThenLoad()
    {
        var callOrder = new List<string>();

        _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Extract"))
            .ReturnsAsync(Result.Success());
        _transformerMock.Setup(t => t.TransformAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Transform"))
            .ReturnsAsync(Result.Success<IReadOnlyList<ParsedQuoteDto>>(new List<ParsedQuoteDto> { new() }));
        _loaderMock.Setup(l => l.LoadAsync(It.IsAny<IEnumerable<ParsedQuoteDto>>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Load"))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();
        var result = await sut.RunEtlPipelineAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Extract", "Transform", "Load"], callOrder);
    }

    [Fact]
    public async Task RunEtlPipelineAsync_NoData_SkipsLoad()
    {
        _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _transformerMock.Setup(t => t.TransformAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ParsedQuoteDto>>(new List<ParsedQuoteDto>()));

        var sut = CreateSut();
        var result = await sut.RunEtlPipelineAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        _loaderMock.Verify(l => l.LoadAsync(
            It.IsAny<IEnumerable<ParsedQuoteDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunEtlPipelineAsync_OperationCanceled_Rethrows()
    {
        _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var sut = CreateSut();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.RunEtlPipelineAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RunEtlPipelineAsync_GenericException_ReturnsFailure()
    {
        _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var sut = CreateSut();
        var result = await sut.RunEtlPipelineAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Etl.UnhandledException", result.Error.Code);
    }

    [Fact]
    public async Task RunEtlPipelineAsync_TransformFailure_ReturnsFailure()
    {
        _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _transformerMock.Setup(t => t.TransformAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<ParsedQuoteDto>>(new Error("Transform.Failed", "test")));

        var sut = CreateSut();
        var result = await sut.RunEtlPipelineAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Transform.Failed", result.Error.Code);
        _loaderMock.Verify(l => l.LoadAsync(
            It.IsAny<IEnumerable<ParsedQuoteDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunEtlPipelineAsync_ExtractPartialFailure_ContinuesButReturnsFailure()
    {
        var callOrder = new List<string>();

        _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Extract"))
            .ReturnsAsync(Result.Failure(new Error("Extract.PartialFailure", "One provider failed")));
        _transformerMock.Setup(t => t.TransformAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Transform"))
            .ReturnsAsync(Result.Success<IReadOnlyList<ParsedQuoteDto>>(new List<ParsedQuoteDto> { new() }));
        _loaderMock.Setup(l => l.LoadAsync(It.IsAny<IEnumerable<ParsedQuoteDto>>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Load"))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();
        var result = await sut.RunEtlPipelineAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Extract.PartialFailure", result.Error.Code);
        Assert.Equal(["Extract", "Transform", "Load"], callOrder);
    }
}
