namespace FinDistill.Application.DTOs;

/// <summary>DTO for a single coin row in the Crypto Market Overview page.</summary>
public class CryptoMarketDto
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal MarketCap { get; set; }
    public int MarketCapRank { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal PriceChangePercent24H { get; set; }
    public decimal Ath { get; set; }
    public decimal AthChangePercent { get; set; }
    public decimal CirculatingSupply { get; set; }
    public string Image { get; set; } = string.Empty;
}
