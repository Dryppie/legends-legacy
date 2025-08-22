namespace Domain.Models.Dungeons;
public sealed record EntryLimit(EntryLimitType Type, int? Count, DateTimeOffset? RefreshAtUtc)
{
    public static EntryLimit Unlimited() => new(EntryLimitType.Unlimited, null, null);
}
