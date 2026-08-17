namespace Services.LL.CharacterActions;

public sealed record ActionResolutionPlan(
    int DueCount,
    int ProcessCount,
    bool HasMoreDueWork);

public static class ActionScheduleCalculator
{
    public static ActionResolutionPlan Calculate(
        DateTimeOffset? nextResolutionAtUtc,
        DateTimeOffset now,
        TimeSpan interval,
        int maximumPerResolution)
    {
        if (nextResolutionAtUtc is null || now < nextResolutionAtUtc.Value)
        {
            return new ActionResolutionPlan(0, 0, false);
        }

        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval));
        if (maximumPerResolution <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumPerResolution));

        var elapsedTicks = (now - nextResolutionAtUtc.Value).Ticks;
        var dueCount = checked(1 + (int)(elapsedTicks / interval.Ticks));
        var processCount = Math.Min(dueCount, maximumPerResolution);
        return new ActionResolutionPlan(
            dueCount,
            processCount,
            dueCount > processCount);
    }
}
