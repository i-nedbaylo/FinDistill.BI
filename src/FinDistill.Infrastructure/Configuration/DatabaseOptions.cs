namespace FinDistill.Infrastructure.Configuration;

/// <summary>
/// Configuration options for database provider selection.
/// Bound from "Database" section in appsettings.json.
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Database provider: "SqlServer" or "PostgreSQL".
    /// </summary>
    public string Provider { get; set; } = "SqlServer";
}
