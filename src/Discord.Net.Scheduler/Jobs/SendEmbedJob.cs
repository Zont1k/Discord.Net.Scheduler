using Discord.Net.Scheduler.Internal;
using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Jobs;

public sealed record SendEmbedJob : ScheduledJob
{
    public ulong ChannelId { get; init; }
    public Embed Embed { get; init; }
    public string? Message { get; init; }
    public AllowedMentions? AllowedMentions { get; init; }

    public SendEmbedJob(string id, ulong channelId, Embed embed)
        : base(id, $"SendEmbed-{channelId}")
    {
        ChannelId = channelId;
        Embed = embed;
    }

    public override async Task<JobResult> ExecuteAsync(JobContext context, CancellationToken ct = default)
    {
        try
        {
            var channel = await context.Client.GetChannelAsync(ChannelId);
            if (channel is not IMessageChannel messageChannel)
                return JobResult.Failure($"Channel {ChannelId} is not a message channel.", TimeSpan.Zero);

            var sw = ValueStopwatch.StartNew();

            await messageChannel.SendMessageAsync(Message, embed: Embed, allowedMentions: AllowedMentions);

            return JobResult.Success(sw.Elapsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return JobResult.Failure(ex.Message, TimeSpan.Zero);
        }
    }
}
