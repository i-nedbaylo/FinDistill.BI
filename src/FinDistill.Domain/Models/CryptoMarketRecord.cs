
/// <summary>
/// Read model for a single cryptocurrency row in the Crypto Market Overview report.
/// Data sourced from CoinGecko /coins/markets endpoint.
/// </summary>
public class CryptoMarketRecord
{
    /// <summary>CoinGecko coin ID (e.g. "bitcoin").</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Coin symbol in uppercase (e.g. "BTC").</summary>
    public string Symbol { get; set; } = string.Empty;
    /// <summary>Human-readable coin name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Current price in the configured vs_currency.</summary>
    public decimal CurrentPrice { get; set; }
    /// <summary>Market capitalisation in USD.</summary>
    public decimal MarketCap { get; set; }
    /// <summary>Market cap rank (1 = largest).</summary>
    public int MarketCapRank { get; set; }
    /// <summary>24-hour trading volume.</summary>
    public decimal TotalVolume { get; set; }
    /// <summary>24-hour price change percentage.</summary>
    public decimal PriceChangePercent24H { get; set; }
    /// <summary>All-time high price.</summary>
    public decimal Ath { get; set; }
    /// <summary>Percentage change from all-time high.</summary>
    public decimal AthChangePercent { get; set; }
    /// <summary>Circulating supply of the coin.</summary>
    public decimal CirculatingSupply { get; set; }
    /// <summary>URL to the coin's image/logo.</summary>
    public string Image { get; set; } = string.Empty;
}
