using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Discord.Net.Scheduler.Builders;
using Discord.Net.Scheduler.Jobs;
using Discord.Net.Scheduler.Scheduling;
using Discord.Net.Scheduler.Scheduling.JobStore;

BenchmarkRunner.Run<SchedulerBenchmarks>();

[MemoryDiagnoser]
public class SchedulerBenchmarks
{
    private InMemoryJobStore _store = default!;
    private List<string> _jobIds = [];

    [Params(100, 1000, 10000, 50000)]
    public int JobCount { get; set; }

    private static int _builderCounter;

    [IterationSetup]
    public void Setup()
    {
        _store = new InMemoryJobStore();
        _jobIds = [];

        for (var i = 0; i < JobCount; i++)
        {
            var id = $"job_{i}";
            _jobIds.Add(id);

            var job = new ScheduledJobBuilder()
                .WithId(id)
                .WithName($"Benchmark Job {i}")
                .SendMessage(123456789, $"Hello from job {i}")
                .WithDelay(TimeSpan.FromHours(1))
                .Build();

            _store.AddAsync(job).GetAwaiter().GetResult();
        }
    }

    [Benchmark]
    public async Task<int> GetAllJobs()
    {
        var jobs = await _store.GetAllAsync();
        return jobs.Count;
    }

    [Benchmark]
    public async Task<int> GetPendingJobs()
    {
        var jobs = await _store.GetPendingAsync();
        return jobs.Count;
    }

    [Benchmark]
    public async Task<IScheduledJob?> LookupSingleJob()
    {
        return await _store.GetAsync("job_0");
    }

    [Benchmark]
    public async Task AddAndRemoveJob()
    {
        var id = $"bench_{Interlocked.Increment(ref _builderCounter)}";
        var job = new ScheduledJobBuilder()
            .WithId(id)
            .SendMessage(123456789, "bench")
            .WithDelay(TimeSpan.FromMinutes(5))
            .Build();

        await _store.AddAsync(job);
        await _store.RemoveAsync(id);
    }

    [Benchmark]
    public string SerializeJob()
    {
        var job = new ScheduledJobBuilder()
            .WithId("serialize_test")
            .SendMessage(123456789, "serialize me")
            .WithDelay(TimeSpan.FromMinutes(10))
            .Build();

        var wrapper = new JobWrapper(job);
        return wrapper.Serialize();
    }

    [Benchmark]
    public IScheduledJob? DeserializeJob()
    {
        const string json = """
            {"type":"SendMessageJob","id":"deser_test","channelId":123456789,"message":"hello","isTTS":false,"status":0,"cronExpression":null,"maxRetries":3,"retryDelay":"00:00:30"}
            """;
        return JobWrapper.Deserialize(json)?.ToJob();
    }
}