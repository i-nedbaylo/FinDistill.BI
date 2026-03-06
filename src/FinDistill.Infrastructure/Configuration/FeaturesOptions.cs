namespace FinDistill.Infrastructure.Configuration;

/// <summary>
/// Feature flags for optional infrastructure components.
/// Bound from "Features" section in appsettings.json.
/// </summary>
public class FeaturesOptions
{
    public const string SectionName = "Features";

    /// <summary>
    /// Enable Redis caching for Data Mart queries.
    /// </summary>
    public bool UseRedis { get; set; }

    /// <summary>
    /// Enable ClickHouse as the Data Mart read engine.
    /// </summary>
    public bool UseClickHouse { get; set; }
}
