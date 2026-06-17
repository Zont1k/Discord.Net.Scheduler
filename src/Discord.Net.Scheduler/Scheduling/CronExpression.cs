using System.Collections;

namespace Discord.Net.Scheduler.Scheduling;

public readonly record struct CronExpression
{
    public BitArray Minutes { get; }
    public BitArray Hours { get; }
    public BitArray DaysOfMonth { get; }
    public BitArray Months { get; }
    public BitArray DaysOfWeek { get; }

    public CronExpression(BitArray minutes, BitArray hours, BitArray daysOfMonth, BitArray months, BitArray daysOfWeek)
    {
        Minutes = minutes;
        Hours = hours;
        DaysOfMonth = daysOfMonth;
        Months = months;
        DaysOfWeek = daysOfWeek;
    }

    public DateTimeOffset GetNextOccurrence(DateTimeOffset from)
    {
        var current = from.ToUniversalTime().AddMinutes(1).UtcDateTime;
        var maxDate = current.AddYears(2);

        while (current < maxDate)
        {
            if (!Months[current.Month - 1])
            {
                current = new DateTime(current.Year, current.Month + 1, 1, 0, 0, 0, DateTimeKind.Utc);
                continue;
            }

            var dayOfWeek = (int)current.DayOfWeek;
            var dayAdjusted = dayOfWeek is 0 ? 0 : dayOfWeek;

            if (!DaysOfMonth[current.Day - 1] || !DaysOfWeek[dayAdjusted])
            {
                current = current.AddDays(1)
                    .AddHours(-current.Hour)
                    .AddMinutes(-current.Minute);
                continue;
            }

            if (!Hours[current.Hour])
            {
                current = current.AddHours(1).AddMinutes(-current.Minute);
                continue;
            }

            if (!Minutes[current.Minute])
            {
                current = current.AddMinutes(1);
                continue;
            }

            return new DateTimeOffset(current, TimeSpan.Zero);
        }

        throw new InvalidOperationException("No future occurrence found within 2 years.");
    }

    public IEnumerable<DateTimeOffset> GetNextOccurrences(DateTimeOffset from, int count)
    {
        var current = from;
        for (var i = 0; i < count; i++)
        {
            var next = GetNextOccurrence(current);
            yield return next;
            current = next.AddSeconds(1);
        }
    }

    public bool Matches(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        var dt = utc.DateTime;

        return Minutes[dt.Minute]
            && Hours[dt.Hour]
            && DaysOfMonth[dt.Day - 1]
            && Months[dt.Month - 1]
            && DaysOfWeek[(int)dt.DayOfWeek is 0 ? 0 : (int)dt.DayOfWeek];
    }
}
