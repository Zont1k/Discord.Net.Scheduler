using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Net.Scheduler.EntityFrameworkCore;

[Table("ScheduledJobs")]
public sealed class JobEntity
{
    [Key]
    [MaxLength(128)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Name { get; set; }

    [MaxLength(64)]
    public string Type { get; set; } = string.Empty;

    public int Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? NextExecution { get; set; }

    public DateTimeOffset? LastExecutedAt { get; set; }

    public int ExecutionCount { get; set; }

    public string? LastError { get; set; }

    public long? IntervalTicks { get; set; }

    [MaxLength(256)]
    public string? CronExpression { get; set; }

    [MaxLength(128)]
    public string? TimezoneId { get; set; }

    public int MaxRetries { get; set; } = 3;

    public long? RetryDelayTicks { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public string? MetadataJson { get; set; }

    // SendMessageJob
    public ulong? ChannelId { get; set; }

    public string? Message { get; set; }

    public bool IsTTS { get; set; }

    // SendEmbedJob
    public ulong? EmbedChannelId { get; set; }

    public string? EmbedTitle { get; set; }

    public string? EmbedDescription { get; set; }

    public string? EmbedUrl { get; set; }

    public string? EmbedImageUrl { get; set; }

    public string? EmbedThumbnailUrl { get; set; }

    public string? EmbedColor { get; set; }

    public string? EmbedFooterText { get; set; }

    public string? EmbedFooterIconUrl { get; set; }

    public string? EmbedAuthorName { get; set; }

    public string? EmbedAuthorUrl { get; set; }

    public string? EmbedAuthorIconUrl { get; set; }

    public string? EmbedMessage { get; set; }

    // EditMessageJob
    public ulong? EditChannelId { get; set; }

    public ulong? EditMessageId { get; set; }

    public string? EditContent { get; set; }
}
