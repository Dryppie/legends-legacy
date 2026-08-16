using Domain.Models.Attributes;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting.V2;

public static class EquipmentConstraintProfile
{
    public const int BalanceVersion = EquipmentStatBudgetCatalog.BalanceVersion;
    public const bool ProductionActive = true;
    public const double AggregateWasteTolerancePercent = 1d;
    public const double MinimumSupportedBasicAttackIntervalMultiplier = 0.75d;
    public const double BlueprintBonusCapMultiplier = 1.25d;
    public const double RarityImprovementCapMultiplier = 1.25d;
    public static double GetCostPerPoint(AttributeType attribute, int tier) =>
        EquipmentStatBudgetCatalog.Get(attribute).CostPerPoint;

    public static IReadOnlyDictionary<AttributeType, float> CreateTierBaseline(int tier)
    {
        var normalizedTier = Math.Max(EquipmentStatBudgetCatalog.MinimumTier, tier);
        var attributes = new Dictionary<AttributeType, float>
        {
            [AttributeType.Power] = 8f * normalizedTier,
            [AttributeType.MaxHealth] = 180 + normalizedTier * 112,
            [AttributeType.Armor] = 0,
            [AttributeType.Resistance] = 0,
            [AttributeType.CritChance] = 5,
            [AttributeType.CritDamage] = 50
        };
        return attributes;
    }

    public static IReadOnlyList<EquipmentLinearBudgetConstraint> CreateItemConstraints(
        IReadOnlyDictionary<AttributeType, float> baselineAttributes,
        int tier,
        double slotBudgetWeight,
        double expectedLoadoutBudgetWeight,
        double basicAttackIntervalMultiplier)
    {
        // V16 stores unbounded raw ratings. Effective combat caps are applied
        // after loadout aggregation, so direct point constraints would make the
        // same recipe spend a different budget share at different tiers.
        return [];
    }

    public static float GetCraftedMitigationCapPercent(int tier)
    {
        if (tier < EquipmentStatBudgetCatalog.MinimumTier)
            throw new ArgumentOutOfRangeException(nameof(tier));

        return AttributeCombatRules.TypedMitigationCapPercent;
    }

    public static IReadOnlyDictionary<AttributeType, double> GetOverflowWeights(
        EquipmentCraftingDesign design)
    {
        var role = design.Recipe.Behavior.Role;
        var tags = design.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var magical = tags.Contains("Magic")
            || tags.Contains("Support")
            || tags.Contains("Summon");

        if (tags.Contains("Fury"))
        {
            return EqualWeights(
                AttributeType.Power,
                AttributeType.CritChance,
                AttributeType.CritDamage,
                AttributeType.AttackSpeed);
        }

        if (tags.Contains("Execution"))
        {
            return EqualWeights(
                AttributeType.Power,
                AttributeType.ArmorPenetration,
                AttributeType.CritDamage,
                AttributeType.Cooldown);
        }

        if (tags.Contains("Aegis"))
        {
            return EqualWeights(
                AttributeType.MaxHealth,
                AttributeType.Armor,
                AttributeType.Resistance,
                AttributeType.BlockChance);
        }

        return design.Recipe.OutputItemType switch
        {
            EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs
                when role.Equals("Heavy", StringComparison.OrdinalIgnoreCase) =>
                EqualWeights(
                    AttributeType.MaxHealth,
                    AttributeType.Armor,
                    AttributeType.Resistance,
                    AttributeType.BlockChance),
            EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs
                when role.Equals("Cloth", StringComparison.OrdinalIgnoreCase) =>
                EqualWeights(
                    AttributeType.Power,
                    AttributeType.Resistance,
                    AttributeType.HealingPowerPercent,
                    AttributeType.HealthRegeneration),
            EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs =>
                EqualWeights(
                    AttributeType.Power,
                    AttributeType.MaxHealth,
                    AttributeType.Armor,
                    AttributeType.Resistance,
                    AttributeType.StatusResistance),
            EquipmentType.OneHanded or EquipmentType.TwoHanded when magical =>
                EqualWeights(
                    AttributeType.Power,
                    AttributeType.MagicPenetration,
                    AttributeType.HealthRegeneration,
                    AttributeType.HealingPowerPercent),
            EquipmentType.OneHanded or EquipmentType.TwoHanded =>
                EqualWeights(AttributeType.Power),
            EquipmentType.OffHand
                when role.Equals("Towershield", StringComparison.OrdinalIgnoreCase) =>
                EqualWeights(
                    AttributeType.MaxHealth,
                    AttributeType.Armor,
                    AttributeType.Resistance,
                    AttributeType.BlockChance),
            EquipmentType.OffHand =>
                EqualWeights(
                    AttributeType.Power,
                    AttributeType.Resistance,
                    AttributeType.HealthRegeneration,
                    AttributeType.MagicPenetration),
            EquipmentType.Ring or EquipmentType.Necklace or EquipmentType.Relic =>
                EqualWeights(
                    AttributeType.Power,
                    AttributeType.MaxHealth,
                    AttributeType.HealthRegeneration,
                    AttributeType.StatusResistance,
                    AttributeType.CrowdControlResistance),
            _ => new Dictionary<AttributeType, double>()
        };
    }

