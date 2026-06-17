using System.Collections.Concurrent;

namespace Discord.Net.Scheduler.Scheduling.Locking;

public sealed class InMemoryLock : IDistributedLock
{
    private static readonly ConcurrentDictionary<string, string> _locks = new();

    public Task<bool> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var instanceId = Guid.NewGuid().ToString("N");
        var added = _locks.TryAdd(key, instanceId);
        return Task.FromResult(added);
    }

    public Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        _locks.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
