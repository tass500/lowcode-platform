using LowCodePlatform.Backend.Services;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class WorkflowRestrictedCronTests
{
    [Theory]
    [InlineData("* * * * *", "2026-03-27T10:05:00Z", "2026-03-27T10:06:00Z")]
    [InlineData("*/5 * * * *", "2026-03-27T10:04:30Z", "2026-03-27T10:05:00Z")]
    [InlineData("*/5 * * * *", "2026-03-27T10:05:00Z", "2026-03-27T10:10:00Z")]
    [InlineData("15 * * * *", "2026-03-27T10:14:00Z", "2026-03-27T10:15:00Z")]
    [InlineData("15 * * * *", "2026-03-27T10:15:00Z", "2026-03-27T11:15:00Z")]
    [InlineData("30 9 * * *", "2026-03-27T08:00:00Z", "2026-03-27T09:30:00Z")]
    [InlineData("30 9 * * *", "2026-03-27T09:30:00Z", "2026-03-28T09:30:00Z")]
    public void GetNextUtcStrictlyAfter_matches_expected(string cron, string utcNowIso, string expectedIso)
    {
        var utcNow = DateTime.Parse(utcNowIso, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var expected = DateTime.Parse(expectedIso, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var next = WorkflowRestrictedCron.GetNextUtcStrictlyAfter(cron, utcNow);
        Assert.Equal(DateTimeKind.Utc, next.Kind);
        Assert.Equal(expected, next);
    }

    [Fact]
    public void TryParse_rejects_invalid()
    {
        Assert.False(WorkflowRestrictedCron.TryParse(null, out var e1, out _));
        Assert.Equal("schedule_cron_missing", e1);

        Assert.False(WorkflowRestrictedCron.TryParse("0 0 1 * *", out var e2, out _));
        Assert.Equal("schedule_cron_day_month_dow_must_be_star", e2);
    }
}
