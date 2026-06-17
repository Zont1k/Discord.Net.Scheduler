using Discord.Net.Scheduler.Jobs;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;
using StackExchange.Redis;

namespace Discord.Net.Scheduler.Redis;

public sealed class RedisJobStore : IJobStore, IAsyncDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _keyPrefix;

    public RedisJobStore(string connectionString, string keyPrefix = "scheduler:jobs:")
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
        _keyPrefix = keyPrefix;
    }

    public RedisJobStore(ConnectionMultiplexer redis, string keyPrefix = "scheduler:jobs:")
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _keyPrefix = keyPrefix;
    }

    private string JobKey(string jobId) => $"{_keyPrefix}{jobId}";

    private string PendingSetKey => $"{_keyPrefix}pending";

    private string ScheduleSetKey => $"{_keyPrefix}schedule";

    public async Task AddAsync(IScheduledJob job, CancellationToken ct = default)
    {
        var wrapper = new JobWrapper(job);
        var json = wrapper.Serialize();
        var key = JobKey(job.Id);

        var transaction = _db.CreateTransaction();
        _ = transaction.StringSetAsync(key, json);
        _ = transaction.SetAddAsync(PendingSetKey, job.Id);

        if (job.NextExecution.HasValue)
        {
            _ = transaction.SortedSetAddAsync(ScheduleSetKey, job.Id, job.NextExecution.Value.ToUnixTimeSeconds());
        }

        await transaction.ExecuteAsync();
    }

    public async Task RemoveAsync(string jobId, CancellationToken ct = default)
    {
        var transaction = _db.CreateTransaction();
        _ = transaction.KeyDeleteAsync(JobKey(jobId));
        _ = transaction.SetRemoveAsync(PendingSetKey, jobId);
        _ = transaction.SortedSetRemoveAsync(ScheduleSetKey, jobId);
        await transaction.ExecuteAsync();
    }

    public async Task<IScheduledJob?> GetAsync(string jobId, CancellationToken ct = default)
    {
        var json = await _db.StringGetAsync(JobKey(jobId));
        if (!json.HasValue) return null;

        var wrapper = JobWrapper.Deserialize(json!);
        return wrapper?.ToJob();
    }

    public async Task<IReadOnlyList<IScheduledJob>> GetPendingAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var dueIds = await _db.SortedSetRangeByScoreAsync(ScheduleSetKey, 0, now);

        var jobs = new List<IScheduledJob>();
        foreach (var id in dueIds)
        {
            var json = await _db.StringGetAsync(JobKey(id!));
            if (!json.HasValue) continue;

            var wrapper = JobWrapper.Deserialize(json!);
            var job = wrapper?.ToJob();
            if (job is not null && job.Status == JobStatus.Pending)
                jobs.Add(job);
        }

        return jobs;
    }

    public async Task<IReadOnlyList<IScheduledJob>> GetAllAsync(CancellationToken ct = default)
    {
        var ids = await _db.SetMembersAsync(PendingSetKey);
        var jobs = new List<IScheduledJob>();

        foreach (var id in ids)
        {
            var json = await _db.StringGetAsync(JobKey(id!));
            if (!json.HasValue) continue;

            var wrapper = JobWrapper.Deserialize(json!);
            var job = wrapper?.ToJob();
            if (job is not null)
                jobs.Add(job);
        }

        return jobs;
    }

    public async Task UpdateAsync(IScheduledJob job, CancellationToken ct = default)
    {
        var wrapper = new JobWrapper(job);
        var json = wrapper.Serialize();
        await _db.StringSetAsync(JobKey(job.Id), json);

        if (job.NextExecution.HasValue)
        {
            await _db.SortedSetAddAsync(ScheduleSetKey, job.Id, job.NextExecution.Value.ToUnixTimeSeconds());
        }
    }

    public async Task MarkCompletedAsync(string jobId, CancellationToken ct = default)
    {
        var job = await GetAsync(jobId, ct);
        if (job is null) return;

        var updated = (ScheduledJob)job with
        {
            Status = JobStatus.Completed,
            LastExecutedAt = DateTimeOffset.UtcNow
        };

        await UpdateAsync(updated, ct);
    }

    public async Task MarkFailedAsync(string jobId, string error, CancellationToken ct = default)
    {
        var job = await GetAsync(jobId, ct);
        if (job is null) return;

        var updated = (ScheduledJob)job with
        {
            Status = JobStatus.Failed,
            LastExecutedAt = DateTimeOffset.UtcNow,
            LastError = error
        };

        await UpdateAsync(updated, ct);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        return (int)await _db.SetLengthAsync(PendingSetKey);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        var ids = await _db.SetMembersAsync(PendingSetKey);
        var transaction = _db.CreateTransaction();

        foreach (var id in ids)
        {
            _ = transaction.KeyDeleteAsync(JobKey(id!));
        }

        _ = transaction.KeyDeleteAsync(PendingSetKey);
        _ = transaction.KeyDeleteAsync(ScheduleSetKey);
        await transaction.ExecuteAsync();
    }

    public ValueTask DisposeAsync()
    {
        _redis.Dispose();
        return ValueTask.CompletedTask;
    }
}
