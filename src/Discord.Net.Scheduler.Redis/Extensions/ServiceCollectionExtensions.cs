using Discord.Net.Scheduler.Scheduling.JobStore;
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
}
