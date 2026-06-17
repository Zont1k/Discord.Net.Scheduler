using System.Text.Json;
using System.Text.Json.Serialization;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Triggers;

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
    public List<string>? TriggersJson { get; init; }
    public List<string> Dependencies { get; init; } = [];

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

    private static readonly JsonSerializerOptions TriggerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        IncludeFields = true
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
        Dependencies = job.Dependencies.ToList();

        if (job.Triggers.Count > 0)
        {
            TriggersJson = job.Triggers.Select(t => SerializeTrigger(t)).ToList();
        }

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
        var triggers = DeserializeTriggers();
        var deps = Dependencies.AsReadOnly();

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
                IsTTS = IsTTS,
                Triggers = triggers,
                Dependencies = deps
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
                Message = EmbedMessage,
                Triggers = triggers,
                Dependencies = deps
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
                Metadata = metadata,
                Triggers = triggers,
                Dependencies = deps
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

    private static string SerializeTrigger(IJobTrigger trigger)
    {
        return trigger switch
        {
            UserJoinedTrigger t => $"{t.Type}:{t.UserId}",
            MessageSentTrigger t => $"{t.Type}:{t.ChannelId}|{t.Pattern}",
            JobCompletedTrigger t => $"{t.Type}:{t.DependentJobId}",
            CronTrigger t => $"{t.Type}:{t.Expression}",
            DelayTrigger t => $"{t.Type}:{t.Delay:c}",
            AtTrigger t => $"{t.Type}:{t.UtcTime:O}",
            _ => throw new NotSupportedException($"Unknown trigger type: {trigger.GetType()}")
        };
    }

    private IReadOnlyList<IJobTrigger> DeserializeTriggers()
    {
        if (TriggersJson is null || TriggersJson.Count == 0)
            return [];

        var list = new List<IJobTrigger>(TriggersJson.Count);

        foreach (var s in TriggersJson)
        {
            var colonIdx = s.IndexOf(':');
            if (colonIdx < 0) continue;

            var type = s[..colonIdx];
            var value = s[(colonIdx + 1)..];

            switch (type)
            {
                case nameof(UserJoinedTrigger):
                    if (ulong.TryParse(value, out var uid))
                        list.Add(new UserJoinedTrigger(uid));
                    break;
                case nameof(MessageSentTrigger):
                    var parts = value.Split('|', 2);
                    ulong? cid = parts[0] is { Length: > 0 } c && ulong.TryParse(c, out var cp) ? cp : null;
                    var pat = parts.Length > 1 && parts[1] is { Length: > 0 } p ? p : null;
                    list.Add(new MessageSentTrigger(cid, pat));
                    break;
                case nameof(JobCompletedTrigger):
                    list.Add(new JobCompletedTrigger(value));
                    break;
                case nameof(CronTrigger):
                    list.Add(new CronTrigger(value));
                    break;
                case nameof(DelayTrigger):
                    if (TimeSpan.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var d))
                        list.Add(new DelayTrigger(d));
                    break;
                case nameof(AtTrigger):
                    if (DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                        list.Add(new AtTrigger(dt));
                    break;
            }
        }

        return list.AsReadOnly();
    }

    public string Serialize() => JsonSerializer.Serialize(this, JsonOptions);

    public static JobWrapper? Deserialize(string json) =>
        JsonSerializer.Deserialize<JobWrapper>(json, JsonOptions);
}