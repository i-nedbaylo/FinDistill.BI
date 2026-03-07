namespace FinDistill.Infrastructure.Configuration;

/// <summary>
/// Configuration options for data source tickers/coin IDs.
/// Bound from "DataSources" section in appsettings.json.
/// </summary>
public class DataSourcesOptions
{
    public const string SectionName = "DataSources";

    public YahooFinanceOptions YahooFinance { get; set; } = new();
    public CoinGeckoOptions CoinGecko { get; set; } = new();
}

public class YahooFinanceOptions
{
    public bool Enabled { get; set; } = true;
    public List<string> Tickers { get; set; } = [];
    /// <summary>Delay between individual ticker requests in milliseconds to avoid 429.</summary>
    public int RequestDelayMs { get; set; } = 1000;
}

public class CoinGeckoOptions
{
    public bool Enabled { get; set; } = true;
    public List<string> CoinIds { get; set; } = [];
    public string VsCurrency { get; set; } = "usd";
    /// <summary>Optional Demo API key (x-cg-demo-api-key). Required for free-tier access.</summary>
    public string? ApiKey { get; set; }
    /// <summary>Delay between individual coin requests in milliseconds to avoid 429.</summary>
    public int RequestDelayMs { get; set; } = 1500;
}
