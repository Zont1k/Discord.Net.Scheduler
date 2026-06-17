using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Pipeline;

public delegate Task<JobResult> JobMiddlewareDelegate(JobContext context);
