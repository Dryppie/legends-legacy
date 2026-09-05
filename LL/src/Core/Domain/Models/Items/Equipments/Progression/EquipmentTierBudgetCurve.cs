namespace Domain.Models.Items.Equipments.Progression;

/// <summary>
/// The one shared, open-ended equipment progression curve. Recipe weights and
/// attribute exchange rates never vary by tier; only this total budget does.
/// </summary>
public static class EquipmentTierBudgetCurve
{
    public const double BaseBudget = 100d;
    public const double TierTenBudget = 1_520d;
    public const int ReferenceEndTier = 10;
    public const int MaximumSupportedTier = 100;
    public const int CharacterLevelsPerEquipmentTier = 50;

    public static readonly double GrowthPerTier = Math.Pow(
        TierTenBudget / BaseBudget,
        1d / (ReferenceEndTier - 1));

    public static double GetBudget(int tier)
    {
        if (tier < 1)
            throw new ArgumentOutOfRangeException(nameof(tier), tier, "Equipment tier must be positive.");

        var budget = BaseBudget * Math.Pow(GrowthPerTier, tier - 1d);
        if (!double.IsFinite(budget) || budget <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tier),
                tier,
                "Equipment tier is too large to represent with the active budget curve.");
        }

        return budget;
    }

    public static double GetScale(int tier) => GetBudget(tier) / BaseBudget;

    public static int GetExpectedTierForCharacterLevel(int characterLevel) =>
        ((Math.Max(1, characterLevel) - 1) / CharacterLevelsPerEquipmentTier) + 1;

    public static int GetRequiredCharacterLevelForTier(int tier)
    {
        if (tier < 1)
            throw new ArgumentOutOfRangeException(nameof(tier), tier, "Equipment tier must be positive.");

        return tier == 1
            ? 1
            : checked((tier - 1) * CharacterLevelsPerEquipmentTier);
    }

    public static int GetFirstCharacterLevelForTier(int tier)
    {
        if (tier < 1)
            throw new ArgumentOutOfRangeException(nameof(tier), tier, "Equipment tier must be positive.");

        return checked(((tier - 1) * CharacterLevelsPerEquipmentTier) + 1);
    }
}
