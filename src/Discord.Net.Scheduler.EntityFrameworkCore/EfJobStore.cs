using System.Text.Json;
using Discord.Net.Scheduler.Jobs;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;
using Microsoft.EntityFrameworkCore;

namespace Discord.Net.Scheduler.EntityFrameworkCore;

public sealed class EfJobStore : IJobStore, IAsyncDisposable
{
    private readonly IDbContextFactory<SchedulerDbContext> _contextFactory;

    private static readonly JsonSerializerOptions MetadataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public EfJobStore(IDbContextFactory<SchedulerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(IScheduledJob job, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var entity = MapToEntity(job);
        context.ScheduledJobs.Add(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string jobId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var entity = await context.ScheduledJobs.FindAsync([jobId], ct);
        if (entity is not null)
        {
            context.ScheduledJobs.Remove(entity);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<IScheduledJob?> GetAsync(string jobId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var entity = await context.ScheduledJobs.AsNoTracking().FirstOrDefaultAsync(e => e.Id == jobId, ct);
        return entity is null ? null : MapToJob(entity);
    }

    public async Task<IReadOnlyList<IScheduledJob>> GetPendingAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var entities = await context.ScheduledJobs
            .AsNoTracking()
            .Where(e => e.Status == (int)JobStatus.Pending && e.NextExecution <= now)
            .ToListAsync(ct);

        return entities.Select(MapToJob).ToList();
    }

    public async Task<IReadOnlyList<IScheduledJob>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var entities = await context.ScheduledJobs.AsNoTracking().ToListAsync(ct);
        return entities.Select(MapToJob).ToList();
    }

    public async Task UpdateAsync(IScheduledJob job, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var entity = MapToEntity(job);
        context.ScheduledJobs.Update(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task MarkCompletedAsync(string jobId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var entity = await context.ScheduledJobs.FindAsync([jobId], ct);
        if (entity is null) return;

        entity.Status = (int)JobStatus.Completed;
        entity.LastExecutedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(string jobId, string error, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var entity = await context.ScheduledJobs.FindAsync([jobId], ct);
        if (entity is null) return;

        entity.Status = (int)JobStatus.Failed;
        entity.LastExecutedAt = DateTimeOffset.UtcNow;
        entity.LastError = error;
        await context.SaveChangesAsync(ct);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.ScheduledJobs.CountAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.ScheduledJobs.RemoveRange(context.ScheduledJobs);
        await context.SaveChangesAsync(ct);
    }

    private static JobEntity MapToEntity(IScheduledJob job)
    {
        var entity = new JobEntity
        {
            Id = job.Id,
            Name = job.Name,
            Type = job.GetType().Name,
            Status = (int)job.Status,
            CreatedAt = job.CreatedAt,
            NextExecution = job.NextExecution,
            LastExecutedAt = job.LastExecutedAt,
            ExecutionCount = job.ExecutionCount,
            LastError = job.LastError,
            IntervalTicks = job.Interval?.Ticks,
            CronExpression = job.CronExpression,
            TimezoneId = job.Timezone?.Id,
            MaxRetries = job.MaxRetries,
            RetryDelayTicks = job.RetryDelay?.Ticks,
            ExpiresAt = job.ExpiresAt,
            MetadataJson = JsonSerializer.Serialize(job.Metadata, MetadataJsonOptions)
        };

        switch (job)
        {
            case SendMessageJob sm:
                entity.ChannelId = sm.ChannelId;
                entity.Message = sm.Message;
                entity.IsTTS = sm.IsTTS;
                break;
            case SendEmbedJob se:
                entity.EmbedChannelId = se.ChannelId;
                entity.EmbedMessage = se.Message;
                if (se.Embed is not null)
                {
                    entity.EmbedTitle = se.Embed.Title;
                    entity.EmbedDescription = se.Embed.Description;
                    entity.EmbedUrl = se.Embed.Url;
                    entity.EmbedImageUrl = se.Embed.Image?.Url;
                    entity.EmbedThumbnailUrl = se.Embed.Thumbnail?.Url;
                    entity.EmbedColor = se.Embed.Color?.ToString();
                    entity.EmbedFooterText = se.Embed.Footer?.Text;
                    entity.EmbedFooterIconUrl = se.Embed.Footer?.IconUrl;
                    entity.EmbedAuthorName = se.Embed.Author?.Name;
                    entity.EmbedAuthorUrl = se.Embed.Author?.Url;
                    entity.EmbedAuthorIconUrl = se.Embed.Author?.IconUrl;
                }
                break;
            case EditMessageJob em:
                entity.EditChannelId = em.ChannelId;
                entity.EditMessageId = em.MessageId;
                entity.EditContent = em.NewContent;
                break;
        }

        return entity;
    }

    private static IScheduledJob MapToJob(JobEntity entity)
    {
        TimeZoneInfo? tz = null;
        if (entity.TimezoneId is not null)
        {
            try { tz = TimeZoneInfo.FindSystemTimeZoneById(entity.TimezoneId); }
            catch { tz = TimeZoneInfo.Utc; }
        }

        var metadata = string.IsNullOrEmpty(entity.MetadataJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(entity.MetadataJson, MetadataJsonOptions)
                ?? new Dictionary<string, string>();

        IReadOnlyDictionary<string, string> readOnlyMetadata = metadata;

        return entity.Type switch
        {
            nameof(SendMessageJob) => new SendMessageJob(entity.Id, entity.ChannelId ?? 0, entity.Message ?? string.Empty)
            {
                Name = entity.Name,
                Status = (JobStatus)entity.Status,
                CreatedAt = entity.CreatedAt,
                NextExecution = entity.NextExecution,
                LastExecutedAt = entity.LastExecutedAt,
                ExecutionCount = entity.ExecutionCount,
                LastError = entity.LastError,
                Interval = entity.IntervalTicks.HasValue ? TimeSpan.FromTicks(entity.IntervalTicks.Value) : null,
                CronExpression = entity.CronExpression,
                Timezone = tz,
                MaxRetries = entity.MaxRetries,
                RetryDelay = entity.RetryDelayTicks.HasValue ? TimeSpan.FromTicks(entity.RetryDelayTicks.Value) : null,
                ExpiresAt = entity.ExpiresAt,
                Metadata = readOnlyMetadata,
                IsTTS = entity.IsTTS
            },
            nameof(SendEmbedJob) => new SendEmbedJob(entity.Id, entity.EmbedChannelId ?? 0, ReconstructEmbed(entity))
            {
                Name = entity.Name,
                Status = (JobStatus)entity.Status,
                CreatedAt = entity.CreatedAt,
                NextExecution = entity.NextExecution,
                LastExecutedAt = entity.LastExecutedAt,
                ExecutionCount = entity.ExecutionCount,
                LastError = entity.LastError,
                Interval = entity.IntervalTicks.HasValue ? TimeSpan.FromTicks(entity.IntervalTicks.Value) : null,
                CronExpression = entity.CronExpression,
                Timezone = tz,
                MaxRetries = entity.MaxRetries,
                RetryDelay = entity.RetryDelayTicks.HasValue ? TimeSpan.FromTicks(entity.RetryDelayTicks.Value) : null,
                ExpiresAt = entity.ExpiresAt,
                Metadata = readOnlyMetadata,
                Message = entity.EmbedMessage ?? string.Empty
            },
            nameof(EditMessageJob) => new EditMessageJob(entity.Id, entity.EditChannelId ?? 0, entity.EditMessageId ?? 0)
            {
                Name = entity.Name,
                Status = (JobStatus)entity.Status,
                CreatedAt = entity.CreatedAt,
                NextExecution = entity.NextExecution,
                LastExecutedAt = entity.LastExecutedAt,
                ExecutionCount = entity.ExecutionCount,
                LastError = entity.LastError,
                Interval = entity.IntervalTicks.HasValue ? TimeSpan.FromTicks(entity.IntervalTicks.Value) : null,
                CronExpression = entity.CronExpression,
                Timezone = tz,
                MaxRetries = entity.MaxRetries,
                RetryDelay = entity.RetryDelayTicks.HasValue ? TimeSpan.FromTicks(entity.RetryDelayTicks.Value) : null,
                ExpiresAt = entity.ExpiresAt,
                Metadata = readOnlyMetadata,
                NewContent = entity.EditContent
            },
            _ => throw new InvalidOperationException($"Unknown job type: {entity.Type}")
        };
    }

    private static Embed ReconstructEmbed(JobEntity entity)
    {
        var builder = new EmbedBuilder
        {
            Title = entity.EmbedTitle,
            Description = entity.EmbedDescription,
            Url = entity.EmbedUrl
        };

        if (entity.EmbedColor is not null &&
            uint.TryParse(entity.EmbedColor.TrimStart('#'), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var colorValue))
        {
            builder.WithColor(new Color(colorValue));
        }

        if (entity.EmbedImageUrl is not null)
            builder.WithImageUrl(entity.EmbedImageUrl);

        if (entity.EmbedThumbnailUrl is not null)
            builder.WithThumbnailUrl(entity.EmbedThumbnailUrl);

        if (entity.EmbedFooterText is not null)
            builder.WithFooter(entity.EmbedFooterText, entity.EmbedFooterIconUrl);

        if (entity.EmbedAuthorName is not null)
            builder.WithAuthor(entity.EmbedAuthorName, entity.EmbedAuthorUrl, entity.EmbedAuthorIconUrl);

        return builder.Build();
    }

    public async ValueTask DisposeAsync()
    {
        await ValueTask.CompletedTask;
    }
}
