using System.Diagnostics;
using System.Text.RegularExpressions;
using Discord.Net.Scheduler.Builders;
using Discord.Net.Scheduler.Triggers;
using Discord.WebSocket;
using Discord.Net.Scheduler.Pipeline;
using Discord.Net.Scheduler.Scheduling.JobStore;
using Discord.Net.Scheduler.Scheduling.Locking;
using Discord.Net.Scheduler.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Discord.Net.Scheduler.Scheduling;

public sealed class JobScheduler : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly IJobStore _store;
    private readonly IDistributedLock _lock;
    private readonly JobExecutionPipeline _pipeline;
    private readonly SchedulerOptions _options;
    private readonly ILogger<JobScheduler> _logger;
    private readonly SchedulerMetrics? _metrics;
    private readonly IServiceProvider _services;

    public JobScheduler(
        DiscordSocketClient client,
        IJobStore store,
        JobExecutionPipeline pipeline,
        IOptions<SchedulerOptions> options,
        ILogger<JobScheduler> logger,
        IServiceProvider services,
        SchedulerMetrics? metrics = null,
        IDistributedLock? distributedLock = null)
    {
        _client = client;
        _store = store;
        _lock = distributedLock ?? new InMemoryLock();
        _pipeline = pipeline;
        _options = options.Value;
        _logger = logger;
        _services = services;
        _metrics = metrics;
    }

    public async Task<string> ScheduleAsync(IScheduledJob job, CancellationToken ct = default)
    {
        await _store.AddAsync(job, ct);
        _logger.LogDebug(
            "Scheduled job {JobId} ({JobName}) for {NextExecution}",
            job.Id, job.Name, job.NextExecution);

        _metrics?.JobScheduled.Add(1);

        return job.Id;
    }

    public async Task<string> ScheduleAsync(
        Action<ScheduledJobBuilder> configure,
        CancellationToken ct = default)
    {
        var builder = new ScheduledJobBuilder();
        configure(builder);
        var job = builder.Build();
        return await ScheduleAsync(job, ct);
    }

    public async Task<string> ScheduleRecurringAsync(
        Action<RecurringJobBuilder> configure,
        CancellationToken ct = default)
    {
        var builder = RecurringJobBuilder.Create();
        configure(builder);
        var job = builder.Build();
        return await ScheduleAsync(job, ct);
    }

    public async Task<bool> CancelAsync(string jobId, CancellationToken ct = default)
    {
        var job = await _store.GetAsync(jobId, ct);
        if (job is not ScheduledJob scheduled)
            return false;

        var updated = scheduled with
        {
            Status = JobStatus.Cancelled,
            NextExecution = null
        };

        await _store.UpdateAsync(updated, ct);
        _logger.LogInformation("Cancelled job {JobId}", jobId);

        _metrics?.JobCancelled.Add(1);

        return true;
    }

    public async Task<bool> RescheduleAsync(
        string jobId,
        DateTimeOffset newExecutionTime,
        CancellationToken ct = default)
    {
        var job = await _store.GetAsync(jobId, ct);
        if (job is not ScheduledJob scheduled)
            return false;

        var updated = scheduled with
        {
            NextExecution = newExecutionTime,
            Status = JobStatus.Pending,
            LastError = null
        };

        await _store.UpdateAsync(updated, ct);
        _logger.LogInformation("Rescheduled job {JobId} to {NewTime}", jobId, newExecutionTime);

        return true;
    }

    public Task<IScheduledJob?> GetJobAsync(string jobId, CancellationToken ct = default)
        => _store.GetAsync(jobId, ct);

    public async Task<IReadOnlyList<IScheduledJob>> GetPendingJobsAsync(CancellationToken ct = default)
        => await _store.GetPendingAsync(ct);

    public async Task<IReadOnlyList<IScheduledJob>> GetAllJobsAsync(CancellationToken ct = default)
        => await _store.GetAllAsync(ct);

    public async Task<int> GetJobCountAsync(CancellationToken ct = default)
        => await _store.CountAsync(ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobScheduler started at {Timestamp}", DateTimeOffset.UtcNow);

        if (_options.RecoverJobsOnStart)
        {
            await RecoverJobsAsync(stoppingToken);
        }

        RegisterEventTriggers();

        var timerInterval = TimeSpan.FromMilliseconds(_options.PollingIntervalMs);

        using var timer = new PeriodicTimer(timerInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessPendingJobsAsync(stoppingToken);
        }
    }

    private void RegisterEventTriggers()
    {
        _client.UserJoined += HandleUserJoinedAsync;
        _client.MessageReceived += HandleMessageReceivedAsync;
    }

    private async Task HandleUserJoinedAsync(SocketGuildUser user)
    {
        try
        {
            var jobs = await _store.GetAllAsync();
            var matching = jobs
                .Where(j => j.Status == JobStatus.Pending)
                .Where(j => j.Triggers.Any(t =>
                    t is UserJoinedTrigger ujt && ujt.UserId == user.Id))
                .ToList();

            foreach (var job in matching)
            {
                if (!await SatisfiesConditionsAsync(job))
                    continue;

                var lockKey = $"job:{job.Id}";
                if (!await _lock.TryAcquireAsync(lockKey, TimeSpan.FromMinutes(5), CancellationToken.None))
                    continue;

                try
                {
                    await ExecuteJobWithRetryAsync(job, CancellationToken.None);
                }
                finally
                {
                    await _lock.ReleaseAsync(lockKey, CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserJoined trigger for user {UserId}", user.Id);
        }
    }

    private async Task HandleMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;

        try
        {
            var jobs = await _store.GetAllAsync();
            var matching = jobs
                .Where(j => j.Status == JobStatus.Pending)
                .Where(j => j.Triggers.Any(t => MatchesMessageTrigger(t, message)))
                .ToList();

            foreach (var job in matching)
            {
                if (!await SatisfiesConditionsAsync(job))
                    continue;

                var lockKey = $"job:{job.Id}";
                if (!await _lock.TryAcquireAsync(lockKey, TimeSpan.FromMinutes(5), CancellationToken.None))
                    continue;

                try
                {
                    await ExecuteJobWithRetryAsync(job, CancellationToken.None);
                }
                finally
                {
                    await _lock.ReleaseAsync(lockKey, CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MessageReceived trigger");
        }
    }

    private static bool MatchesMessageTrigger(IJobTrigger trigger, SocketMessage message)
    {
        if (trigger is not MessageSentTrigger mst)
            return false;

        if (mst.ChannelId.HasValue && message.Channel.Id != mst.ChannelId.Value)
            return false;

        if (mst.Pattern is not null &&
            !Regex.IsMatch(message.Content, mst.Pattern, RegexOptions.IgnoreCase))
            return false;

        return true;
    }

    private async Task RecoverJobsAsync(CancellationToken ct)
    {
        try
        {
            var allJobs = await _store.GetAllAsync(ct);
            var recoveryCount = 0;

            foreach (var job in allJobs)
            {
                if (job is not ScheduledJob scheduled)
                    continue;

                if (job.Status == JobStatus.Pending && job.NextExecution.HasValue)
                {
                    if (job.ExpiresAt.HasValue && job.ExpiresAt.Value <= DateTimeOffset.UtcNow)
                    {
                        var expired = scheduled with { Status = JobStatus.Completed };
                        await _store.UpdateAsync(expired, ct);
                        _logger.LogInformation("Job {JobId} expired, marked as completed", job.Id);
                        continue;
                    }

                    recoveryCount++;
                    _logger.LogDebug("Recovered job {JobId} ({JobName})", job.Id, job.Name);
                }
            }

            _logger.LogInformation("Recovered {Count} pending jobs on startup", recoveryCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover jobs on startup");
        }
    }

    private async Task ProcessPendingJobsAsync(CancellationToken ct)
    {
        try
        {
            IReadOnlyList<IScheduledJob> pending;
            try
            {
                pending = await _store.GetPendingAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve pending jobs");
                return;
            }

            foreach (var job in pending)
            {
                if (ct.IsCancellationRequested)
                    break;

                if (!await DependenciesMetAsync(job, ct))
                    continue;

                if (!await SatisfiesConditionsAsync(job))
                    continue;

                var lockKey = $"job:{job.Id}";
                if (!await _lock.TryAcquireAsync(lockKey, TimeSpan.FromMinutes(5), ct))
                {
                    _logger.LogDebug("Job {JobId} is locked by another instance, skipping", job.Id);
                    continue;
                }

                try
                {
                    await ExecuteJobWithRetryAsync(job, ct);
                }
                finally
                {
                    await _lock.ReleaseAsync(lockKey, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in job processing loop");
        }
    }

    private async Task<bool> DependenciesMetAsync(IScheduledJob job, CancellationToken ct)
    {
        if (job.Dependencies.Count == 0)
            return true;

        foreach (var depId in job.Dependencies)
        {
            var dep = await _store.GetAsync(depId, ct);
            if (dep is null || dep.Status != JobStatus.Completed)
                return false;
        }

        return true;
    }

    private async Task<bool> SatisfiesConditionsAsync(IScheduledJob job)
    {
        if (job.RunCondition is null)
            return true;

        try
        {
            return await job.RunCondition(_services);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RunCondition failed for job {JobId}", job.Id);
            return false;
        }
    }

    private async Task ExecuteJobWithRetryAsync(IScheduledJob job, CancellationToken ct)
    {
        var attempt = 0;
        var maxAttempts = Math.Max(1, job.MaxRetries + 1);

        while (attempt < maxAttempts)
        {
            if (ct.IsCancellationRequested)
                return;

            attempt++;

            var context = new JobContext(
                job.Id,
                job.Name,
                _client,
                _services,
                ct)
            {
                ScheduledAt = job.NextExecution ?? DateTimeOffset.UtcNow,
                RetryAttempt = attempt - 1
            };

            try
            {
                var sw = Stopwatch.StartNew();

                _pipeline.SetCore(ctx => job.ExecuteAsync(ctx, ct));
                var pipeline = _pipeline.Build();
                var result = await pipeline(context);

                sw.Stop();
                _metrics?.JobExecutionTime.Record(sw.Elapsed.TotalMilliseconds);

                if (result.IsSuccess)
                {
                    await HandleSuccessAsync(job, ct);
                    return;
                }

                _logger.LogWarning(
                    "Job {JobId} failed (attempt {Attempt}/{MaxAttempts}): {Error}",
                    job.Id, attempt, maxAttempts, result.Error);

                _metrics?.JobFailures.Add(1);

                if (attempt < maxAttempts && job.RetryDelay.HasValue)
                {
                    if (job is ScheduledJob scheduled)
                    {
                        await _store.UpdateAsync(scheduled with { LastError = result.Error }, ct);
                    }
                    await Task.Delay(job.RetryDelay.Value, ct);
                }
                else
                {
                    await _store.MarkFailedAsync(job.Id, result.Error ?? "Unknown error", ct);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Job {JobId} was cancelled", job.Id);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Job {JobId} threw an exception on attempt {Attempt}",
                    job.Id, attempt);

                _metrics?.JobFailures.Add(1);

                if (attempt >= maxAttempts)
                {
                    await _store.MarkFailedAsync(job.Id, ex.ToString(), ct);
                    return;
                }

                if (job.RetryDelay.HasValue)
                    await Task.Delay(job.RetryDelay.Value, ct);
            }
        }
    }

    private async Task HandleSuccessAsync(IScheduledJob job, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(job.CronExpression))
        {
            var next = CronParser.GetNextOccurrence(
                job.CronExpression,
                DateTimeOffset.UtcNow);

            if (job is ScheduledJob scheduled)
            {
                var updated = scheduled with
                {
                    Status = JobStatus.Pending,
                    LastExecutedAt = DateTimeOffset.UtcNow,
                    ExecutionCount = scheduled.ExecutionCount + 1,
                    NextExecution = next,
                    LastError = null
                };

                await _store.UpdateAsync(updated, ct);
            }

            _logger.LogDebug(
                "Recurring job {JobId} rescheduled for {NextExecution}",
                job.Id, next);
        }
        else
        {
            await _store.MarkCompletedAsync(job.Id, ct);
            _logger.LogInformation("One-time job {JobId} completed", job.Id);
        }

        _metrics?.JobsCompleted.Add(1);

        await TriggerDependentJobsAsync(job.Id, ct);
    }

    private async Task TriggerDependentJobsAsync(string completedJobId, CancellationToken ct)
    {
        try
        {
            var all = await _store.GetAllAsync(ct);
            var dependents = all
                .Where(j => j.Dependencies.Contains(completedJobId) && j.Status == JobStatus.Pending)
                .ToList();

            foreach (var dep in dependents)
            {
                if (await DependenciesMetAsync(dep, ct) && await SatisfiesConditionsAsync(dep))
                {
                    _logger.LogDebug(
                        "Triggering dependent job {JobId} after {CompletedJobId}",
                        dep.Id, completedJobId);

                    if (dep is ScheduledJob scheduled)
                    {
                        var updated = scheduled with { NextExecution = DateTimeOffset.UtcNow };
                        await _store.UpdateAsync(updated, ct);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger dependent jobs for {JobId}", completedJobId);
        }
    }
}