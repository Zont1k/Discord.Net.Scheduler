namespace Discord.Net.Scheduler.Attributes;

/// <summary>
/// Marks a class as a cron-based scheduled job for automatic registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CronJobAttribute : Attribute
{
    /// <summary>
    /// Gets the cron expression that defines the job schedule.
    /// </summary>
    public string CronExpression { get; }

    /// <summary>
    /// Gets or sets the IANA timezone identifier for schedule evaluation.
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts on failure.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay in seconds between retry attempts.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 30;

    /// <param name="cronExpression">A 5-field cron expression.</param>
    public CronJobAttribute(string cronExpression)
    {
        CronExpression = cronExpression;
    }
}