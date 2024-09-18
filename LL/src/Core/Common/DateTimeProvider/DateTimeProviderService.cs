namespace Common.DateTimeProvider;
public class DateTimeProviderService : IDateTimeProviderService
{
    public DateTimeOffset Now()
    {
        return DateTimeOffset.UtcNow;
    }

    public DateTimeOffset NowInCopenhagenTimezone()
    {
        TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tzi);
    }
}