using Discord.Net.Scheduler.Scheduling;
using Microsoft.Extensions.Logging;

namespace Discord.Net.Scheduler.Pipeline;

public sealed class ErrorHandlingMiddleware : IJobMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<JobResult> InvokeAsync(JobContext context, JobMiddlewareDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Job {JobId} encountered an unhandled error", context.JobId);
            return JobResult.Failure(ex.ToString(), TimeSpan.Zero);
        }
    }
}
