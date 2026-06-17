using Discord.Net.Scheduler.Builders;
using Discord.Net.Scheduler.Triggers;

namespace Discord.Net.Scheduler.Tests;

public sealed class TriggerTests
{
    [Fact]
    public void WhenUserJoins_ShouldAddUserJoinedTrigger()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(123ul, "Welcome!")
            .In(TimeSpan.FromMinutes(1))
            .WhenUserJoins(456789ul)
            .Build();

        job.Triggers.Should().ContainSingle();
        job.Triggers[0].Should().BeOfType<UserJoinedTrigger>();
        var trigger = (UserJoinedTrigger)job.Triggers[0];
        trigger.UserId.Should().Be(456789ul);
    }

    [Fact]
    public void WhenMessageSent_WithoutPattern_ShouldAddTrigger()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(123ul, "Got a message!")
            .In(TimeSpan.FromMinutes(1))
            .WhenMessageSent(channelId: 789ul)
            .Build();

        var trigger = job.Triggers.Should().ContainSingle().Subject;
        trigger.Should().BeOfType<MessageSentTrigger>();
        var mst = (MessageSentTrigger)trigger;
        mst.ChannelId.Should().Be(789ul);
        mst.Pattern.Should().BeNull();
    }

    [Fact]
    public void WhenMessageSent_WithPattern_ShouldAddTrigger()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(123ul, "Command matched!")
            .In(TimeSpan.FromMinutes(1))
            .WhenMessageSent(pattern: "!help")
            .Build();

        var trigger = job.Triggers.Should().ContainSingle().Subject;
        trigger.Should().BeOfType<MessageSentTrigger>();
        var mst = (MessageSentTrigger)trigger;
        mst.ChannelId.Should().BeNull();
        mst.Pattern.Should().Be("!help");
    }

    [Fact]
    public void AfterJob_ShouldAddJobCompletedTriggerAndDependency()
    {
        var job = new ScheduledJobBuilder()
            .Execute((ctx, ct) => Task.FromResult(JobResult.Success(TimeSpan.Zero)))
            .In(TimeSpan.FromMinutes(1))
            .AfterJob("backup-job")
            .Build();

        job.Triggers.Should().ContainSingle();
        job.Triggers[0].Should().BeOfType<JobCompletedTrigger>();
        var trigger = (JobCompletedTrigger)job.Triggers[0];
        trigger.DependentJobId.Should().Be("backup-job");

        job.Dependencies.Should().ContainSingle().Which.Should().Be("backup-job");
    }

    [Fact]
    public void After_ShouldAddMultipleDependencies()
    {
        var job = new ScheduledJobBuilder()
            .Execute((ctx, ct) => Task.FromResult(JobResult.Success(TimeSpan.Zero)))
            .In(TimeSpan.FromMinutes(1))
            .After("job-a", "job-b", "job-c")
            .Build();

        job.Dependencies.Should().HaveCount(3);
        job.Dependencies.Should().Contain("job-a");
        job.Dependencies.Should().Contain("job-b");
        job.Dependencies.Should().Contain("job-c");
    }

    [Fact]
    public void RunIf_WithServiceProviderCondition_ShouldStoreCondition()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(1ul, "test")
            .In(TimeSpan.FromMinutes(1))
            .RunIf(sp => Task.FromResult(true))
            .Build();

        job.RunCondition.Should().NotBeNull();
    }

    [Fact]
    public void RunIf_WithSimpleCondition_ShouldStoreCondition()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(1ul, "test")
            .In(TimeSpan.FromMinutes(1))
            .RunIf(() => true)
            .Build();

        job.RunCondition.Should().NotBeNull();
    }

    [Fact]
    public void Build_ShouldHaveEmptyTriggersAndDependencies_ByDefault()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(1ul, "Hello")
            .In(TimeSpan.FromMinutes(1))
            .Build();

        job.Triggers.Should().BeEmpty();
        job.Dependencies.Should().BeEmpty();
        job.RunCondition.Should().BeNull();
    }

    [Fact]
    public void RecurringJobBuilder_ShouldSupportTriggers()
    {
        var job = RecurringJobBuilder.Create()
            .SendMessage(1ul, "test")
            .WithCron("0 * * * *")
            .WhenUserJoins(123ul)
            .Build();

        job.Triggers.Should().ContainSingle();
        job.Triggers[0].Should().BeOfType<UserJoinedTrigger>();
    }

    [Fact]
    public void RecurringJobBuilder_ShouldSupportDependencies()
    {
        var job = RecurringJobBuilder.Create()
            .Execute((ctx, ct) => Task.FromResult(JobResult.Success(TimeSpan.Zero)))
            .WithCron("0 0 * * *")
            .After("daily-backup")
            .Build();

        job.Dependencies.Should().ContainSingle().Which.Should().Be("daily-backup");
    }

    [Fact]
    public void TriggerSerialization_RoundTrip_ShouldPreserveUserJoined()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(1ul, "test")
            .In(TimeSpan.FromMinutes(1))
            .WhenUserJoins(42ul)
            .Build();

        var wrapper = new JobWrapper(job);
        var json = wrapper.Serialize();
        var deserialized = JobWrapper.Deserialize(json)!.ToJob()!;

        deserialized.Triggers.Should().ContainSingle();
        deserialized.Triggers[0].Should().BeOfType<UserJoinedTrigger>();
        ((UserJoinedTrigger)deserialized.Triggers[0]).UserId.Should().Be(42ul);
    }

    [Fact]
    public void TriggerSerialization_RoundTrip_ShouldPreserveJobCompleted()
    {
        var job = new ScheduledJobBuilder()
            .SendMessage(1ul, "test")
            .In(TimeSpan.FromMinutes(1))
            .AfterJob("some-job")
            .Build();

        var wrapper = new JobWrapper(job);
        var json = wrapper.Serialize();
        var deserialized = JobWrapper.Deserialize(json)!.ToJob()!;

        deserialized.Dependencies.Should().ContainSingle().Which.Should().Be("some-job");
        deserialized.Triggers.Should().ContainSingle();
        deserialized.Triggers[0].Should().BeOfType<JobCompletedTrigger>();
        ((JobCompletedTrigger)deserialized.Triggers[0]).DependentJobId.Should().Be("some-job");
    }
}