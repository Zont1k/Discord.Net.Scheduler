namespace Discord.Net.Scheduler.Scheduling;

public abstract record ScheduledJob : IScheduledJob
{
    protected ScheduledJob(string id, string? name = null)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; init; }
    public string? Name { get; init; }
    public JobStatus Status { get; init; } = JobStatus.Pending;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NextExecution { get; init; }
    public DateTimeOffset? LastExecutedAt { get; init; }
    public int ExecutionCount { get; init; }
    public string? LastError { get; init; }
    public TimeSpan? Interval { get; init; }
    public string? CronExpression { get; init; }
    public TimeZoneInfo? Timezone { get; init; }
    public int MaxRetries { get; init; } = 3;
    public TimeSpan? RetryDelay { get; init; } = TimeSpan.FromSeconds(30);
    public DateTimeOffset? ExpiresAt { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();

    public abstract Task<JobResult> ExecuteAsync(JobContext context, CancellationToken ct = default);
}
