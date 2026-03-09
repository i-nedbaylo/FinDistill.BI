namespace FinDistill.Application.Configuration;

/// <summary>
/// Configuration options for the ETL schedule.
/// Bound from the "EtlSchedule" section in appsettings.json.
/// Used by both the dedicated Worker process and the in-process ETL worker inside Web.
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
