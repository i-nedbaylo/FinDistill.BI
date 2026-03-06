using System.Net;
using System.Text.Json;
using FinDistill.Domain.Enums;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinDistill.Infrastructure.Providers;

/// <summary>
/// Market data provider for Yahoo Finance.
/// Uses Yahoo Finance v8 API for historical quotes.
/// Implements exponential backoff for HTTP 429 responses.
/// </summary>
public class YahooFinanceProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooFinanceProvider> _logger;

    private const string BaseUrl = "https://query1.finance.yahoo.com/v8/finance/chart";
    private const int MaxRetries = 3;

    public YahooFinanceProvider(HttpClient httpClient, ILogger<YahooFinanceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public DataSourceType SourceType => DataSourceType.YahooFinance;

    public async Task<string> FetchRawDataAsync(string ticker, CancellationToken ct)
    {
        var url = $"{BaseUrl}/{ticker}?range=5d&interval=1d";

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, ct);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                    _logger.LogWarning("Yahoo Finance rate limited for {Ticker}, retrying in {Delay}s (attempt {Attempt}/{MaxRetries})",
                        ticker, delay.TotalSeconds, attempt + 1, MaxRetries);
                    await Task.Delay(delay, ct);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                var rawJson = await response.Content.ReadAsStringAsync(ct);

                return ConvertToStandardFormat(rawJson, ticker);
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                _logger.LogWarning(ex, "Yahoo Finance request failed for {Ticker}, retrying in {Delay}s", ticker, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }

        throw new InvalidOperationException($"Failed to fetch data from Yahoo Finance for {ticker} after {MaxRetries} retries.");
    }

    public async Task<IEnumerable<string>> FetchBulkDataAsync(IEnumerable<string> tickers, CancellationToken ct)
    {
        var results = new List<string>();

        foreach (var ticker in tickers)
        {
            try
            {
                var result = await FetchRawDataAsync(ticker, ct);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yahoo Finance: failed to fetch {Ticker}, skipping", ticker);
            }
        }

        return results;
    }

    /// <summary>
    /// Converts Yahoo Finance API response to standardized JSON format.
    /// </summary>
    private static string ConvertToStandardFormat(string rawJson, string ticker)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var result = root.GetProperty("chart").GetProperty("result");
        if (result.GetArrayLength() == 0)
            return "[]";

        var first = result[0];
        var timestamps = first.GetProperty("timestamp");
        var indicators = first.GetProperty("indicators").GetProperty("quote")[0];

        var quotes = new List<object>();

        for (var i = 0; i < timestamps.GetArrayLength(); i++)
        {
            var unixTime = timestamps[i].GetInt64();
            var date = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;

            quotes.Add(new
            {
                ticker,
                date = DateOnly.FromDateTime(date).ToString("yyyy-MM-dd"),
                open = GetDecimalOrZero(indicators, "open", i),
                high = GetDecimalOrZero(indicators, "high", i),
                low = GetDecimalOrZero(indicators, "low", i),
                close = GetDecimalOrZero(indicators, "close", i),
                volume = GetDecimalOrZero(indicators, "volume", i)
            });
        }

        return JsonSerializer.Serialize(quotes);
    }

    private static decimal GetDecimalOrZero(JsonElement indicators, string property, int index)
    {
        if (!indicators.TryGetProperty(property, out var arr))
            return 0;

        var element = arr[index];
        return element.ValueKind == JsonValueKind.Null ? 0 : element.GetDecimal();
    }
}
