using Discord.Net.Scheduler.Pipeline;
using Discord.Net.Scheduler.Scheduling;
using Discord.WebSocket;

namespace Discord.Net.Scheduler.Tests;

public sealed class JobPipelineTests
{
    [Fact]
    public async Task Pipeline_ShouldExecuteMiddlewareInOrder()
    {
        var executionOrder = new List<string>();
        var client = Substitute.For<DiscordSocketClient>();

        var pipeline = new JobExecutionPipeline();
        pipeline.Use(async (ctx, next) =>
        {
            executionOrder.Add("first-before");
            var result = await next(ctx);
            executionOrder.Add("first-after");
            return result;
        });

        pipeline.Use(async (ctx, next) =>
        {
            executionOrder.Add("second-before");
            var result = await next(ctx);
            executionOrder.Add("second-after");
            return result;
        });

        pipeline.SetCore(ctx =>
        {
            executionOrder.Add("core");
            return Task.FromResult(JobResult.Success(TimeSpan.FromMilliseconds(100)));
        });

        var built = pipeline.Build();

        var services = Substitute.For<IServiceProvider>();
        var context = new JobContext("test", "test-job", client, services, CancellationToken.None);

        var result = await built(context);

        result.IsSuccess.Should().BeTrue();
        executionOrder.Should().ContainInOrder(
            "first-before",
            "second-before",
            "core",
            "second-after",
            "first-after");
    }

    [Fact]
    public async Task Pipeline_ShouldHandleErrorInMiddleware()
    {
        var client = Substitute.For<DiscordSocketClient>();

        var pipeline = new JobExecutionPipeline();
        pipeline.Use<ErrorHandlingMiddleware>();

        pipeline.SetCore(ctx =>
        {
            throw new InvalidOperationException("Something broke");
        });

        var built = pipeline.Build();

        var services = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<ErrorHandlingMiddleware>>();
        services.GetService(typeof(ILogger<ErrorHandlingMiddleware>)).Returns(logger);
        services.GetService(typeof(ErrorHandlingMiddleware)).Returns(new ErrorHandlingMiddleware(logger));

        var context = new JobContext("error-test", "error-job", client, services, CancellationToken.None);

        var result = await built(context);

        result.Status.Should().Be(JobStatus.Failed);
        result.IsSuccess.Should().BeFalse();
    }
}
