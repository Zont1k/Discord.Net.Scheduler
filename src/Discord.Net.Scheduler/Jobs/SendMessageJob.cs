using System.Diagnostics;
using Discord.Net.Scheduler.Internal;
using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Jobs;

public sealed record SendMessageJob : ScheduledJob
{
    public ulong ChannelId { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsTTS { get; init; }
    public AllowedMentions? AllowedMentions { get; init; }
    public MessageReference? MessageReference { get; init; }

    public SendMessageJob(string id, ulong channelId, string message)
        : base(id, $"SendMessage-{channelId}")
    {
        ChannelId = channelId;
        Message = message;
    }

    public override async Task<JobResult> ExecuteAsync(JobContext context, CancellationToken ct = default)
    {
        try
        {
            var channel = await context.Client.GetChannelAsync(ChannelId);
            if (channel is not IMessageChannel messageChannel)
                return JobResult.Failure($"Channel {ChannelId} is not a message channel.", TimeSpan.Zero);

            var sw = ValueStopwatch.StartNew();

            await messageChannel.SendMessageAsync(Message, isTTS: IsTTS, allowedMentions: AllowedMentions, messageReference: MessageReference);

            return JobResult.Success(sw.Elapsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return JobResult.Failure(ex.Message, TimeSpan.Zero);
        }
    }
}
