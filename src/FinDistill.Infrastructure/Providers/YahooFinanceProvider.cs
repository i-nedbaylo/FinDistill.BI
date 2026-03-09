using System.Net;
using System.Text.Json;
using FinDistill.Domain.Enums;
using FinDistill.Domain.Interfaces;
using FinDistill.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinDistill.Infrastructure.Providers;

/// <summary>
/// Market data provider for Yahoo Finance.
/// Uses Yahoo Finance v8 API for historical quotes.
/// Retry logic is handled by RetryDelegatingHandler in the HTTP pipeline.
/// </summary>
public class YahooFinanceProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooFinanceProvider> _logger;
    private readonly YahooFinanceOptions _options;

    private const string BaseUrl = "https://query2.finance.yahoo.com/v8/finance/chart";

    public YahooFinanceProvider(
        HttpClient httpClient,
        ILogger<YahooFinanceProvider> logger,
        IOptions<DataSourcesOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value.YahooFinance;
    }

    public DataSourceType SourceType => DataSourceType.YahooFinance;

    public async Task<string> FetchRawDataAsync(string ticker, CancellationToken ct)
    {
        var encodedTicker = Uri.EscapeDataString(ticker);
        var url = $"{BaseUrl}/{encodedTicker}?range=1y&interval=1d";

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var rawJson = await response.Content.ReadAsStringAsync(ct);

        return ConvertToStandardFormat(rawJson, ticker);
    }

    public async Task<IEnumerable<string>> FetchBulkDataAsync(IEnumerable<string> tickers, CancellationToken ct)
    {
        var results = new List<string>();
        var tickerList = tickers.ToList();

        for (var i = 0; i < tickerList.Count; i++)
        {
            var ticker = tickerList[i];
            try
            {
                var result = await FetchRawDataAsync(ticker, ct);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yahoo Finance: failed to fetch {Ticker}, skipping", ticker);
            }

            // Throttle requests to avoid 429, skip delay after the last ticker
            if (i < tickerList.Count - 1 && _options.RequestDelayMs > 0)
            {
                await Task.Delay(_options.RequestDelayMs, ct);
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

        if (index < 0 || index >= arr.GetArrayLength())
            return 0;

        var element = arr[index];
        return element.ValueKind == JsonValueKind.Null ? 0 : element.GetDecimal();
    }
}
