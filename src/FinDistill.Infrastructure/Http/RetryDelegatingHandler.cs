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
        HttpResponseMessage? response = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                response = await base.SendAsync(attempt == 0 ? request : await CloneRequestAsync(request), ct);

                if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt == MaxRetries)
                    return response;

                response.Dispose();

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                _logger.LogWarning("HTTP 429 for {Url}, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
                    request.RequestUri, delay.TotalSeconds, attempt + 1, MaxRetries);
                await Task.Delay(delay, ct);
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                _logger.LogWarning(ex, "HTTP request failed for {Url}, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
                    request.RequestUri, delay.TotalSeconds, attempt + 1, MaxRetries);
                await Task.Delay(delay, ct);
            }
        }

        // Should not reach here, but satisfy compiler
        return response ?? await base.SendAsync(request, ct);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content is not null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var prop in request.Options)
            clone.Options.TryAdd(prop.Key, prop.Value);

        return clone;
    }
}
