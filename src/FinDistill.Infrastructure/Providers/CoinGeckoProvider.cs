using System.Net;
using System.Text.Json;
using FinDistill.Domain.Enums;
using FinDistill.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinDistill.Infrastructure.Providers;

/// <summary>
/// Market data provider for CoinGecko free API.
/// Fetches historical market data for cryptocurrencies.
/// Retry logic is handled by RetryDelegatingHandler in the HTTP pipeline.
/// </summary>
public class CoinGeckoProvider : IMarketDataProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoProvider> _logger;

    private const string BaseUrl = "https://api.coingecko.com/api/v3";

    public CoinGeckoProvider(HttpClient httpClient, ILogger<CoinGeckoProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public DataSourceType SourceType => DataSourceType.CoinGecko;

    public async Task<string> FetchRawDataAsync(string coinId, CancellationToken ct)
    {
        var encodedCoinId = Uri.EscapeDataString(coinId);
        var url = $"{BaseUrl}/coins/{encodedCoinId}/market_chart?vs_currency=usd&days=5&interval=daily";

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var rawJson = await response.Content.ReadAsStringAsync(ct);

        return ConvertToStandardFormat(rawJson, coinId);
    }

    public async Task<IEnumerable<string>> FetchBulkDataAsync(IEnumerable<string> coinIds, CancellationToken ct)
    {
        var results = new List<string>();

        foreach (var coinId in coinIds)
        {
            try
            {
                var result = await FetchRawDataAsync(coinId, ct);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CoinGecko: failed to fetch {CoinId}, skipping", coinId);
            }
        }

        return results;
    }

    /// <summary>
    /// Converts CoinGecko market_chart response to standardized JSON format.
    /// CoinGecko returns prices as [[timestamp, price], ...] arrays.
    /// </summary>
    private static string ConvertToStandardFormat(string rawJson, string coinId)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var prices = root.GetProperty("prices");
        var totalVolumes = root.TryGetProperty("total_volumes", out var vol) ? vol : default;

        var quotes = new List<object>();

        for (var i = 0; i < prices.GetArrayLength(); i++)
        {
            var priceEntry = prices[i];
            var timestamp = priceEntry[0].GetInt64();
            var price = priceEntry[1].GetDecimal();
            var date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;

            var volume = 0m;
            if (totalVolumes.ValueKind == JsonValueKind.Array && i < totalVolumes.GetArrayLength())
            {
                volume = totalVolumes[i][1].GetDecimal();
            }

            var ticker = coinId.ToUpperInvariant();
            quotes.Add(new
            {
                ticker,
                date = DateOnly.FromDateTime(date).ToString("yyyy-MM-dd"),
                open = price,
                high = price,
                low = price,
                close = price,
                volume
            });
        }

        return JsonSerializer.Serialize(quotes);
    }
}
