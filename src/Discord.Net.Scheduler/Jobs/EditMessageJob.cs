using Discord.Net.Scheduler.Internal;
using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Jobs;

public sealed record EditMessageJob : ScheduledJob
{
    public ulong ChannelId { get; init; }
    public ulong MessageId { get; init; }
    public string? NewContent { get; init; }
    public Embed? NewEmbed { get; init; }

    public EditMessageJob(string id, ulong channelId, ulong messageId, string? newContent = null)
        : base(id, $"EditMessage-{channelId}")
    {
        ChannelId = channelId;
        MessageId = messageId;
        NewContent = newContent;
    }

    public override async Task<JobResult> ExecuteAsync(JobContext context, CancellationToken ct = default)
    {
        try
        {
            var channel = await context.Client.GetChannelAsync(ChannelId);
            if (channel is not IMessageChannel messageChannel)
                return JobResult.Failure($"Channel {ChannelId} is not a message channel.", TimeSpan.Zero);

            var sw = ValueStopwatch.StartNew();

            var message = await messageChannel.GetMessageAsync(MessageId);
            if (message is not IUserMessage userMessage)
                return JobResult.Failure($"Message {MessageId} is not a user message.", TimeSpan.Zero);

            await userMessage.ModifyAsync(props =>
            {
                if (NewContent is not null)
                    props.Content = NewContent;

                if (NewEmbed is not null)
                    props.Embed = NewEmbed;
            });

            return JobResult.Success(sw.Elapsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return JobResult.Failure(ex.Message, TimeSpan.Zero);
        }
    }
}
