using Discord.Net.Scheduler.Scheduling.JobStore;
using Discord.Net.Scheduler.Scheduling.Locking;
using Discord.Net.Scheduler.Redis;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedisJobStore(this IServiceCollection services, string connectionString, string keyPrefix = "scheduler:jobs:")
    {
        services.AddSingleton<IJobStore>(_ => new RedisJobStore(connectionString, keyPrefix));
        return services;
    }

    public static IServiceCollection AddRedisJobStore(this IServiceCollection services, ConnectionMultiplexer redis, string keyPrefix = "scheduler:jobs:")
    {
        services.AddSingleton<IJobStore>(_ => new RedisJobStore(redis, keyPrefix));
        return services;
    }

    public static IServiceCollection AddRedisDistributedLock(this IServiceCollection services, string connectionString, string keyPrefix = "scheduler:lock:")
    {
        var multiplexer = ConnectionMultiplexer.Connect(connectionString);
        return services.AddDistributedLock<RedisDistributedLock>(_ => new RedisDistributedLock(multiplexer, keyPrefix));
    }

    public static IServiceCollection AddRedisDistributedLock(this IServiceCollection services, IConnectionMultiplexer multiplexer, string keyPrefix = "scheduler:lock:")
    {
        return services.AddDistributedLock<RedisDistributedLock>(_ => new RedisDistributedLock(multiplexer, keyPrefix));
    }
}

