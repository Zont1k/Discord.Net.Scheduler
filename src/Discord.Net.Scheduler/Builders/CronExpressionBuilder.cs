namespace Discord.Net.Scheduler.Builders;

public sealed class CronExpressionBuilder
{
    private int? _minute;
    private int? _hour;
    private int? _dayOfMonth;
    private int? _month;
    private int? _dayOfWeek;
    private TimeZoneInfo? _timezone;
    private int _minuteInterval = -1;
    private int _hourInterval = -1;

    private CronExpressionBuilder() { }

    public static CronExpressionBuilder Create() => new();

    public CronExpressionBuilder EveryMinute()
    {
        _minute = -1;
        _hour = null; _dayOfMonth = null; _month = null; _dayOfWeek = null;
        return this;
    }

    public CronExpressionBuilder EveryHour()
    {
        _minute = 0;
        _hour = -1;
        return this;
    }

    public CronExpressionBuilder Hourly() => EveryHour();

    public CronExpressionBuilder EveryDay()
    {
        _minute = 0; _hour = 0;
        _dayOfMonth = -1;
        return this;
    }

    public CronExpressionBuilder Daily() => EveryDay();

    public CronExpressionBuilder EveryWeekday()
    {
        _minute = 0; _hour = 0;
        _dayOfWeek = -1;
        return this;
    }

    public CronExpressionBuilder OnDays(params DayOfWeek[] days)
    {
        _dayOfWeek = 0;
        foreach (var d in days)
            _dayOfWeek |= (int)Math.Pow(2, (int)d);
        return this;
    }

    public CronExpressionBuilder EveryMonth()
    {
        _minute = 0; _hour = 0; _dayOfMonth = 1;
        _month = -1;
        return this;
    }

    public CronExpressionBuilder Monthly() => EveryMonth();

    public CronExpressionBuilder At(int hour, int minute)
    {
        _hour = hour;
        _minute = minute;
        return this;
    }

    public CronExpressionBuilder InTimezone(TimeZoneInfo tz)
    {
        _timezone = tz;
        return this;
    }

    public string Build()
    {
        var minute = _minuteInterval > 0 ? $"*/{_minuteInterval}" : _minute switch
        {
            -1 => "*",
            null => "*",
            _ => _minute.Value.ToString()
        };

        var hour = _hourInterval > 0 ? $"*/{_hourInterval}" : _hour switch
        {
            -1 => "*",
            null => "*",
            _ => _hour.Value.ToString()
        };

        var dom = _dayOfMonth switch
        {
            -1 => "*",
            null => "*",
            _ => _dayOfMonth.Value.ToString()
        };

        var month = _month switch
        {
            -1 => "*",
            null => "*",
            _ => _month.Value.ToString()
        };

        var dow = _dayOfWeek switch
        {
            -1 => "*",
            null => "*",
            _ => _dayOfWeek.Value.ToString()
        };

        return $"{minute} {hour} {dom} {month} {dow}";
    }

    public static implicit operator string(CronExpressionBuilder builder) => builder.Build();
}
