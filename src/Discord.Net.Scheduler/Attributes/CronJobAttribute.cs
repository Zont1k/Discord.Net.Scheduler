namespace Discord.Net.Scheduler.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CronJobAttribute : Attribute
{
    public string CronExpression { get; }

    public string? Timezone { get; set; }

    public int MaxRetries { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 30;

    public CronJobAttribute(string cronExpression)
    {
        CronExpression = cronExpression;
    }
}
