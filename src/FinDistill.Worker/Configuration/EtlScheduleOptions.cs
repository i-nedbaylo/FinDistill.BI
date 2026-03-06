namespace FinDistill.Worker.Configuration;

/// <summary>
/// Configuration options for ETL schedule.
/// Bound from "EtlSchedule" section in appsettings.json.
/// </summary>
public class EtlScheduleOptions
{
    public const string SectionName = "EtlSchedule";

    /// <summary>
    /// Interval in minutes between ETL pipeline runs.
    /// </summary>
    public int IntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Optional CRON expression. If set, overrides IntervalMinutes.
    /// Currently reserved for future use.
    /// </summary>
    public string? CronExpression { get; set; }
}