    public static IReadOnlyDictionary<AttributeType, double> GetRarityOverflowWeights(
        EquipmentType equipmentType,
        TemperingProfileDefinition profile)
    {
        var authoredStats = profile.Stats
            .Select(stat => stat.Stat)
            .ToHashSet();
        var magical = authoredStats.Contains(AttributeType.MagicPenetration)
            || authoredStats.Contains(AttributeType.HealingPowerPercent);

        return equipmentType switch
        {
            EquipmentType.OneHanded or EquipmentType.TwoHanded when magical =>
                EqualWeights(
                    AttributeType.Power,
                    AttributeType.MagicPenetration,
                    AttributeType.HealthRegeneration,
                    AttributeType.HealingPowerPercent),
            EquipmentType.OneHanded or EquipmentType.TwoHanded =>
                EqualWeights(AttributeType.Power),
            EquipmentType.OffHand =>
                EqualWeights(
                    AttributeType.Power,
                    AttributeType.MaxHealth,
                    AttributeType.Armor,
                    AttributeType.Resistance,
                    AttributeType.HealthRegeneration,
                    AttributeType.MagicPenetration,
                    AttributeType.BlockChance),
            EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs =>
                EqualWeights(
                    AttributeType.Power,
                    AttributeType.MaxHealth,
                    AttributeType.Armor,
                    AttributeType.Resistance,
                    AttributeType.HealthRegeneration,
                    AttributeType.StatusResistance),
            EquipmentType.Ring or EquipmentType.Necklace or EquipmentType.Relic =>
                EqualWeights(
                    AttributeType.Power,
                    AttributeType.MaxHealth,
                    AttributeType.Armor,
                    AttributeType.Resistance,
                    AttributeType.HealthRegeneration,
                    AttributeType.StatusResistance,
                    AttributeType.CrowdControlResistance,
                    AttributeType.CritChance,
                    AttributeType.AttackSpeed),
            _ => new Dictionary<AttributeType, double>()
        };
    }

    public static double GetPerItemCapMultiplier(double slotBudgetWeight) =>
        Math.Max(1d, slotBudgetWeight);

    public static double GetMaximumAdditionalPoints(
        AttributeType attribute,
        int tier,
        IReadOnlyDictionary<AttributeType, double> currentPoints,
        IReadOnlyList<EquipmentLinearBudgetConstraint> constraints,
        double perItemCapMultiplier)
    {
        var maximum = Math.Max(
            0d,
            EquipmentStatBudgetCatalog.Get(attribute).PerItemHardCap
            * Math.Max(1d, perItemCapMultiplier)
            - currentPoints.GetValueOrDefault(attribute));

        foreach (var constraint in constraints)
        {
            var contributionPerPoint = attribute == constraint.EffectiveAttribute ? 1d : 0d;
            if (contributionPerPoint <= 0)
                continue;

            var currentContribution = currentPoints.Sum(entry =>
                entry.Value
                * (entry.Key == constraint.EffectiveAttribute ? 1d : 0d));
            maximum = Math.Min(
                maximum,
                Math.Max(0d, constraint.MaximumAddedValue - currentContribution)
                / contributionPerPoint);
        }

        return maximum;
    }

    private static IReadOnlyDictionary<AttributeType, double> EqualWeights(
        params AttributeType[] attributes) =>
        attributes.Distinct().ToDictionary(attribute => attribute, _ => 1d);
}

public sealed record EquipmentLinearBudgetConstraint(
    AttributeType EffectiveAttribute,
    double MaximumAddedValue);
