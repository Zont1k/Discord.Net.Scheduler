namespace Discord.Net.Scheduler.Scheduling;

public interface IDiscordJob
{
    Task<JobResult> ExecuteAsync(JobContext context, CancellationToken ct = default);
}

public interface IDiscordCronJob : IDiscordJob
{
    string CronExpression { get; }

    string? JobName => GetType().Name;

    TimeZoneInfo? Timezone => null;

    int MaxRetries => 3;

    TimeSpan? RetryDelay => TimeSpan.FromSeconds(30);
}

public interface IDiscordDelayedJob : IDiscordJob
{
    TimeSpan Delay { get; }

    string? JobName => GetType().Name;

    int MaxRetries => 0;

    TimeSpan? RetryDelay => null;
}
