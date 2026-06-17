namespace Discord.Net.Scheduler.Triggers;

public interface IJobTrigger
{
    string Type { get; }
}

public sealed record UserJoinedTrigger(ulong UserId) : IJobTrigger
{
    public string Type => nameof(UserJoinedTrigger);
}

public sealed record MessageSentTrigger(ulong? ChannelId, string? Pattern) : IJobTrigger
{
    public string Type => nameof(MessageSentTrigger);
}

public sealed record JobCompletedTrigger(string DependentJobId) : IJobTrigger
{
    public string Type => nameof(JobCompletedTrigger);
}

public sealed record CronTrigger(string Expression) : IJobTrigger
{
    public string Type => nameof(CronTrigger);
}

public sealed record DelayTrigger(TimeSpan Delay) : IJobTrigger
{
    public string Type => nameof(DelayTrigger);
}

public sealed record AtTrigger(DateTimeOffset UtcTime) : IJobTrigger
{
    public string Type => nameof(AtTrigger);
}