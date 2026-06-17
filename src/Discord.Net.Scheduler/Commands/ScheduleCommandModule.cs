using Discord.Interactions;
using Discord.Net.Scheduler.Builders;
using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Commands;

public sealed class ScheduleCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly JobScheduler _scheduler;
    private readonly IServiceProvider _services;

    public ScheduleCommandModule(JobScheduler scheduler, IServiceProvider services)
    {
        _scheduler = scheduler;
        _services = services;
    }

    [SlashCommand("schedule", "Schedule a one-time or recurring message")]
    public async Task ScheduleAsync(
        [Summary("channel", "Channel to send the message in")] ITextChannel? channel = null,
        [Summary("cron", "Cron expression (e.g. '0 8 * * *' or 'daily')")] string? cron = null,
        [Summary("in", "Delay in minutes (e.g. '30' for 30 min)")] double? inMinutes = null,
        [Summary("at", "Schedule for a specific date/time (ISO 8601)")] string? at = null,
        [Summary("message", "Text message to send")] string? message = null)
    {
        await DeferAsync(ephemeral: true);

        var target = channel ?? (ITextChannel)Context.Channel;

        if (string.IsNullOrWhiteSpace(message))
        {
            await FollowupAsync("Please provide a message to schedule.", ephemeral: true);
            return;
        }

        try
        {
            string jobId;

            if (!string.IsNullOrWhiteSpace(cron))
            {
                jobId = await _scheduler.ScheduleRecurringAsync(b =>
                {
                    b.WithName($"Scheduled: {message[..Math.Min(message.Length, 50)]}")
                     .SendMessage(target.Id, message)
                     .WithCron(cron.Trim());
                });
            }
            else if (inMinutes.HasValue)
            {
                var executionTime = DateTimeOffset.UtcNow.AddMinutes(inMinutes.Value);
                jobId = await _scheduler.ScheduleAsync(b =>
                {
                    b.WithName($"Scheduled: {message[..Math.Min(message.Length, 50)]}")
                     .SendMessage(target.Id, message)
                     .At(executionTime);
                });
            }
            else if (!string.IsNullOrWhiteSpace(at))
            {
                if (!DateTimeOffset.TryParse(at, out var parsedAt))
                {
                    await FollowupAsync(
                        "Invalid date/time format. Use ISO 8601 (e.g. 2026-06-18T08:00:00Z).",
                        ephemeral: true);
                    return;
                }

                jobId = await _scheduler.ScheduleAsync(b =>
                {
                    b.WithName($"Scheduled: {message[..Math.Min(message.Length, 50)]}")
                     .SendMessage(target.Id, message)
                     .At(parsedAt);
                });
            }
            else
            {
                await FollowupAsync(
                    "Specify one of: cron / in / at to define when to execute.",
                    ephemeral: true);
                return;
            }

            await FollowupAsync(
                $"Scheduled job `{jobId}`. {(cron is not null ? "It will repeat on schedule." : "")}",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Failed to schedule: {ex.Message}", ephemeral: true);
        }
    }

    [SlashCommand("jobs", "List all scheduled jobs")]
    public async Task ListJobsAsync()
    {
        await DeferAsync(ephemeral: true);

        var jobs = await _scheduler.GetAllJobsAsync();
        if (jobs.Count == 0)
        {
            await FollowupAsync("No scheduled jobs.", ephemeral: true);
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("### Scheduled Jobs");
        sb.AppendLine("| ID | Name | Status | Next Execution | Cron |");
        sb.AppendLine("|---|---|---|---|---|");

        foreach (var job in jobs.Take(10))
        {
            var id = job.Id[..Math.Min(job.Id.Length, 8)];
            var name = job.Name ?? "(unnamed)";
            var status = job.Status;
            var next = job.NextExecution?.ToString("yyyy-MM-dd HH:mm") ?? "-";
            var cron = job.CronExpression ?? "-";
            sb.AppendLine($"| `{id}` | {name} | {status} | {next} | `{cron}` |");
        }

        if (jobs.Count > 10)
            sb.AppendLine($"... and {jobs.Count - 10} more");

        await FollowupAsync(sb.ToString(), ephemeral: true);
    }

    [SlashCommand("cancel", "Cancel a scheduled job")]
    public async Task CancelJobAsync(
        [Summary("job_id", "ID of the job to cancel")] string jobId)
    {
        await DeferAsync(ephemeral: true);

        var success = await _scheduler.CancelAsync(jobId);
        if (success)
            await FollowupAsync($"Cancelled job `{jobId}`.", ephemeral: true);
        else
            await FollowupAsync($"Job `{jobId}` not found.", ephemeral: true);
    }
}
