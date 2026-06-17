using Discord.Net.Scheduler.Jobs;
using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Builders;

public sealed class ScheduledJobBuilder
{
    private string? _id;
    private string? _name;
    private string? _message;
    private ulong? _channelId;
    private Embed? _embed;
    private bool _isTTS;
    private ulong? _editMessageId;
    private Func<JobContext, CancellationToken, Task<JobResult>>? _customAction;
    private TimeSpan? _delay;
    private string? _cronExpression;
    private TimeZoneInfo? _timezone;
    private int _maxRetries = 3;
    private TimeSpan? _retryDelay = TimeSpan.FromSeconds(30);
    private DateTimeOffset? _scheduledAt;
    private DateTimeOffset? _expiresAt;
    private AllowedMentions? _allowedMentions;
    private readonly Dictionary<string, string> _metadata = new(StringComparer.Ordinal);

    public ScheduledJobBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public ScheduledJobBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ScheduledJobBuilder SendMessage(ulong channelId, string message)
    {
        _channelId = channelId;
        _message = message;
        return this;
    }

    public ScheduledJobBuilder SendEmbed(ulong channelId, Embed embed, string? message = null)
    {
        _channelId = channelId;
        _embed = embed;
        _message = message;
        return this;
    }

    public ScheduledJobBuilder EditMessage(ulong channelId, ulong messageId, string? newContent = null, Embed? newEmbed = null)
    {
        _channelId = channelId;
        _editMessageId = messageId;
        _message = newContent;
        _embed = newEmbed;
        return this;
    }

    public ScheduledJobBuilder WithTTS(bool tts = true)
    {
        _isTTS = tts;
        return this;
    }

    public ScheduledJobBuilder WithAllowedMentions(AllowedMentions allowed)
    {
        _allowedMentions = allowed;
        return this;
    }

    public ScheduledJobBuilder Execute(Func<JobContext, CancellationToken, Task<JobResult>> action)
    {
        _customAction = action;
        return this;
    }

    public ScheduledJobBuilder Every(TimeSpan interval)
    {
        _cronExpression = null;
        _delay = interval;
        return this;
    }

    public ScheduledJobBuilder WithDelay(TimeSpan delay)
    {
        _delay = delay;
        return this;
    }

    public ScheduledJobBuilder In(TimeSpan delay)
    {
        _delay = delay;
        return this;
    }

    public ScheduledJobBuilder At(DateTimeOffset scheduledAt)
    {
        _scheduledAt = scheduledAt;
        return this;
    }

    public ScheduledJobBuilder WithCron(string cronExpression)
    {
        _cronExpression = cronExpression;
        return this;
    }

    public ScheduledJobBuilder WithTimezone(TimeZoneInfo timezone)
    {
        _timezone = timezone;
        return this;
    }

    public ScheduledJobBuilder WithRetries(int maxRetries, TimeSpan? retryDelay = null)
    {
        _maxRetries = maxRetries;
        _retryDelay = retryDelay ?? _retryDelay;
        return this;
    }

    public ScheduledJobBuilder ExpiresAt(DateTimeOffset expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    public ScheduledJobBuilder WithMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    public ScheduledJobBuilder WithMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        foreach (var (key, value) in metadata)
            _metadata[key] = value;

        return this;
    }

    private string ResolveId()
    {
        if (_id is not null)
            return _id;

        return _customAction is not null
            ? $"custom_{Guid.NewGuid():N}"
            : $"job_{Guid.NewGuid():N}";
    }

    public IScheduledJob Build()
    {
        var id = ResolveId();

        DateTimeOffset? nextExecution;
        if (_scheduledAt.HasValue)
        {
            nextExecution = _scheduledAt.Value;
        }
        else if (_delay.HasValue)
        {
            nextExecution = DateTimeOffset.UtcNow.Add(_delay.Value);
        }
        else
        {
            nextExecution = DateTimeOffset.UtcNow;
        }

        if (_customAction is not null)
        {
            return new CustomJob(id, _customAction, _name)
            {
                NextExecution = nextExecution,
                CronExpression = _cronExpression,
                Timezone = _timezone,
                MaxRetries = _maxRetries,
                RetryDelay = _retryDelay,
                ExpiresAt = _expiresAt,
                Metadata = _metadata
            };
        }

        if (_editMessageId.HasValue)
        {
            return new EditMessageJob(id, _channelId!.Value, _editMessageId.Value, _message)
            {
                NextExecution = nextExecution,
                CronExpression = _cronExpression,
                Timezone = _timezone,
                MaxRetries = _maxRetries,
                RetryDelay = _retryDelay,
                ExpiresAt = _expiresAt,
                Metadata = _metadata
            };
        }

        if (_embed is not null)
        {
            return new SendEmbedJob(id, _channelId!.Value, _embed)
            {
                Message = _message,
                NextExecution = nextExecution,
                CronExpression = _cronExpression,
                Timezone = _timezone,
                MaxRetries = _maxRetries,
                RetryDelay = _retryDelay,
                ExpiresAt = _expiresAt,
                AllowedMentions = _allowedMentions,
                Metadata = _metadata
            };
        }

        if (_message is not null)
        {
            return new SendMessageJob(id, _channelId!.Value, _message)
            {
                IsTTS = _isTTS,
                NextExecution = nextExecution,
                CronExpression = _cronExpression,
                Timezone = _timezone,
                MaxRetries = _maxRetries,
                RetryDelay = _retryDelay,
                ExpiresAt = _expiresAt,
                AllowedMentions = _allowedMentions,
                Metadata = _metadata
            };
        }

        throw new InvalidOperationException(
            "No job action specified. Use SendMessage, SendEmbed, EditMessage, or Execute.");
    }
}
