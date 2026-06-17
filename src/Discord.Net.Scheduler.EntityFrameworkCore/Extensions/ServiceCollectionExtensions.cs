using Discord.Net.Scheduler.EntityFrameworkCore;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class EfCoreServiceCollectionExtensions
{
    public static IServiceCollection AddEfCoreJobStore<TContext>(this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TContext : SchedulerDbContext
    {
        services.AddDbContextFactory<TContext>(optionsAction);
        services.AddSingleton<IJobStore, EfJobStore>();
        return services;
    }

    public static IServiceCollection AddEfCoreJobStore<TContext>(this IServiceCollection services)
        where TContext : SchedulerDbContext
    {
        services.AddSingleton<IJobStore, EfJobStore>();
        return services;
    }
}
