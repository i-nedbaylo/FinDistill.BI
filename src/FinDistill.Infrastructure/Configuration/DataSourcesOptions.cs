namespace FinDistill.Infrastructure.Configuration;

/// <summary>
/// Configuration options for data source tickers/coin IDs.
/// Bound from "DataSources" section in appsettings.json.
/// </summary>
public class DataSourcesOptions
{
    public const string SectionName = "DataSources";

    /// <summary>Options for the Yahoo Finance data source.</summary>
    public YahooFinanceOptions YahooFinance { get; set; } = new();
    /// <summary>Options for the CoinGecko data source.</summary>
    public CoinGeckoOptions CoinGecko { get; set; } = new();
}

/// <summary>Configuration for Yahoo Finance market data ingestion.</summary>
public class YahooFinanceOptions
{
    /// <summary>Whether this source is enabled for ETL extraction.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>List of ticker symbols to fetch (e.g. "AAPL", "SPY").</summary>
    public List<string> Tickers { get; set; } = [];
    /// <summary>Number of calendar days of history to fetch per run. Maps to Yahoo range parameter (e.g. 5 → "5d", 365 → "1y").</summary>
    public int HistoryDays { get; set; } = 365;
    /// <summary>Delay between individual ticker requests in milliseconds to avoid HTTP 429.</summary>
    public int RequestDelayMs { get; set; } = 2000;
}

/// <summary>Configuration for CoinGecko cryptocurrency data ingestion.</summary>
public class CoinGeckoOptions
{
    /// <summary>Whether this source is enabled for ETL extraction.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>List of CoinGecko coin IDs to fetch (e.g. "bitcoin", "ethereum").</summary>
    public List<string> CoinIds { get; set; } = [];
    /// <summary>Currency to quote prices in (e.g. "usd").</summary>
    public string VsCurrency { get; set; } = "usd";
    /// <summary>Optional Demo API key (x-cg-demo-api-key). Required for free-tier access.</summary>
    public string? ApiKey { get; set; }
    /// <summary>Number of calendar days of history to fetch per run.</summary>
    public int HistoryDays { get; set; } = 365;
    /// <summary>Delay between individual coin requests in milliseconds to avoid HTTP 429.</summary>
    public int RequestDelayMs { get; set; } = 1500;
}
