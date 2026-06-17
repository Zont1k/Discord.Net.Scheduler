using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Triggers;

namespace Discord.Net.Scheduler.Builders;

public sealed class RecurringJobBuilder
{
    private readonly ScheduledJobBuilder _inner = new();
    private string? _cronExpression;
    private TimeZoneInfo? _timezone;
    private bool _retriesSet;

    private RecurringJobBuilder()
    {
    }

    public static RecurringJobBuilder Create() => new();

    public RecurringJobBuilder WithId(string id)
    {
        _inner.WithId(id);
        return this;
    }

    public RecurringJobBuilder WithName(string name)
    {
        _inner.WithName(name);
        return this;
    }

    public RecurringJobBuilder SendMessage(ulong channelId, string message)
    {
        _inner.SendMessage(channelId, message);
        return this;
    }

    public RecurringJobBuilder SendEmbed(ulong channelId, Embed embed, string? message = null)
    {
        _inner.SendEmbed(channelId, embed, message);
        return this;
    }

    public RecurringJobBuilder WithCron(string cronExpression)
    {
        _cronExpression = cronExpression;
        return this;
    }

    public RecurringJobBuilder WithTimezone(TimeZoneInfo timezone)
    {
        _timezone = timezone;
        return this;
    }

    public RecurringJobBuilder Every(TimeSpan interval)
    {
        _inner.WithDelay(interval);
        return this;
    }

    public RecurringJobBuilder Hourly()
    {
        _cronExpression = "0 * * * *";
        return this;
    }

    public RecurringJobBuilder Daily()
    {
        _cronExpression = "0 0 * * *";
        return this;
    }

    public RecurringJobBuilder Weekly()
    {
        _cronExpression = "0 0 * * 0";
        return this;
    }

    public RecurringJobBuilder Monthly()
    {
        _cronExpression = "0 0 1 * *";
        return this;
    }

    public RecurringJobBuilder Execute(Func<JobContext, CancellationToken, Task<JobResult>> action)
    {
        _inner.Execute(action);
        return this;
    }

    public RecurringJobBuilder WithRetries(int maxRetries, TimeSpan? retryDelay = null)
    {
        _retriesSet = true;
        _inner.WithRetries(maxRetries, retryDelay);
        return this;
    }

    public RecurringJobBuilder WithMetadata(string key, string value)
    {
        _inner.WithMetadata(key, value);
        return this;
    }

    public RecurringJobBuilder WhenUserJoins(ulong userId)
    {
        _inner.WhenUserJoins(userId);
        return this;
    }

    public RecurringJobBuilder WhenMessageSent(ulong? channelId = null, string? pattern = null)
    {
        _inner.WhenMessageSent(channelId, pattern);
        return this;
    }

    public RecurringJobBuilder AfterJob(string jobId)
    {
        _inner.AfterJob(jobId);
        return this;
    }

    public RecurringJobBuilder After(params string[] jobIds)
    {
        _inner.After(jobIds);
        return this;
    }

    public RecurringJobBuilder RunIf(Func<IServiceProvider, Task<bool>> condition)
    {
        _inner.RunIf(condition);
        return this;
    }

    public RecurringJobBuilder RunIf(Func<bool> condition)
    {
        _inner.RunIf(condition);
        return this;
    }

    public IScheduledJob Build()
    {
        if (_cronExpression is null)
            throw new InvalidOperationException("Recurring job must have a cron expression or interval.");

        if (!CronParser.IsValid(_cronExpression))
            throw new FormatException($"Invalid cron expression: '{_cronExpression}'.");

        var next = CronParser.GetNextOccurrence(_cronExpression, DateTimeOffset.UtcNow);

        _inner.WithCron(_cronExpression);
        _inner.WithTimezone(_timezone ?? TimeZoneInfo.Utc);
        _inner.At(next);

        if (!_retriesSet)
            _inner.WithRetries(3, TimeSpan.FromMinutes(1));

        return _inner.Build();
    }
}