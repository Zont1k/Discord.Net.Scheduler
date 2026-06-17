using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Discord.Net.Scheduler.Scheduling;

public sealed class JobContext
{
    private readonly IServiceProvider _services;

    public JobContext(
        string jobId,
        string? jobName,
        DiscordSocketClient client,
        IServiceProvider services,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        JobId = jobId;
        JobName = jobName;
        Client = client;
        _services = services;
        CancellationToken = cancellationToken;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    public string JobId { get; }
    public string? JobName { get; }
    public DiscordSocketClient Client { get; }
    public CancellationToken CancellationToken { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public TService GetService<TService>() where TService : notnull
        => _services.GetRequiredService<TService>();

    public TService? GetOptionalService<TService>() where TService : class
        => _services.GetService<TService>();

    public DateTimeOffset ScheduledAt { get; init; }
    public DateTimeOffset ExecutingAt { get; internal set; }
    public int RetryAttempt { get; internal set; }
}
