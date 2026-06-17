using Discord.Net.Scheduler.Jobs;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;
using MongoDB.Driver;

namespace Discord.Net.Scheduler.MongoDB;

public sealed class MongoJobStore : IJobStore
{
    private readonly IMongoCollection<JobWrapper> _collection;

    public MongoJobStore(IMongoDatabase database, string collectionName = "scheduled_jobs")
    {
        _collection = database.GetCollection<JobWrapper>(collectionName);
    }

    public async Task AddAsync(IScheduledJob job, CancellationToken ct = default)
    {
        var wrapper = new JobWrapper(job);
        await _collection.InsertOneAsync(wrapper, cancellationToken: ct);
    }

    public async Task RemoveAsync(string jobId, CancellationToken ct = default)
    {
        await _collection.DeleteOneAsync(j => j.Id == jobId, ct);
    }

    public async Task<IScheduledJob?> GetAsync(string jobId, CancellationToken ct = default)
    {
        var cursor = await _collection.FindAsync(j => j.Id == jobId, cancellationToken: ct);
        var wrapper = await cursor.FirstOrDefaultAsync(ct);
        return wrapper?.ToJob();
    }

    public async Task<IReadOnlyList<IScheduledJob>> GetPendingAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var cursor = await _collection.FindAsync(
            j => j.Status == JobStatus.Pending && j.NextExecution <= now,
            cancellationToken: ct);

        var wrappers = await cursor.ToListAsync(ct);
        return wrappers.Select(w => w.ToJob()!).ToList();
    }

    public async Task<IReadOnlyList<IScheduledJob>> GetAllAsync(CancellationToken ct = default)
    {
        var cursor = await _collection.FindAsync(FilterDefinition<JobWrapper>.Empty, cancellationToken: ct);
        var wrappers = await cursor.ToListAsync(ct);
        return wrappers.Select(w => w.ToJob()!).ToList();
    }

    public async Task UpdateAsync(IScheduledJob job, CancellationToken ct = default)
    {
        var wrapper = new JobWrapper(job);
        await _collection.ReplaceOneAsync(j => j.Id == job.Id, wrapper, cancellationToken: ct);
    }

    public async Task MarkCompletedAsync(string jobId, CancellationToken ct = default)
    {
        var update = Builders<JobWrapper>.Update
            .Set(j => j.Status, JobStatus.Completed)
            .Set(j => j.LastExecutedAt, DateTimeOffset.UtcNow);

        await _collection.UpdateOneAsync(j => j.Id == jobId, update, cancellationToken: ct);
    }

    public async Task MarkFailedAsync(string jobId, string error, CancellationToken ct = default)
    {
        var update = Builders<JobWrapper>.Update
            .Set(j => j.Status, JobStatus.Failed)
            .Set(j => j.LastExecutedAt, DateTimeOffset.UtcNow)
            .Set(j => j.LastError, error);

        await _collection.UpdateOneAsync(j => j.Id == jobId, update, cancellationToken: ct);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        return (int)await _collection.CountDocumentsAsync(FilterDefinition<JobWrapper>.Empty, cancellationToken: ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _collection.DeleteManyAsync(FilterDefinition<JobWrapper>.Empty, ct);
    }
}
