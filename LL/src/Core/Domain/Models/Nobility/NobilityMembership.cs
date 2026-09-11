namespace Domain.Models.Nobility;

public sealed class NobilityMembership
{
    public Guid AccountId { get; set; }
    public Guid RewardCharacterId { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public bool ShowBadge { get; set; } = true;
    // Retained for historical reward audit; no longer advanced or scheduled.
    public DateOnly? DailyRewardsThrough { get; set; }
    public DateTimeOffset NextDailyRewardAt { get; set; }
    public List<NobilityCoverage> Coverage { get; set; } = [];

    public NobilityCoverage? At(DateTimeOffset at) => Coverage
        .FirstOrDefault(period => period.StartsAt <= at && at < period.EndsAt);

    public NobilityExtension Preview(int months, DateTimeOffset now)
    {
        if (months <= 0 || months > 1200) throw new ArgumentOutOfRangeException(nameof(months));
        now = now.ToUniversalTime();
        var current = At(now);
        var anchor = current?.StartsAt ?? now;
        var totalMonths = checked((current?.CalendarMonths ?? 0) + months);
        return new(current?.Id, anchor, totalMonths, current?.EndsAt, anchor.AddMonths(totalMonths));
    }

    public NobilityCoverage Extend(int months, DateTimeOffset now)
    {
        var extension = Preview(months, now);
        var period = extension.CoverageId is { } id ? Coverage.Single(x => x.Id == id) : null;
        if (period is null)
        {
            period = new NobilityCoverage { AccountId = AccountId, StartsAt = extension.Anchor };
            Coverage.Add(period);
        }
        period.CalendarMonths = extension.TotalMonths;
        period.EndsAt = extension.ExpiresAt;
        Version = Guid.NewGuid();
        return period;
    }
}

public sealed record NobilityExtension(Guid? CoverageId, DateTimeOffset Anchor, int TotalMonths,
    DateTimeOffset? PreviousExpiry, DateTimeOffset ExpiresAt);
