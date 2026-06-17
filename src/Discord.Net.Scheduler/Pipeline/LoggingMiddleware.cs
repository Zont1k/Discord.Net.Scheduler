using Discord.Net.Scheduler.Scheduling;
using Microsoft.Extensions.Logging;

namespace Discord.Net.Scheduler.Pipeline;

public sealed class LoggingMiddleware : IJobMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task<JobResult> InvokeAsync(JobContext context, JobMiddlewareDelegate next)
    {
        var jobId = context.JobId;
        var jobName = context.JobName ?? "(unnamed)";

        _logger.LogInformation(
            "Job {JobId} ({JobName}) execution started at {Timestamp}",
            jobId, jobName, DateTimeOffset.UtcNow);

        try
        {
            var result = await next(context);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Job {JobId} ({JobName}) completed in {Elapsed}",
                    jobId, jobName, result.ExecutionTime);
            }
            else
            {
                _logger.LogWarning(
                    "Job {JobId} ({JobName}) failed: {Error} (elapsed: {Elapsed})",
                    jobId, jobName, result.Error, result.ExecutionTime);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Job {JobId} ({JobName}) was cancelled",
                jobId, jobName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Job {JobId} ({JobName}) threw an unhandled exception",
                jobId, jobName);
            return JobResult.Failure(ex.ToString(), TimeSpan.Zero);
        }
    }
}
