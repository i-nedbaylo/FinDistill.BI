namespace FinDistill.Infrastructure.Configuration;

/// <summary>
/// Configuration options for ClickHouse connection.
/// ConnectionString is populated from "ConnectionStrings:ClickHouse" in appsettings.json.
/// </summary>
public class ClickHouseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
