using Discord.Net.Scheduler.Builders;
using Discord.Net.Scheduler.Jobs;
using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Tests;

public sealed class ScheduledJobBuilderTests
{
    [Fact]
    public void Build_ShouldCreateSendMessageJob_WhenSendMessageCalled()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(123ul, "Test message")
            .In(TimeSpan.FromMinutes(5))
            .Build();

        job.Should().BeOfType<SendMessageJob>();
        var smj = (SendMessageJob)job;
        smj.ChannelId.Should().Be(123ul);
        smj.Message.Should().Be("Test message");
    }

    [Fact]
    public void Build_ShouldCreateSendEmbedJob_WhenSendEmbedCalled()
    {
        var embed = new EmbedBuilder().WithTitle("Test").Build();

        var job = new ScheduledJobBuilder()
            .SendEmbed(456ul, embed, "With embed")
            .In(TimeSpan.FromHours(1))
            .Build();

        job.Should().BeOfType<SendEmbedJob>();
        var sej = (SendEmbedJob)job;
        sej.ChannelId.Should().Be(456ul);
    }

    [Fact]
    public void Build_ShouldCreateCustomJob_WhenExecuteCalled()
    {
        var job = new ScheduledJobBuilder()
            .Execute((ctx, ct) => Task.FromResult(JobResult.Success(TimeSpan.Zero)))
            .WithName("custom-test")
            .At(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        job.Should().BeOfType<CustomJob>();
        job.Name.Should().Be("custom-test");
    }

    [Fact]
    public void Build_ShouldSetNextExecution_FromDelay()
    {
        var before = DateTimeOffset.UtcNow;

        var job = new ScheduledJobBuilder()
            .SendMessage(1ul, "test")
            .In(TimeSpan.FromMinutes(10))
            .Build();

        job.NextExecution.Should().NotBeNull();
        job.NextExecution!.Value.Should().BeCloseTo(before.AddMinutes(10), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Build_ShouldSetNextExecution_FromAt()
    {
        var scheduled = new DateTimeOffset(2026, 12, 25, 10, 0, 0, TimeSpan.Zero);

        var job = new ScheduledJobBuilder()
            .SendMessage(1ul, "test")
            .At(scheduled)
            .Build();

        job.NextExecution.Should().Be(scheduled);
    }

    [Fact]
    public void Build_ShouldThrow_WhenNoActionSpecified()
    {
        var act = () => new ScheduledJobBuilder().Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Build_ShouldSetRetries_WhenConfigured()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(1ul, "test")
            .In(TimeSpan.FromMinutes(1))
            .WithRetries(5, TimeSpan.FromSeconds(10))
            .Build();

        job.MaxRetries.Should().Be(5);
        job.RetryDelay.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Build_ShouldSetExpiration_WhenConfigured()
    {
        var expires = DateTimeOffset.UtcNow.AddDays(7);

        var job = new ScheduledJobBuilder()
            .SendMessage(1ul, "test")
            .In(TimeSpan.FromMinutes(1))
            .ExpiresAt(expires)
            .Build();

        job.ExpiresAt.Should().Be(expires);
    }
}
