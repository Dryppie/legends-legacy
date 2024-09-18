namespace Common.DateTimeProvider;
public interface IDateTimeProviderService
{
    DateTimeOffset Now();
    DateTimeOffset NowInCopenhagenTimezone();
}