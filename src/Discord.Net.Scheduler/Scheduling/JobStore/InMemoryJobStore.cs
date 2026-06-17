using System.Collections.Concurrent;

namespace Discord.Net.Scheduler.Scheduling.JobStore;

public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<string, IScheduledJob> _jobs = new(StringComparer.Ordinal);

    public Task AddAsync(IScheduledJob job, CancellationToken ct = default)
    {
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string jobId, CancellationToken ct = default)
    {
        _jobs.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }

    public Task<IScheduledJob?> GetAsync(string jobId, CancellationToken ct = default)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<IReadOnlyList<IScheduledJob>> GetPendingAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var pending = _jobs.Values
            .Where(j => j.Status == JobStatus.Pending && j.NextExecution <= now)
            .ToArray();

        return Task.FromResult<IReadOnlyList<IScheduledJob>>(pending);
    }

    public Task<IReadOnlyList<IScheduledJob>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<IScheduledJob>>(_jobs.Values.ToArray());
    }

    public Task UpdateAsync(IScheduledJob job, CancellationToken ct = default)
    {
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task MarkCompletedAsync(string jobId, CancellationToken ct = default)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job is ScheduledJob scheduled)
        {
            _jobs[jobId] = scheduled with
            {
                Status = JobStatus.Completed,
                LastExecutedAt = DateTimeOffset.UtcNow
            };
        }

        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(string jobId, string error, CancellationToken ct = default)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job is ScheduledJob scheduled)
        {
            _jobs[jobId] = scheduled with
            {
                Status = JobStatus.Failed,
                LastExecutedAt = DateTimeOffset.UtcNow,
                LastError = error
            };
        }

        return Task.CompletedTask;
    }

    public Task<int> CountAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_jobs.Count);
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        _jobs.Clear();
        return Task.CompletedTask;
    }
}
