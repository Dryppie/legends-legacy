using Domain.Models.Items;

namespace Domain.Models.Professions.Crafting.V2;

public static class TemperingConstants
{
    public const int PotentialCost = 1;
    public const int ActionDurationSeconds = 10;

    public static int GetRarityUpgradeCount(Rarity rarity) =>
        Math.Max(0, (int)rarity);

    public static double GetDirectedImprovementBudget(int tier) =>
        Math.Max(1d, Math.Clamp(tier, 1, 10) * 2d);
}
