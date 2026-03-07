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
        // Pre-buffer content before retry loop so it can be replayed on each attempt.
        // After base.SendAsync the original content stream is consumed and cannot be re-read.
        byte[]? contentBytes = null;
        string? contentType = null;
        if (request.Content is not null)
        {
            contentBytes = await request.Content.ReadAsByteArrayAsync(ct);
            contentType = request.Content.Headers.ContentType?.ToString();
        }

        HttpResponseMessage? response = null;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            var msg = attempt == 0 ? request : CloneRequest(request, contentBytes, contentType);

            try
            {
                response = await base.SendAsync(msg, ct);

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

        // Should not reach here — the loop always returns or throws before exiting
        throw new InvalidOperationException("Unreachable code in RetryDelegatingHandler.SendAsync");
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request, byte[]? contentBytes, string? contentType)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (contentBytes is not null)
        {
            clone.Content = new ByteArrayContent(contentBytes);
            if (contentType is not null)
                clone.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var prop in request.Options)
            clone.Options.TryAdd(prop.Key, prop.Value);

        return clone;
    }
}
