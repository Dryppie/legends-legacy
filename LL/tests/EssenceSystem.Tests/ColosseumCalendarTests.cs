using Domain.Models.Colosseum;

public sealed class ColosseumCalendarTests
{
    [Fact]
    public void GetCurrentWeeklyResetStart_ReturnsCurrentMondayUtc_WhenNowIsAfterWeeklyReset()
    {
        var now = new DateTimeOffset(2026, 6, 25, 12, 0, 0, TimeSpan.Zero);

        var reset = ArenaCalendar.GetCurrentWeeklyResetStart(now);

        Assert.Equal(new DateTimeOffset(2026, 6, 22, 0, 0, 0, TimeSpan.Zero), reset);
    }

    [Fact]
    public void GetCurrentWeeklyResetStart_ReturnsSameInstant_WhenNowIsExactlyMondayUtc()
    {
        var now = new DateTimeOffset(2026, 6, 22, 0, 0, 0, TimeSpan.Zero);

        var reset = ArenaCalendar.GetCurrentWeeklyResetStart(now);

        Assert.Equal(now, reset);
    }

    [Fact]
    public void GetCurrentWeeklyResetStart_UsesUtcDate_ForOffsetInputs()
    {
        var now = new DateTimeOffset(2026, 6, 22, 1, 30, 0, TimeSpan.FromHours(2));

        var reset = ArenaCalendar.GetCurrentWeeklyResetStart(now);

        Assert.Equal(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero), reset);
    }
}
