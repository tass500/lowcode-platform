using System.Globalization;

namespace LowCodePlatform.Backend.Services;

/// <summary>
/// MVP UTC cron: 5 fields (minute hour day month dow). Day, month, and dow must be <c>*</c>.
/// Supported: <c>* * * * *</c> (every minute), <c>*/N * * * *</c> (1≤N≤59),
/// <c>M * * * *</c> (hourly at minute M), <c>M H * * *</c> (daily at H:M).
/// </summary>
public static class WorkflowRestrictedCron
{
    public static bool TryParse(string? expression, out string? error, out Func<DateTime, DateTime> getNextUtcStrictlyAfter)
    {
        getNextUtcStrictlyAfter = _ => DateTime.UtcNow;
        error = null;

        if (string.IsNullOrWhiteSpace(expression))
        {
            error = "schedule_cron_missing";
            return false;
        }

        var parts = expression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5)
        {
            error = "schedule_cron_must_have_five_fields";
            return false;
        }

        if (parts[2] != "*" || parts[3] != "*" || parts[4] != "*")
        {
            error = "schedule_cron_day_month_dow_must_be_star";
            return false;
        }

        var minuteField = parts[0];
        var hourField = parts[1];

        if (minuteField == "*" && hourField == "*")
        {
            getNextUtcStrictlyAfter = NextEveryMinute;
            return true;
        }

        if (minuteField.StartsWith("*/", StringComparison.Ordinal) && hourField == "*")
        {
            var nStr = minuteField[2..];
            if (!int.TryParse(nStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n is < 1 or > 59)
            {
                error = "schedule_cron_step_minute_invalid";
                return false;
            }

            getNextUtcStrictlyAfter = t => NextEveryNMinutes(t, n);
            return true;
        }

        if (hourField == "*" && int.TryParse(minuteField, NumberStyles.Integer, CultureInfo.InvariantCulture, out var m) && m is >= 0 and <= 59)
        {
            getNextUtcStrictlyAfter = t => NextHourlyAtMinute(t, m);
            return true;
        }

        if (int.TryParse(minuteField, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dm) &&
            dm is >= 0 and <= 59 &&
            int.TryParse(hourField, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) &&
            h is >= 0 and <= 23)
        {
            getNextUtcStrictlyAfter = t => NextDailyAt(t, h, dm);
            return true;
        }

        error = "schedule_cron_pattern_not_supported";
        return false;
    }

    public static DateTime GetNextUtcStrictlyAfter(string expression, DateTime utcNow)
    {
        if (!TryParse(expression, out var err, out var next))
            throw new InvalidOperationException(err ?? "schedule_cron_invalid");
        return next(EnsureUtc(utcNow));
    }

    private static DateTime EnsureUtc(DateTime t)
        => t.Kind == DateTimeKind.Utc ? t : DateTime.SpecifyKind(t.ToUniversalTime(), DateTimeKind.Utc);

    /// <summary>First whole minute strictly after <paramref name="utcNow"/>.</summary>
    private static DateTime NextEveryMinute(DateTime utcNow)
    {
        var t = EnsureUtc(utcNow);
        var floor = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, DateTimeKind.Utc);
        return t > floor ? floor.AddMinutes(1) : floor.AddMinutes(1);
    }

    private static DateTime NextEveryNMinutes(DateTime utcNow, int n)
    {
        var t = EnsureUtc(utcNow);
        var s = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0, DateTimeKind.Utc);
        if (t >= s)
            s = s.AddMinutes(1);

        while (s.Minute % n != 0)
            s = s.AddMinutes(1);

        return s;
    }

    private static DateTime NextHourlyAtMinute(DateTime utcNow, int minute)
    {
        var t = EnsureUtc(utcNow);
        var candidate = new DateTime(t.Year, t.Month, t.Day, t.Hour, minute, 0, DateTimeKind.Utc);
        if (candidate <= t)
            candidate = candidate.AddHours(1);
        return candidate;
    }

    private static DateTime NextDailyAt(DateTime utcNow, int hour, int minute)
    {
        var t = EnsureUtc(utcNow);
        var candidate = new DateTime(t.Year, t.Month, t.Day, hour, minute, 0, DateTimeKind.Utc);
        if (candidate <= t)
            candidate = candidate.AddDays(1);
        return candidate;
    }
}
