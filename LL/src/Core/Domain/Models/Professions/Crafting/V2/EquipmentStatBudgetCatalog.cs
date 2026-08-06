using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public static class EquipmentStatBudgetCatalog
{
    public const int BalanceVersion = 11;
    public const int MinimumTier = 1;
    public const int MaximumTier = 10;

    private static readonly IReadOnlyDictionary<AttributeType, EquipmentStatBudgetDefinition> Definitions =
        new Dictionary<AttributeType, EquipmentStatBudgetDefinition>
        {
            [AttributeType.Power] =
                Tiered(1_800, (1, 24d), (5, 6.7d), (10, 3.5d)),
            [AttributeType.MaxHealth] = Fixed(0.2d, 25_000),
            [AttributeType.Armor] = Tiered(
                AttributeCombatRules.TypedMitigationCapPercent,
                (1, 0.68d), (5, 1.87d), (10, 4.12d)),
            [AttributeType.Resistance] = Tiered(
                AttributeCombatRules.TypedMitigationCapPercent,
                (1, 0.68d), (5, 1.87d), (10, 4.12d)),
            [AttributeType.CritChance] = Fixed(4d, 75),
            [AttributeType.CritDamage] =
                Tiered(1_000, (1, 2d), (5, 2.25d), (10, 2.5d)),
            [AttributeType.ArmorPenetration] = Fixed(
                3d,
                AttributeCombatRules.TypedPenetrationCapPercent),
            [AttributeType.MagicPenetration] = Fixed(
                3d,
                AttributeCombatRules.TypedPenetrationCapPercent),
            [AttributeType.DodgeChance] = Fixed(5d, 50),
            [AttributeType.BlockChance] = Fixed(5d, 50),
            [AttributeType.DamageReduction] =
                Fixed(6d, AttributeCatalog.GetFixedCap(AttributeType.DamageReduction)),
            [AttributeType.HealingPowerPercent] = Fixed(3d, 5_000),
            [AttributeType.HealthRegeneration] =
                Tiered(5_000, (1, 1.5d), (5, 1.5d), (10, 2.1d)),
            [AttributeType.LifeSteal] = Fixed(6d, 50),
            [AttributeType.Cooldown] =
                Fixed(6d, AttributeCatalog.GetFixedCap(AttributeType.Cooldown)),
            [AttributeType.StatusResistance] =
                Tiered(5_000, (1, 2d), (5, 0.4d), (10, 0.665d)),
            [AttributeType.CrowdControlResistance] = Fixed(2d, 5_000),
            [AttributeType.SummonPower] =
                Tiered(5_000, (1, 3d), (5, 1.25d), (10, 1d)),
            [AttributeType.SummonHealth] =
                Tiered(5_000, (1, 1.9d), (5, 0.75d), (10, 0.5d)),
            [AttributeType.AttackSpeed] = Fixed(2.8d, 200)
        };
    public static IReadOnlyCollection<AttributeType> Attributes => Definitions.Keys.ToArray();

    public static EquipmentStatBudgetRule Get(AttributeType stat, int tier)
    {
        if (!Definitions.TryGetValue(stat, out var definition))
            throw new InvalidOperationException($"No equipment budget rule exists for '{stat}'.");

        return new EquipmentStatBudgetRule(
            InterpolateCost(definition.CostAnchors, NormalizeTier(tier)),
            definition.PerItemHardCap);
    }

    public static IReadOnlyList<EquipmentStatCostAnchor> GetCostAnchors(AttributeType stat) =>
        Definitions.TryGetValue(stat, out var definition)
            ? definition.CostAnchors
            : throw new InvalidOperationException($"No equipment budget rule exists for '{stat}'.");

    public static bool IsKnown(AttributeType stat) => Definitions.ContainsKey(stat);

    private static int NormalizeTier(int tier) => Math.Clamp(tier, MinimumTier, MaximumTier);

    private static double InterpolateCost(
        IReadOnlyList<EquipmentStatCostAnchor> anchors,
        int tier)
    {
        var upperIndex = 0;
        while (upperIndex < anchors.Count && anchors[upperIndex].Tier < tier)
            upperIndex++;

        if (upperIndex == 0)
            return anchors[0].CostPerPoint;
        if (upperIndex >= anchors.Count)
            return anchors[^1].CostPerPoint;

        var lower = anchors[upperIndex - 1];
        var upper = anchors[upperIndex];
        if (lower.Tier == upper.Tier)
            return upper.CostPerPoint;

        var progress = (tier - lower.Tier) / (double)(upper.Tier - lower.Tier);
        return Math.Round(
            lower.CostPerPoint + ((upper.CostPerPoint - lower.CostPerPoint) * progress),
            4,
            MidpointRounding.AwayFromZero);
    }

    private static EquipmentStatBudgetDefinition Fixed(double costPerPoint, float hardCap) =>
        Tiered(hardCap, (MinimumTier, costPerPoint), (MaximumTier, costPerPoint));

    private static EquipmentStatBudgetDefinition Tiered(
        float hardCap,
        params (int Tier, double CostPerPoint)[] anchors) =>
        new(
            hardCap,
            Array.AsReadOnly(
                anchors
                    .OrderBy(anchor => anchor.Tier)
                    .Select(anchor => new EquipmentStatCostAnchor(anchor.Tier, anchor.CostPerPoint))
                    .ToArray()));
}

public sealed record EquipmentStatBudgetDefinition(
    float PerItemHardCap,
    IReadOnlyList<EquipmentStatCostAnchor> CostAnchors);

public sealed record EquipmentStatCostAnchor(int Tier, double CostPerPoint);

public sealed record EquipmentStatBudgetRule(double CostPerPoint, float PerItemHardCap)
{
    public float HardCap => PerItemHardCap;
}
