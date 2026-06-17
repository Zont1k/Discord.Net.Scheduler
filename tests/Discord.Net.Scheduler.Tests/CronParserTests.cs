using Discord.Net.Scheduler.Scheduling;

namespace Discord.Net.Scheduler.Tests;

public sealed class CronParserTests
{
    [Theory]
    [InlineData("0 0 * * *", true)]
    [InlineData("*/5 * * * *", true)]
    [InlineData("0 8 * * 1-5", true)]
    [InlineData("0 0 1 1 *", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("0 0 * * * *", false)]
    [InlineData("60 0 * * *", false)]
    [InlineData("0 24 * * *", false)]
    public void IsValid_ShouldValidateCronExpression(string? expression, bool expected)
    {
        var result = CronParser.IsValid(expression!);
        result.Should().Be(expected);
    }

    [Fact]
    public void Parse_ShouldReturnExpression_ForValidInput()
    {
        var result = CronParser.Parse("0 0 * * *");
        result.Should().NotBeNull();
    }

    [Fact]
    public void Parse_ShouldThrow_ForInvalidInput()
    {
        var act = () => CronParser.Parse("invalid");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_ShouldHandleNamedExpressions()
    {
        var yearly = CronParser.Parse("@yearly");
        yearly.Should().NotBeNull();

        var daily = CronParser.Parse("@daily");
        daily.Should().NotBeNull();

        var hourly = CronParser.Parse("@hourly");
        hourly.Should().NotBeNull();
    }

    [Fact]
    public void GetNextOccurrence_ShouldReturnFutureDate_ForDailyExpression()
    {
        var now = new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.Zero);
        var next = CronParser.GetNextOccurrence("30 14 * * *", now);

        next.Year.Should().Be(2026);
        next.Month.Should().Be(6);
        next.Day.Should().Be(17);
        next.Hour.Should().Be(14);
        next.Minute.Should().Be(30);
    }

    [Fact]
    public void GetNextOccurrence_ShouldReturnNextDay_ForAfterHours()
    {
        var now = new DateTimeOffset(2026, 6, 17, 23, 0, 0, TimeSpan.Zero);
        var next = CronParser.GetNextOccurrence("0 8 * * *", now);

        next.Year.Should().Be(2026);
        next.Month.Should().Be(6);
        next.Day.Should().Be(18);
        next.Hour.Should().Be(8);
        next.Minute.Should().Be(0);
    }

    [Fact]
    public void GetNextOccurrence_ShouldHandleWeekdaysOnly()
    {
        // June 17, 2026 is a Wednesday
        var now = new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero);
        var next = CronParser.GetNextOccurrence("0 9 * * 1-5", now);

        next.DayOfWeek.Should().NotBe(DayOfWeek.Saturday);
        next.DayOfWeek.Should().NotBe(DayOfWeek.Sunday);
    }

    [Fact]
    public void GetNextOccurrence_ShouldHandleStepValues()
    {
        var now = new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.Zero);
        var next = CronParser.GetNextOccurrence("*/15 * * * *", now);

        next.Minute.Should().Be(15);
    }

    [Fact]
    public void GetNextOccurrences_ShouldReturnMultiple()
    {
        var now = new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.Zero);
        var occurrences = CronParser.GetNextOccurrences("0 * * * *", now, 5).ToArray();

        occurrences.Should().HaveCount(5);
        occurrences[0].Hour.Should().Be(11);
        occurrences[1].Hour.Should().Be(12);
        occurrences[2].Hour.Should().Be(13);
    }
}
