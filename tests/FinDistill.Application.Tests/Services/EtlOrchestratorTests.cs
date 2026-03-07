using FinDistill.Application.DTOs;
using FinDistill.Application.Interfaces;
using FinDistill.Application.Services;
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
            .Returns(Task.CompletedTask);
        _transformerMock.Setup(t => t.TransformAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Transform"))
            .ReturnsAsync(new List<ParsedQuoteDto> { new() });
        _loaderMock.Setup(l => l.LoadAsync(It.IsAny<IEnumerable<ParsedQuoteDto>>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Load"))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RunEtlPipelineAsync(CancellationToken.None);

        Assert.Equal(["Extract", "Transform", "Load"], callOrder);
    }

    [Fact]
    public async Task RunEtlPipelineAsync_NoData_SkipsLoad()
    {
        _transformerMock.Setup(t => t.TransformAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ParsedQuoteDto>());

        var sut = CreateSut();
        await sut.RunEtlPipelineAsync(CancellationToken.None);

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
    public async Task RunEtlPipelineAsync_GenericException_DoesNotRethrow()
    {
        _extractorMock.Setup(e => e.ExtractAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var sut = CreateSut();

        // Should not throw — exception is caught and logged
        await sut.RunEtlPipelineAsync(CancellationToken.None);
    }
}
