using System.Net;
using FinDistill.Infrastructure.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace FinDistill.Infrastructure.Tests.Http;

public class RetryDelegatingHandlerTests
{
    private readonly Mock<ILogger<RetryDelegatingHandler>> _loggerMock = new();

    private HttpClient CreateClientWithMockHandler(Mock<HttpMessageHandler> innerHandler)
    {
        var retryHandler = new RetryDelegatingHandler(_loggerMock.Object)
        {
            InnerHandler = innerHandler.Object
        };
        return new HttpClient(retryHandler);
    }

    [Fact]
    public async Task SendAsync_200_ReturnsWithoutRetry()
    {
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var client = CreateClientWithMockHandler(innerHandler);
        var response = await client.GetAsync("https://example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        innerHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_429ThenSuccess_RetriesAndReturns()
    {
        var callCount = 0;
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    : new HttpResponseMessage(HttpStatusCode.OK);
            });

        var client = CreateClientWithMockHandler(innerHandler);
        var response = await client.GetAsync("https://example.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task SendAsync_500_DoesNotRetry()
    {
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = CreateClientWithMockHandler(innerHandler);
        var response = await client.GetAsync("https://example.com");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        innerHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}
