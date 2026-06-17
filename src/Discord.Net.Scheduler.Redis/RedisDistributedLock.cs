using Discord.Net.Scheduler.Scheduling.Locking;
using StackExchange.Redis;

namespace Discord.Net.Scheduler.Redis;

public sealed class RedisDistributedLock : IDistributedLock
{
    private readonly IDatabase _db;
    private readonly string _keyPrefix;

    public RedisDistributedLock(IConnectionMultiplexer multiplexer, string keyPrefix = "scheduler:lock:")
    {
        _db = multiplexer.GetDatabase();
        _keyPrefix = keyPrefix;
    }

    public async Task<bool> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        return await _db.LockTakeAsync($"{_keyPrefix}{key}", Environment.MachineName, expiry);
    }

    public async Task ReleaseAsync(string key, CancellationToken ct = default)
    {
        await _db.LockReleaseAsync($"{_keyPrefix}{key}", Environment.MachineName);
    }
}
