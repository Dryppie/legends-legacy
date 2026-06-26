namespace Domain.Models.Colosseum;

public static class ArenaCalendar
{
    public static DateTimeOffset GetCurrentWeeklyResetStart(DateTimeOffset now)
    {
        var date = now.UtcDateTime.Date;
        var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return new DateTimeOffset(date.AddDays(-daysSinceMonday), TimeSpan.Zero);
    }
}
