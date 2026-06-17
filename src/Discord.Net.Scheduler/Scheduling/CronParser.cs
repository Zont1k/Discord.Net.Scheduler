using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Discord.Net.Scheduler.Scheduling;

public static partial class CronParser
{
    private static readonly ConcurrentDictionary<string, CronExpression> Cache = new(StringComparer.Ordinal);

    public static bool IsValid(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        return TryParse(expression, out _);
    }

    public static CronExpression Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Cron expression cannot be null or empty.", nameof(expression));

        if (Cache.TryGetValue(expression, out var cached))
            return cached;

        if (TryParseInternal(expression, out var result))
        {
            Cache[expression] = result;
            return result;
        }

        throw new FormatException($"Invalid cron expression: '{expression}'.");
    }

    public static bool TryParse(string expression, out CronExpression result)
    {
        return TryParseInternal(expression, out result);
    }

    public static DateTimeOffset GetNextOccurrence(string expression, DateTimeOffset from)
    {
        var cron = Parse(expression);
        return cron.GetNextOccurrence(from);
    }

    public static IEnumerable<DateTimeOffset> GetNextOccurrences(string expression, DateTimeOffset from, int count)
    {
        var cron = Parse(expression);
        return cron.GetNextOccurrences(from, count);
    }

    private static bool TryParseInternal(string expression, out CronExpression result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(expression))
            return false;

        var expr = expression.Trim();

        if (NamedExpressions.TryGetValue(expr, out var named))
        {
            try
            {
                result = ParseInternal(named);
                return true;
            }
            catch
            {
                return false;
            }
        }

        var match = CreateCronRegex().Match(expr);
        if (!match.Success)
            return false;

        try
        {
            result = ParseInternal(expr);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static CronExpression ParseInternal(string expression)
    {
        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
            throw new FormatException($"Cron expression must have exactly 5 fields, got {parts.Length}.");

        return new CronExpression(
            ParseField(parts[0], 0, 59),
            ParseField(parts[1], 0, 23),
            ParseField(parts[2], 1, 31),
            ParseField(parts[3], 1, 12),
            ParseField(parts[4], 0, 7)
        );
    }

    internal static BitArray ParseField(string field, int min, int max)
    {
        var bits = new BitArray(max - min + 1);

        if (field == "*")
        {
            bits.SetAll(true);
            return bits;
        }

        foreach (var segment in field.Split(','))
        {
            var stepMatch = StepRegex().Match(segment);
            if (stepMatch.Success)
            {
                var rangePart = stepMatch.Groups[1].Value;
                var step = int.Parse(stepMatch.Groups[2].Value, CultureInfo.InvariantCulture);

                int rangeStart, rangeEnd;
                if (rangePart == "*")
                {
                    rangeStart = min;
                    rangeEnd = max;
                }
                else if (rangePart.Contains('-'))
                {
                    var rangeParts = rangePart.Split('-');
                    rangeStart = int.Parse(rangeParts[0], CultureInfo.InvariantCulture);
                    rangeEnd = int.Parse(rangeParts[1], CultureInfo.InvariantCulture);
                }
                else
                {
                    rangeStart = int.Parse(rangePart, CultureInfo.InvariantCulture);
                    rangeEnd = max;
                }

                for (var i = rangeStart; i <= rangeEnd; i += step)
                {
                    if (i >= min && i <= max)
                        bits[i - min] = true;
                }
            }
            else if (segment == "*")
            {
                bits.SetAll(true);
            }
            else if (segment.Contains('-'))
            {
                var rangeParts = segment.Split('-');
                var rangeStart = int.Parse(rangeParts[0], CultureInfo.InvariantCulture);
                var rangeEnd = int.Parse(rangeParts[1], CultureInfo.InvariantCulture);
                for (var i = rangeStart; i <= rangeEnd; i++)
                {
                    if (i >= min && i <= max)
                        bits[i - min] = true;
                }
            }
            else if (int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                value = value is 0 or 7 ? 0 : value;

                if (value >= min && value <= max)
                    bits[value - min] = true;
                else
                    throw new FormatException($"Value {segment} is out of range ({min}-{max}).");
            }
        }

        return bits;
    }

    private static readonly Dictionary<string, string> NamedExpressions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["@yearly"] = "0 0 1 1 *",
        ["@annually"] = "0 0 1 1 *",
        ["@monthly"] = "0 0 1 * *",
        ["@weekly"] = "0 0 * * 0",
        ["@daily"] = "0 0 * * *",
        ["@midnight"] = "0 0 * * *",
        ["@hourly"] = "0 * * * *"
    };

    [GeneratedRegex(@"^(\*|(?:\d+|\*)(?:-(?:\d+|\*))?)\/(\d+)$")]
    private static partial Regex StepRegex();

    [GeneratedRegex(@"^(\*/\d+|\*|(?:\d+(?:-\d+)?(?:\/\d+)?)(?:,(?:\d+(?:-\d+)?))*)\s+(\*/\d+|\*|(?:\d+(?:-\d+)?(?:\/\d+)?)(?:,(?:\d+(?:-\d+)?))*)\s+(\*/\d+|\*|(?:\d+(?:-\d+)?(?:\/\d+)?)(?:,(?:\d+(?:-\d+)?))*)\s+(\*/\d+|\*|(?:\d+(?:-\d+)?(?:\/\d+)?)(?:,(?:\d+(?:-\d+)?))*)\s+(\*/\d+|\*|(?:\d+(?:-\d+)?(?:\/\d+)?)(?:,(?:\d+(?:-\d+)?))*)$")]
    private static partial Regex CreateCronRegex();
}
