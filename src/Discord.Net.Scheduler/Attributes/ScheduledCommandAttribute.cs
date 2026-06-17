namespace Discord.Net.Scheduler.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ScheduledCommandAttribute : Attribute
{
    public string CommandName { get; }

    public string? CronExpression { get; }

    public ScheduledCommandAttribute(string commandName)
    {
        CommandName = commandName;
    }

    public ScheduledCommandAttribute(string commandName, string cronExpression)
    {
        CommandName = commandName;
        CronExpression = cronExpression;
    }
}
