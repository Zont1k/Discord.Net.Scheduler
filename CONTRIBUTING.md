# Contributing

Thanks for your interest in Discord.Net.Scheduler!

## How to contribute

1. Fork the repo
2. Create a feature branch (`git checkout -b feature/my-thing`)
3. Commit your changes (`git commit -am "Add my thing"`)
4. Push the branch (`git push origin feature/my-thing`)
5. Open a Pull Request

## Development

```sh
dotnet build
dotnet test
dotnet pack
```

Tests must pass before merging. Add tests for new features.

## Code style

- Use file-scoped namespaces
- Use `var` where type is obvious
- No `#region` blocks
- Follow existing patterns in the codebase

## Project structure

```
src/Discord.Net.Scheduler/          # core library
src/Discord.Net.Scheduler.Redis/    # Redis store
src/Discord.Net.Scheduler.EntityFrameworkCore/  # EF Core store
src/Discord.Net.Scheduler.SourceGenerator/      # source generator
tests/Discord.Net.Scheduler.Tests/  # xunit tests
benchmarks/Discord.Net.Scheduler.Benchmarks/    # BenchmarkDotNet
samples/SchedulerSample/            # example bot
```

## Questions

Open a [discussion](https://github.com/Zont1k/Discord.Net.Scheduler/discussions).
