using Discord.Net.Scheduler.Jobs;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;

namespace Discord.Net.Scheduler.Tests;

public sealed class InMemoryJobStoreTests
{
    private readonly InMemoryJobStore _sut = new();

    [Fact]
    public async Task AddAsync_ShouldStoreJob()
    {
        var job = new SendMessageJob("test-1", 123ul, "Hello");

        await _sut.AddAsync(job);
        var retrieved = await _sut.GetAsync("test-1");

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("test-1");
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnDueJobs()
    {
        var pastJob = new SendMessageJob("past", 123ul, "Past")
        {
            NextExecution = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        var futureJob = new SendMessageJob("future", 123ul, "Future")
        {
            NextExecution = DateTimeOffset.UtcNow.AddHours(2)
        };

        await _sut.AddAsync(pastJob);
        await _sut.AddAsync(futureJob);

        var pending = await _sut.GetPendingAsync();

        pending.Should().Contain(j => j.Id == "past");
        pending.Should().NotContain(j => j.Id == "future");
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteJob()
    {
        var job = new SendMessageJob("remove-me", 123ul, "Remove");
        await _sut.AddAsync(job);

        await _sut.RemoveAsync("remove-me");
        var retrieved = await _sut.GetAsync("remove-me");

        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task MarkCompletedAsync_ShouldUpdateStatus()
    {
        var job = new SendMessageJob("complete-me", 123ul, "Complete");
        await _sut.AddAsync(job);

        await _sut.MarkCompletedAsync("complete-me");
        var retrieved = await _sut.GetAsync("complete-me");

        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(JobStatus.Completed);
        retrieved.LastExecutedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldUpdateStatusAndError()
    {
        var job = new SendMessageJob("fail-me", 123ul, "Fail");
        await _sut.AddAsync(job);

        await _sut.MarkFailedAsync("fail-me", "Something went wrong");
        var retrieved = await _sut.GetAsync("fail-me");

        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(JobStatus.Failed);
        retrieved.LastError.Should().Be("Something went wrong");
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        await _sut.AddAsync(new SendMessageJob("1", 1ul, "A"));
        await _sut.AddAsync(new SendMessageJob("2", 1ul, "B"));
        await _sut.AddAsync(new SendMessageJob("3", 1ul, "C"));

        var count = await _sut.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllJobs()
    {
        await _sut.AddAsync(new SendMessageJob("1", 1ul, "A"));
        await _sut.AddAsync(new SendMessageJob("2", 1ul, "B"));

        await _sut.ClearAsync();
        var count = await _sut.CountAsync();

        count.Should().Be(0);
    }
}
