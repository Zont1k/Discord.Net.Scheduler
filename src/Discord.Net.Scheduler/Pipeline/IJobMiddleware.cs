using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Pipeline;

public interface IJobMiddleware
{
    Task<JobResult> InvokeAsync(JobContext context, JobMiddlewareDelegate next);
}
