using System.Net;
using Microsoft.Extensions.Logging;

namespace FinDistill.Infrastructure.Http;

/// <summary>
/// HTTP message handler that retries requests on transient failures and HTTP 429 (Too Many Requests)
/// with exponential backoff. Replaces duplicated retry logic in individual API providers.
/// </summary>
public class RetryDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<RetryDelegatingHandler> _logger;
    private const int MaxRetries = 3;

    public RetryDelegatingHandler(ILogger<RetryDelegatingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxRetries + 1; attempt++)
        {
            try
            {
                var response = await base.SendAsync(request, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (attempt < MaxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                        _logger.LogWarning("HTTP 429 for {Url}, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
                            request.RequestUri, delay.TotalSeconds, attempt + 1, MaxRetries);
                        await Task.Delay(delay, ct);
                        continue;
                    }
                }

                return response;
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                _logger.LogWarning(ex, "HTTP request failed for {Url}, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
                    request.RequestUri, delay.TotalSeconds, attempt + 1, MaxRetries);
                await Task.Delay(delay, ct);
            }
        }

        // Final attempt — let it throw naturally
        return await base.SendAsync(request, ct);
    }
}
