namespace FinDistill.Infrastructure.Configuration;

/// <summary>
/// Configuration options for ClickHouse connection.
/// Bound from "ConnectionStrings:ClickHouse" in appsettings.json.
/// </summary>
public class ClickHouseOptions
{
    public const string SectionName = "ClickHouse";

    public string ConnectionString { get; set; } = string.Empty;
}
