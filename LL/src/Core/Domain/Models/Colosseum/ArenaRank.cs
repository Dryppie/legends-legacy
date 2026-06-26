namespace Domain.Models.Colosseum;

public sealed record ArenaRankTier(
    string Id,
    string Name,
    int MinRating,
    int? MaxRating,
    int SortOrder);

public sealed record ArenaRankProgress(
    string CurrentTierId,
    string CurrentTierName,
    int Rating,
    int CurrentTierMinRating,
    int? NextTierMinRating,
    string? NextTierName,
    int? RatingUntilNextTier,
    decimal ProgressPercent);

public static class ArenaRank
{
    public static readonly IReadOnlyList<ArenaRankTier> Tiers =
    [
        new("bronze", "Bronze", 0, 1099, 1),
        new("silver", "Silver", 1100, 1249, 2),
        new("gold", "Gold", 1250, 1449, 3),
        new("platinum", "Platinum", 1450, 1699, 4),
        new("diamond", "Diamond", 1700, 1999, 5),
        new("champion", "Champion", 2000, 2299, 6),
        new("ascendant", "Ascendant", 2300, null, 7)
    ];

    public static ArenaRankTier GetTier(int rating)
    {
        return Tiers.Last(tier => rating >= tier.MinRating);
    }

    public static ArenaRankProgress GetProgress(int rating)
    {
        var current = GetTier(rating);
        var next = Tiers.FirstOrDefault(tier => tier.MinRating > rating);

        if (next is null)
        {
            return new ArenaRankProgress(
                current.Id,
                current.Name,
                rating,
                current.MinRating,
                null,
                null,
                null,
                100m);
        }

        var span = next.MinRating - current.MinRating;
        var gained = Math.Clamp(rating - current.MinRating, 0, span);
        var progress = span == 0 ? 100m : Math.Round(gained * 100m / span, 2);

        return new ArenaRankProgress(
            current.Id,
            current.Name,
            rating,
            current.MinRating,
            next.MinRating,
            next.Name,
            Math.Max(0, next.MinRating - rating),
            progress);
    }
}
