using Discord.Net.Scheduler.Pipeline;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;
using Discord.Net.Scheduler.Telemetry;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordScheduler(
        this IServiceCollection services,
        Action<SchedulerOptions>? configureOptions = null)
    {
        services.AddOptions<SchedulerOptions>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.AddSingleton<IJobStore, InMemoryJobStore>();
        services.AddSingleton<JobExecutionPipeline>();
        services.AddSingleton<SchedulerMetrics>();
        services.AddSingleton<JobScheduler>();

        services.AddHostedService(sp => sp.GetRequiredService<JobScheduler>());

        return services;
    }

    public static IServiceCollection AddDiscordScheduler(
        this IServiceCollection services,
        Action<SchedulerOptions, IServiceProvider> configureOptions)
    {
        services.AddOptions<SchedulerOptions>().Configure<IServiceProvider>((opts, sp) =>
        {
            configureOptions(opts, sp);
        });

        services.AddSingleton<IJobStore, InMemoryJobStore>();
        services.AddSingleton<JobExecutionPipeline>();
        services.AddSingleton<SchedulerMetrics>();
        services.AddSingleton<JobScheduler>();

        services.AddHostedService(sp => sp.GetRequiredService<JobScheduler>());

        return services;
    }

    public static IServiceCollection AddJobStore<TStore>(
        this IServiceCollection services)
        where TStore : class, IJobStore
    {
        services.Remove(services.FirstOrDefault(d =>
            d.ServiceType == typeof(IJobStore))!);

        services.AddSingleton<IJobStore, TStore>();
        return services;
    }

    public static IServiceCollection AddJobStore<TStore>(
        this IServiceCollection services,
        TStore instance)
        where TStore : class, IJobStore
    {
        services.Remove(services.FirstOrDefault(d =>
            d.ServiceType == typeof(IJobStore))!);

        services.AddSingleton<IJobStore>(instance);
        return services;
    }

    public static IServiceCollection AddCronJob<TJob>(
        this IServiceCollection services)
        where TJob : class, IDiscordCronJob
    {
        services.AddTransient<TJob>();

        services.AddSingleton<ICronJobRegistration>(sp =>
        {
            var job = sp.GetRequiredService<TJob>();
            return new CronJobRegistration(
                job.GetType().Name,
                job.CronExpression,
                sp);
        });

        return services;
    }

    public static IServiceCollection AddJobMiddleware<TMiddleware>(
        this IServiceCollection services)
        where TMiddleware : class, IJobMiddleware
    {
        services.AddTransient<TMiddleware>();
        services.AddSingleton<IJobMiddleware>(sp =>
        {
            var pipeline = sp.GetRequiredService<JobExecutionPipeline>();
            pipeline.Use<TMiddleware>();
            return sp.GetRequiredService<TMiddleware>();
        });

        return services;
    }

    internal sealed record CronJobRegistration(
        string JobName,
        string CronExpression,
        IServiceProvider ServiceProvider) : ICronJobRegistration;
}

internal interface ICronJobRegistration
{
    string JobName { get; }
    string CronExpression { get; }
    IServiceProvider ServiceProvider { get; }
}
