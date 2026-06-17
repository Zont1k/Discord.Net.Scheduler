using Discord;
using Discord.Net.Scheduler;
using Discord.Net.Scheduler.Scheduling;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole();

// Manual DiscordSocketClient registration instead of AddDiscordSocketClient
builder.Services.AddSingleton<DiscordSocketClient>(_ =>
{
    var client = new DiscordSocketClient(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
        LogGatewayIntentWarnings = false
    });
    return client;
});

builder.Services.AddDiscordScheduler(options =>
{
    options.PollingIntervalMs = 1000;
    options.RecoverJobsOnStart = true;
});

var host = builder.Build();

var client = host.Services.GetRequiredService<DiscordSocketClient>();
var scheduler = host.Services.GetRequiredService<JobScheduler>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

client.Log += msg =>
{
    Console.WriteLine($"[Discord] {msg}");
    return Task.CompletedTask;
};

client.Ready += async () =>
{
    logger.LogInformation("Bot is ready! Setting up scheduled jobs...");

    // The channel ID should be replaced with an actual channel ID
    // or obtained from a configuration file.
    var channelId = 0ul;

    // Example 1: Schedule a one-time message with delay
    var jobId = await scheduler.ScheduleAsync(job => job
        .SendMessage(channelId, "This message was scheduled to arrive after a delay!")
        .In(TimeSpan.FromSeconds(30))
        .WithName("delayed-message"));

    logger.LogInformation("Scheduled one-time job: {JobId}", jobId);

    // Example 2: Schedule a recurring daily job using cron
    var dailyJobId = await scheduler.ScheduleRecurringAsync(job => job
        .SendMessage(channelId, "Daily scheduled message")
        .WithCron("0 8 * * *")
        .WithName("daily-message")
        .WithTimezone(TimeZoneInfo.Utc));

    logger.LogInformation("Scheduled daily cron job: {DailyJobId}", dailyJobId);

    // Example 3: Custom job with DI support
    var customJobId = await scheduler.ScheduleAsync(job => job
        .Execute(async (ctx, ct) =>
        {
            ctx.GetService<ILogger<Program>>()
                .LogInformation("Custom job executed at {Time}", DateTimeOffset.UtcNow);
            return JobResult.Success(TimeSpan.Zero);
        })
        .WithName("custom-job")
        .Every(TimeSpan.FromHours(1)));

    logger.LogInformation("Scheduled custom recurring job: {CustomJobId}", customJobId);
};

await host.RunAsync();
