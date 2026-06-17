using System.Text.Json;
using System.Text.Json.Serialization;
using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Jobs;

public sealed record JobWrapper
{
    public string Type { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string? Name { get; init; }
    public JobStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? NextExecution { get; init; }
    public DateTimeOffset? LastExecutedAt { get; init; }
    public int ExecutionCount { get; init; }
    public string? LastError { get; init; }
    public TimeSpan? Interval { get; init; }
    public string? CronExpression { get; init; }
    public string? TimezoneId { get; init; }
    public int MaxRetries { get; init; } = 3;
    public TimeSpan? RetryDelay { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = [];

    // SendMessageJob fields
    public ulong? ChannelId { get; init; }
    public string? Message { get; init; }
    public bool IsTTS { get; init; }

    // SendEmbedJob fields
    public ulong? EmbedChannelId { get; init; }
    public string? EmbedTitle { get; init; }
    public string? EmbedDescription { get; init; }
    public string? EmbedUrl { get; init; }
    public string? EmbedImageUrl { get; init; }
    public string? EmbedThumbnailUrl { get; init; }
    public string? EmbedColor { get; init; }
    public string? EmbedFooterText { get; init; }
    public string? EmbedFooterIconUrl { get; init; }
    public string? EmbedAuthorName { get; init; }
    public string? EmbedAuthorUrl { get; init; }
    public string? EmbedAuthorIconUrl { get; init; }
    public string? EmbedMessage { get; init; }

    // EditMessageJob fields
    public ulong? EditChannelId { get; init; }
    public ulong? EditMessageId { get; init; }
    public string? EditContent { get; init; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JobWrapper()
    {
    }

    public JobWrapper(IScheduledJob job)
    {
        Type = job.GetType().Name;
        Id = job.Id;
        Name = job.Name;
        Status = job.Status;
        CreatedAt = job.CreatedAt;
        NextExecution = job.NextExecution;
        LastExecutedAt = job.LastExecutedAt;
        ExecutionCount = job.ExecutionCount;
        LastError = job.LastError;
        Interval = job.Interval;
        CronExpression = job.CronExpression;
        TimezoneId = job.Timezone?.Id;
        MaxRetries = job.MaxRetries;
        RetryDelay = job.RetryDelay;
        ExpiresAt = job.ExpiresAt;
        Metadata = job.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        switch (job)
        {
            case SendMessageJob sm:
                ChannelId = sm.ChannelId;
                Message = sm.Message;
                IsTTS = sm.IsTTS;
                break;
            case SendEmbedJob se:
                EmbedChannelId = se.ChannelId;
                EmbedMessage = se.Message;
                if (se.Embed is not null)
                {
                    EmbedTitle = se.Embed.Title;
                    EmbedDescription = se.Embed.Description;
                    EmbedUrl = se.Embed.Url;
                    EmbedImageUrl = se.Embed.Image?.Url;
                    EmbedThumbnailUrl = se.Embed.Thumbnail?.Url;
                    EmbedColor = se.Embed.Color?.ToString();
                    EmbedFooterText = se.Embed.Footer?.Text;
                    EmbedFooterIconUrl = se.Embed.Footer?.IconUrl;
                    EmbedAuthorName = se.Embed.Author?.Name;
                    EmbedAuthorUrl = se.Embed.Author?.Url;
                    EmbedAuthorIconUrl = se.Embed.Author?.IconUrl;
                }
                break;
            case EditMessageJob em:
                EditChannelId = em.ChannelId;
                EditMessageId = em.MessageId;
                EditContent = em.NewContent;
                break;
        }
    }

    public IScheduledJob? ToJob()
    {
        TimeZoneInfo? tz = null;
        if (TimezoneId is not null)
        {
            try { tz = TimeZoneInfo.FindSystemTimeZoneById(TimezoneId); }
            catch { tz = TimeZoneInfo.Utc; }
        }

        var metadata = Metadata as IReadOnlyDictionary<string, string>;

        return Type switch
        {
            nameof(SendMessageJob) => new SendMessageJob(Id, ChannelId ?? 0, Message ?? string.Empty)
            {
                Name = Name,
                Status = Status,
                CreatedAt = CreatedAt,
                NextExecution = NextExecution,
                LastExecutedAt = LastExecutedAt,
                ExecutionCount = ExecutionCount,
                LastError = LastError,
                Interval = Interval,
                CronExpression = CronExpression,
                Timezone = tz,
                MaxRetries = MaxRetries,
                RetryDelay = RetryDelay,
                ExpiresAt = ExpiresAt,
                Metadata = metadata,
                IsTTS = IsTTS
            },
            nameof(SendEmbedJob) => new SendEmbedJob(Id, EmbedChannelId ?? 0, ReconstructEmbed())
            {
                Name = Name,
                Status = Status,
                CreatedAt = CreatedAt,
                NextExecution = NextExecution,
                LastExecutedAt = LastExecutedAt,
                ExecutionCount = ExecutionCount,
                LastError = LastError,
                Interval = Interval,
                CronExpression = CronExpression,
                Timezone = tz,
                MaxRetries = MaxRetries,
                RetryDelay = RetryDelay,
                ExpiresAt = ExpiresAt,
                Metadata = metadata,
                Message = EmbedMessage
            },
            nameof(EditMessageJob) => new EditMessageJob(Id, EditChannelId ?? 0, EditMessageId ?? 0, EditContent)
            {
                Name = Name,
                Status = Status,
                CreatedAt = CreatedAt,
                NextExecution = NextExecution,
                LastExecutedAt = LastExecutedAt,
                ExecutionCount = ExecutionCount,
                LastError = LastError,
                Interval = Interval,
                CronExpression = CronExpression,
                Timezone = tz,
                MaxRetries = MaxRetries,
                RetryDelay = RetryDelay,
                ExpiresAt = ExpiresAt,
                Metadata = metadata
            },
            _ => null
        };
    }

    private Embed ReconstructEmbed()
    {
        var builder = new EmbedBuilder
        {
            Title = EmbedTitle,
            Description = EmbedDescription,
            Url = EmbedUrl
        };

        if (EmbedColor is not null &&
            uint.TryParse(EmbedColor.TrimStart('#'), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var colorValue))
        {
            builder.WithColor(new Color(colorValue));
        }

        if (EmbedImageUrl is not null)
            builder.WithImageUrl(EmbedImageUrl);

        if (EmbedThumbnailUrl is not null)
            builder.WithThumbnailUrl(EmbedThumbnailUrl);

        if (EmbedFooterText is not null)
            builder.WithFooter(EmbedFooterText, EmbedFooterIconUrl);

        if (EmbedAuthorName is not null)
            builder.WithAuthor(EmbedAuthorName, EmbedAuthorUrl, EmbedAuthorIconUrl);

        return builder.Build();
    }

    public string Serialize() => JsonSerializer.Serialize(this, JsonOptions);

    public static JobWrapper? Deserialize(string json) =>
        JsonSerializer.Deserialize<JobWrapper>(json, JsonOptions);
}
