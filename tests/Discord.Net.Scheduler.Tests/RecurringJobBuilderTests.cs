using Discord.Net.Scheduler.Builders;
using Discord.Net.Scheduler.Jobs;
using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Tests;

public sealed class RecurringJobBuilderTests
{
    [Fact]
    public void Build_ShouldCreateJobWithCronExpression()
    {
        var job = RecurringJobBuilder.Create()
            .SendMessage(123ul, "Daily message")
            .WithCron("0 8 * * *")
            .Build();

        job.CronExpression.Should().Be("0 8 * * *");
        job.NextExecution.Should().NotBeNull();
    }

    [Fact]
    public void Build_ShouldThrow_WhenNoCronExpression()
    {
        var act = () => RecurringJobBuilder.Create()
            .SendMessage(1ul, "test")
            .Build();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Build_ShouldThrow_WhenInvalidCron()
    {
        var act = () => RecurringJobBuilder.Create()
            .SendMessage(1ul, "test")
            .WithCron("invalid")
            .Build();

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Build_ShouldUseDailyHelper()
    {
        var job = RecurringJobBuilder.Create()
            .SendMessage(1ul, "test")
            .Daily()
            .Build();

        job.CronExpression.Should().Be("0 0 * * *");
    }

    [Fact]
    public void Build_ShouldUseHourlyHelper()
    {
        var job = RecurringJobBuilder.Create()
            .SendMessage(1ul, "test")
            .Hourly()
            .Build();

        job.CronExpression.Should().Be("0 * * * *");
    }

    [Fact]
    public void Build_ShouldUseWeeklyHelper()
    {
        var job = RecurringJobBuilder.Create()
            .SendMessage(1ul, "test")
            .Weekly()
            .Build();

        job.CronExpression.Should().Be("0 0 * * 0");
    }

    [Fact]
    public void Build_ShouldUseMonthlyHelper()
    {
        var job = RecurringJobBuilder.Create()
            .SendMessage(1ul, "test")
            .Monthly()
            .Build();

        job.CronExpression.Should().Be("0 0 1 * *");
    }

    [Fact]
    public void Build_ShouldSetRetriesForRecurringJobs()
    {
        var job = RecurringJobBuilder.Create()
            .SendMessage(1ul, "test")
            .WithCron("0 0 * * *")
            .WithRetries(2, TimeSpan.FromSeconds(15))
            .Build();

        job.MaxRetries.Should().Be(2);
        job.RetryDelay.Should().Be(TimeSpan.FromSeconds(15));
    }
}
