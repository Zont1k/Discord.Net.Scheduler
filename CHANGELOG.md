# Changelog

## v1.0.0 (2026-06-17)

### Added
- Core scheduling framework (`JobScheduler`, `IScheduledJob`, `ScheduledJob`)
- Cron parser with 5-field expressions, step values, named shortcuts
- Job types: `SendMessageJob`, `SendEmbedJob`, `EditMessageJob`, `CustomJob`
- Fluent builders: `ScheduledJobBuilder`, `RecurringJobBuilder`
- `Hourly()`/`Daily()`/`Weekly()`/`Monthly()` shorthand
- Middleware pipeline (`JobExecutionPipeline`, `IJobMiddleware`)
- Built-in middleware: `LoggingMiddleware`, `ErrorHandlingMiddleware`
- OpenTelemetry metrics (`SchedulerMetrics`)
- Source generator (`[CronJob]` attribute → `AddGeneratedCronJobs()`)
- In-memory job store (`InMemoryJobStore`)
- Redis job store (`RedisJobStore`)
- EF Core job store (`EfJobStore` + `SchedulerDbContext` + `JobEntity`)
- Event triggers: `.WhenUserJoins()`, `.WhenMessageSent()`, `.AfterJob()`
- Job dependencies: `.After()`, `.RunIf()`
- DI extensions: `AddDiscordScheduler()`, `AddRedisJobStore()`, `AddEfCoreJobStore()`
- NuGet packages: `Discord.Net.Scheduler`, `Discord.Net.Scheduler.Redis`, `Discord.Net.Scheduler.EntityFrameworkCore`
- 43 unit tests
- BenchmarkDotNet project with performance numbers
- CI pipeline (build + test + NuGet publish on tag)
- README with full documentation, benchmarks, badges
