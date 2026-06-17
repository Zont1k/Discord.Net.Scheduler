using Discord.Net.Scheduler.MongoDB;
using Discord.Net.Scheduler.Scheduling.JobStore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Discord.Net.Scheduler.Extensions;

public static class MongoServiceCollectionExtensions
{
    public static IServiceCollection AddMongoJobStore(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string collectionName = "scheduled_jobs")
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        return services.AddSingleton<IJobStore>(_ => new MongoJobStore(database, collectionName));
    }

    public static IServiceCollection AddMongoJobStore(
        this IServiceCollection services,
        IMongoDatabase database,
        string collectionName = "scheduled_jobs")
    {
        return services.AddSingleton<IJobStore>(_ => new MongoJobStore(database, collectionName));
    }
}
