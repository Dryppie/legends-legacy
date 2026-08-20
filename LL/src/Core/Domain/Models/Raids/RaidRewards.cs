namespace Domain.Models.Raids;

public sealed record RaidRewardPackage(
    int Trophies,
    IReadOnlyList<RaidPendingItem> Items);

public sealed record RaidRewardGrant(
    RaidRewardKind Kind,
    RaidRewardPackage Package);

public static class RaidRewardCalculator
{
    private const decimal RepeatRewardMultiplier = 0.25m;

    public static RaidRewardPackage FullPackage(
        RaidRewardDefinition rewards,
        RaidOutcome outcome)
    {
        var trophies = outcome switch
        {
            RaidOutcome.Slain => rewards.SlainTrophies,
            RaidOutcome.Broken => rewards.BrokenTrophies,
            RaidOutcome.Wounded => rewards.WoundedTrophies,
            _ => rewards.RepelledTrophies
        };
        var outcomeMultiplier = outcome switch
        {
            RaidOutcome.Slain => 1m,
            RaidOutcome.Broken => 0.65m,
            RaidOutcome.Wounded => 0.40m,
            _ => 0.20m
        };

        return new RaidRewardPackage(
            trophies,
            rewards.GuaranteedItems
                .Select(item => new RaidPendingItem(
                    item.ItemId,
                    ScaleQuantity(item.Quantity, outcomeMultiplier)))
                .ToArray());
    }

    public static RaidRewardGrant CalculateGrant(
        RaidRewardPackage fullPackage,
        IReadOnlyCollection<RaidRewardPackage> previousWeeklyEntitlements)
    {
        if (previousWeeklyEntitlements.Count == 0)
            return new RaidRewardGrant(RaidRewardKind.WeeklyBase, fullPackage);

        var previousTrophies = previousWeeklyEntitlements.Sum(x => x.Trophies);
        var previousItems = previousWeeklyEntitlements
            .SelectMany(x => x.Items)
            .GroupBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Sum(item => item.Quantity), StringComparer.OrdinalIgnoreCase);
        var upgrade = new RaidRewardPackage(
            Math.Max(0, fullPackage.Trophies - previousTrophies),
            fullPackage.Items
                .Select(item => new RaidPendingItem(
                    item.ItemId,
                    Math.Max(0, item.Quantity - previousItems.GetValueOrDefault(item.ItemId))))
                .Where(item => item.Quantity > 0)
                .ToArray());

        if (upgrade.Trophies > 0 || upgrade.Items.Count > 0)
            return new RaidRewardGrant(RaidRewardKind.WeeklyUpgrade, upgrade);

        return new RaidRewardGrant(
            RaidRewardKind.Repeat,
            new RaidRewardPackage(
                ScaleQuantity(fullPackage.Trophies, RepeatRewardMultiplier),
                fullPackage.Items
                    .Select(item => new RaidPendingItem(
                        item.ItemId,
                        ScaleQuantity(item.Quantity, RepeatRewardMultiplier)))
                    .ToArray()));
    }

    private static int ScaleQuantity(int quantity, decimal multiplier) =>
        Math.Max(1, (int)Math.Round(quantity * multiplier, MidpointRounding.AwayFromZero));
}
