using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Pipeline;

public sealed class JobExecutionPipeline
{
    private readonly List<Func<JobMiddlewareDelegate, JobMiddlewareDelegate>> _middleware = [];
    private JobMiddlewareDelegate? _core;

    internal JobExecutionPipeline()
    {
    }

    public JobExecutionPipeline Use(Func<JobContext, JobMiddlewareDelegate, Task<JobResult>> middleware)
    {
        _middleware.Add(next => ctx => middleware(ctx, next));
        return this;
    }

    public JobExecutionPipeline Use<TMiddleware>() where TMiddleware : class, IJobMiddleware
    {
        _middleware.Add(next => ctx =>
        {
            var middleware = ctx.GetService<TMiddleware>();
            return middleware.InvokeAsync(ctx, next);
        });
        return this;
    }

    internal void SetCore(JobMiddlewareDelegate core)
    {
        _core = core;
    }

    internal JobMiddlewareDelegate Build()
    {
        if (_core is null)
            throw new InvalidOperationException("Core delegate must be set before building.");

        JobMiddlewareDelegate pipeline = _core;

        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            pipeline = _middleware[i](pipeline);
        }

        return pipeline;
    }
}
