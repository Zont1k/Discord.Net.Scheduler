namespace Discord.Net.Scheduler.Scheduling;

public sealed class SchedulerOptions
{
    public int PollingIntervalMs { get; set; } = 1000;

    public bool RecoverJobsOnStart { get; set; } = true;

    public TimeZoneInfo DefaultTimezone { get; set; } = TimeZoneInfo.Utc;

    public bool EnableMetrics { get; set; } = true;

    public bool EnableAutoCleanup { get; set; } = false;

    public TimeSpan CompletedJobRetention { get; set; } = TimeSpan.FromDays(7);
}
