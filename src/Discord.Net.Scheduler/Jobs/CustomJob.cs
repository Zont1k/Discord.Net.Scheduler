using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Jobs;

public sealed record CustomJob : ScheduledJob
{
    private readonly Func<JobContext, CancellationToken, Task<JobResult>> _execution;

    public CustomJob(
        string id,
        Func<JobContext, CancellationToken, Task<JobResult>> execution,
        string? name = null)
        : base(id, name ?? $"Custom-{id}")
    {
        _execution = execution ?? throw new ArgumentNullException(nameof(execution));
    }

    public override Task<JobResult> ExecuteAsync(JobContext context, CancellationToken ct = default)
    {
        return _execution(context, ct);
    }
}
