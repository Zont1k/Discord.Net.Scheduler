namespace Discord.Net.Scheduler.Scheduling.JobStore;

public interface IJobStore
{
    Task AddAsync(IScheduledJob job, CancellationToken ct = default);

    Task RemoveAsync(string jobId, CancellationToken ct = default);

    Task<IScheduledJob?> GetAsync(string jobId, CancellationToken ct = default);

    Task<IReadOnlyList<IScheduledJob>> GetPendingAsync(CancellationToken ct = default);

    Task<IReadOnlyList<IScheduledJob>> GetAllAsync(CancellationToken ct = default);

    Task UpdateAsync(IScheduledJob job, CancellationToken ct = default);

    Task MarkCompletedAsync(string jobId, CancellationToken ct = default);

    Task MarkFailedAsync(string jobId, string error, CancellationToken ct = default);

    Task<int> CountAsync(CancellationToken ct = default);

    Task ClearAsync(CancellationToken ct = default);
}
