namespace Domain.Models.Nobility;

public sealed record NobilityRetentionWindow(DateTimeOffset From, DateTimeOffset Until);

public static class NobilityRetention
{
    // Historical endpoints are immutable after expiry. Reconciliation timing cannot shrink paid work.
    // The persisted action cursor consumes these windows once; daily reward sweeps never move that cursor.
    public static IReadOnlyList<NobilityRetentionWindow> Windows(IReadOnlyList<NobilityCoverage> coverage, DateTimeOffset now, int freeHours = 24)
    {
        var windows = new List<NobilityRetentionWindow>();
        foreach (var period in coverage.Where(x => x.StartsAt <= now))
        {
            windows.Add(new(period.StartsAt.AddHours(-freeHours), period.StartsAt));
            var until = period.EndsAt <= now ? period.EndsAt : now.AddTicks(1);
            var from = (period.EndsAt <= now ? period.EndsAt : now).AddHours(-168);
            windows.Add(new(from > period.StartsAt ? from : period.StartsAt, until));
        }
        var latest = coverage.Where(x => x.StartsAt <= now).MaxBy(x => x.StartsAt);
        if (latest is null || latest.EndsAt <= now)
        {
            var from = now.AddHours(-freeHours);
            if (latest is not null && latest.EndsAt > from) from = latest.EndsAt;
            windows.Add(new(from, now.AddTicks(1)));
        }
        var merged = new List<NobilityRetentionWindow>();
        foreach (var window in windows.Where(x => x.From < x.Until).OrderBy(x => x.From))
        {
            if (merged.Count > 0 && window.From <= merged[^1].Until)
                merged[^1] = merged[^1] with { Until = window.Until > merged[^1].Until ? window.Until : merged[^1].Until };
            else merged.Add(window);
        }
        return merged;
    }
}
