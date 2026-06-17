namespace Discord.Net.Scheduler.Scheduling.Locking;

public interface IDistributedLock
{
    Task<bool> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    Task ReleaseAsync(string key, CancellationToken ct = default);
}
