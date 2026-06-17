namespace Discord.Net.Scheduler.Attributes;

/// <summary>
/// Marks a method as a scheduled command that can be triggered on a timer or cron schedule.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ScheduledCommandAttribute : Attribute
{
    /// <summary>
    /// Gets the command name used for identification.
    /// </summary>
    public string CommandName { get; }

    /// <summary>
    /// Gets the optional cron expression for automatic scheduling.
    /// </summary>
    public string? CronExpression { get; }

    /// <param name="commandName">The name of the command.</param>
    public ScheduledCommandAttribute(string commandName)
    {
        CommandName = commandName;
    }

    /// <param name="commandName">The name of the command.</param>
    /// <param name="cronExpression">A 5-field cron expression for automatic scheduling.</param>
    public ScheduledCommandAttribute(string commandName, string cronExpression)
    {
        CommandName = commandName;
        CronExpression = cronExpression;
    }
}