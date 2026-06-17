using Discord.Net.Scheduler.Triggers;

namespace Discord.Net.Scheduler.Scheduling;

public interface IScheduledJob
{
    string Id { get; }
    string? Name { get; }
    JobStatus Status { get; }
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? NextExecution { get; }
    DateTimeOffset? LastExecutedAt { get; }
    int ExecutionCount { get; }
    string? LastError { get; }
    TimeSpan? Interval { get; }
    string? CronExpression { get; }
    TimeZoneInfo? Timezone { get; }
    int MaxRetries { get; }
    TimeSpan? RetryDelay { get; }
    DateTimeOffset? ExpiresAt { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
    IReadOnlyList<IJobTrigger> Triggers { get; }
    IReadOnlyList<string> Dependencies { get; }
    Func<IServiceProvider, Task<bool>>? RunCondition { get; }
    Task<JobResult> ExecuteAsync(JobContext context, CancellationToken ct = default);
}