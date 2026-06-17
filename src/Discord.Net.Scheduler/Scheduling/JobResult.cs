namespace Discord.Net.Scheduler.Scheduling;

public readonly record struct JobResult(JobStatus Status, string? Error = null, TimeSpan? ExecutionTime = null)
{
    public static JobResult Success(TimeSpan executionTime) => new(JobStatus.Completed, ExecutionTime: executionTime);

    public static JobResult Failure(string error, TimeSpan executionTime) => new(JobStatus.Failed, error, executionTime);

    public static JobResult Skipped(string? reason = null) => new(JobStatus.Skipped, reason);

    public bool IsSuccess => Status is JobStatus.Completed;
}
