using System.Text.Json;
using FinDistill.Domain.Interfaces;
using FinDistill.Domain.Models;
using FinDistill.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinDistill.Infrastructure.Providers;

/// <summary>
/// Fetches live cryptocurrency market overview from CoinGecko /coins/markets endpoint.
/// Uses the same HttpClient (with RetryDelegatingHandler) as CoinGeckoProvider.
/// </summary>
public class CoinGeckoMarketProvider : ICryptoMarketProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoMarketProvider> _logger;
    private readonly CoinGeckoOptions _options;

    private const string BaseUrl = "https://api.coingecko.com/api/v3";

    public CoinGeckoMarketProvider(
        HttpClient httpClient,
        ILogger<CoinGeckoMarketProvider> logger,
        IOptions<DataSourcesOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value.CoinGecko;

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-cg-demo-api-key", _options.ApiKey);
    }

    public async Task<IReadOnlyList<CryptoMarketRecord>> GetMarketOverviewAsync(
        string vsCurrency, int limit, CancellationToken ct)
    {
        var clampedLimit = Math.Clamp(limit, 1, 250);
        var url = $"{BaseUrl}/coins/markets"
                + $"?vs_currency={Uri.EscapeDataString(vsCurrency)}"
                + $"&order=market_cap_desc"
                + $"&per_page={clampedLimit}"
                + $"&page=1"
                + $"&sparkline=false"
                + $"&price_change_percentage=24h";

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);

        return ParseMarketResponse(json);
    }

    private static IReadOnlyList<CryptoMarketRecord> ParseMarketResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new List<CryptoMarketRecord>();

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            result.Add(new CryptoMarketRecord
            {
                Id = item.GetStringOrEmpty("id"),
                Symbol = item.GetStringOrEmpty("symbol").ToUpperInvariant(),
                Name = item.GetStringOrEmpty("name"),
                Image = item.GetStringOrEmpty("image"),
                CurrentPrice = item.GetDecimalOrZero("current_price"),
                MarketCap = item.GetDecimalOrZero("market_cap"),
                MarketCapRank = item.TryGetProperty("market_cap_rank", out var rankEl) && rankEl.ValueKind != JsonValueKind.Null
                    ? rankEl.GetInt32() : 0,
                TotalVolume = item.GetDecimalOrZero("total_volume"),
                PriceChangePercent24H = item.GetDecimalOrZero("price_change_percentage_24h"),
                Ath = item.GetDecimalOrZero("ath"),
                AthChangePercent = item.GetDecimalOrZero("ath_change_percentage"),
                CirculatingSupply = item.GetDecimalOrZero("circulating_supply")
            });
        }

        return result;
    }
}

/// <summary>Extension helpers for JsonElement to reduce boilerplate in provider parsing.</summary>
file static class JsonElementExtensions
{
    public static string GetStringOrEmpty(this JsonElement el, string property)
        => el.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? string.Empty : string.Empty;

    public static decimal GetDecimalOrZero(this JsonElement el, string property)
    {
        if (!el.TryGetProperty(property, out var p) || p.ValueKind == JsonValueKind.Null)
            return 0m;
        return p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : 0m;
    }
}
