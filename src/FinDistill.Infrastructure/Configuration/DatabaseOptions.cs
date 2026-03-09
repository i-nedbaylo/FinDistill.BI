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

    /// <summary>
    /// When true, EF Core migrations are applied automatically on application startup.
    /// Should be enabled only in single-instance deployments (e.g. Railway, Render).
    /// Defaults to false (safe-by-default).
    /// </summary>
    public bool AutoMigrate { get; set; } = false;
}
