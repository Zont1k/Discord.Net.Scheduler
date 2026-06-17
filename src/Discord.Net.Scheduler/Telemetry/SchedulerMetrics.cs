using System.Diagnostics.Metrics;

namespace Discord.Net.Scheduler.Telemetry;

public sealed class SchedulerMetrics : IDisposable
{
    private readonly Meter _meter;

    public SchedulerMetrics(string meterName = "Discord.Net.Scheduler")
    {
        _meter = new Meter(meterName, "1.0.0");

        JobScheduled = _meter.CreateCounter<long>("scheduler.job.scheduled", "jobs",
            "Total number of jobs scheduled");

        JobExecutionTime = _meter.CreateHistogram<double>("scheduler.job.execution_time", "ms",
            "Job execution time in milliseconds");

        JobsCompleted = _meter.CreateCounter<long>("scheduler.job.completed", "jobs",
            "Total number of jobs completed successfully");

        JobFailures = _meter.CreateCounter<long>("scheduler.job.failed", "jobs",
            "Total number of job failures");

        JobCancelled = _meter.CreateCounter<long>("scheduler.job.cancelled", "jobs",
            "Total number of jobs cancelled");

        ActiveJobs = _meter.CreateObservableGauge<int>("scheduler.job.active", () => _activeJobsCount,
            "Number of currently active jobs");
    }

    public Counter<long> JobScheduled { get; }

    public Histogram<double> JobExecutionTime { get; }

    public Counter<long> JobsCompleted { get; }

    public Counter<long> JobFailures { get; }

    public Counter<long> JobCancelled { get; }

    public ObservableGauge<int> ActiveJobs { get; }

    private int _activeJobsCount;

    public void IncrementActiveJobs() => Interlocked.Increment(ref _activeJobsCount);

    public void DecrementActiveJobs() => Interlocked.Decrement(ref _activeJobsCount);

    public void Dispose()
    {
        _meter.Dispose();
    }
}
