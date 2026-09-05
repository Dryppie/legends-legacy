using Domain.Models.Attributes;

namespace Domain.Models.Items.Equipments.Progression;

public static class EquipmentStatBudgetCatalog
{
    public const int BalanceVersion = 17;
    public const int PreviousBalanceVersion = 16;
    public const int LegacyBalanceVersion = 15;
    public const int MinimumTier = 1;

    // Source-compatible only. V16 never uses this value to clamp progression.
    public const int MaximumTier = int.MaxValue;

    private static readonly IReadOnlyDictionary<AttributeType, EquipmentStatBudgetDefinition> Definitions =
        new Dictionary<AttributeType, EquipmentStatBudgetDefinition>
        {
            [AttributeType.Power] = Flat(22.5d),
            [AttributeType.MaxHealth] = Flat(0.185d),
            [AttributeType.Armor] = Rating(0.9d, AttributeCombatRules.TypedMitigationCapPercent, 55d),
            [AttributeType.Resistance] = Rating(0.75d, AttributeCombatRules.TypedMitigationCapPercent, 80d),
            [AttributeType.CritChance] = Percentage(6d, AttributeCombatRules.CritChanceCapPercent),
            [AttributeType.CritDamage] = Percentage(2.2d, AttributeCombatRules.CritDamageBonusCapPercent),
            [AttributeType.ArmorPenetration] = Percentage(3.5d, AttributeCombatRules.TypedPenetrationCapPercent),
            [AttributeType.MagicPenetration] = Percentage(4d, AttributeCombatRules.TypedPenetrationCapPercent),
            [AttributeType.DodgeChance] = Percentage(30d, AttributeCombatRules.DodgeChanceCapPercent),
            [AttributeType.BlockChance] = Percentage(5.7d, AttributeCombatRules.BlockChanceCapPercent),
            [AttributeType.DamageReduction] = Percentage(6d, AttributeCombatRules.DamageReductionCapPercent),
            [AttributeType.HealingPowerPercent] = Percentage(4.5d, AttributeCombatRules.HealingPowerCapPercent),
            [AttributeType.HealthRegeneration] = Flat(3d),
            [AttributeType.LifeSteal] = Percentage(8d, AttributeCombatRules.LifeStealCapPercent),
            [AttributeType.Cooldown] = Percentage(7.5d, AttributeCombatRules.CooldownReductionCapPercent),
            [AttributeType.StatusResistance] = Percentage(0.82d, AttributeCombatRules.StatusResistanceCapPercent),
            [AttributeType.CrowdControlResistance] = Percentage(0.82d, AttributeCombatRules.CrowdControlResistanceCapPercent),
            [AttributeType.AttackSpeed] = Percentage(1d, AttributeCombatRules.AttackSpeedCapPercent)
        };

    private static readonly IReadOnlyDictionary<AttributeType, EquipmentStatBudgetRule> Rules =
        Definitions.ToDictionary(
            pair => pair.Key,
            pair => new EquipmentStatBudgetRule(
                pair.Value.CostPerPoint,
                pair.Value.PerItemHardCap,
                pair.Value.ScalingKind,
                pair.Value.EffectiveCap,
                pair.Value.HalfCapNormalizedRating));

    public static IReadOnlyCollection<AttributeType> Attributes => Definitions.Keys.ToArray();

    public static EquipmentStatBudgetRule Get(AttributeType stat)
    {
        if (!Rules.TryGetValue(stat, out var rule))
            throw new InvalidOperationException($"No equipment budget rule exists for '{stat}'.");

        return rule;
    }

    /// <summary>The normalized v17 exchange rate is independent of tier.</summary>
    public static EquipmentStatBudgetRule Get(AttributeType stat, int tier)
    {
        if (tier < MinimumTier)
            throw new ArgumentOutOfRangeException(nameof(tier), tier, "Equipment tier must be positive.");

        return Get(stat);
    }

    public static IReadOnlyList<EquipmentStatCostAnchor> GetCostAnchors(AttributeType stat)
    {
        var rule = Get(stat);
        return [new EquipmentStatCostAnchor(MinimumTier, rule.CostPerPoint)];
    }

    public static bool IsKnown(AttributeType stat) => Definitions.ContainsKey(stat);

    public static bool IsRating(AttributeType stat) =>
        Definitions.TryGetValue(stat, out var definition)
        && definition.ScalingKind == EquipmentStatScalingKind.ProgressionNormalizedRating;

    public static bool IsDirectPercentage(AttributeType stat) =>
        Definitions.TryGetValue(stat, out var definition)
        && definition.ScalingKind == EquipmentStatScalingKind.DirectPercentage;

    public static bool IsTierAnchor(AttributeType stat) =>
        Definitions.TryGetValue(stat, out var definition)
        && definition.ScalingKind is EquipmentStatScalingKind.Flat
            or EquipmentStatScalingKind.ProgressionNormalizedRating;

    /// <summary>
    /// Cost expressed in the tier-budget currency consumed by the allocator.
    /// Direct percentages buy normalized value, while flats and opposed ratings
    /// materialize with the tier scale.
    /// </summary>
    public static double GetMaterializedCostPerPoint(AttributeType stat, int tier)
    {
        if (tier < MinimumTier)
            throw new ArgumentOutOfRangeException(nameof(tier), tier, "Equipment tier must be positive.");

        var rule = Get(stat);
        return rule.ScalingKind == EquipmentStatScalingKind.DirectPercentage
            ? rule.CostPerPoint * EquipmentTierBudgetCurve.GetScale(tier)
            : rule.CostPerPoint;
    }

    public static float ConvertRatingToEffectiveValue(
        AttributeType stat,
        double rawRating,
        int progressionTier)
    {
        var rule = Get(stat);
        if (rule.ScalingKind != EquipmentStatScalingKind.ProgressionNormalizedRating)
            throw new InvalidOperationException($"Attribute '{stat}' is not an equipment rating in v17.");

        var normalizedRating = Math.Max(0d, rawRating)
            / EquipmentTierBudgetCurve.GetScale(Math.Max(MinimumTier, progressionTier));
        if (normalizedRating <= 0d)
            return 0f;
        var effective = rule.EffectiveCap
            * normalizedRating
            / (rule.HalfCapNormalizedRating + normalizedRating);
        return (float)Math.Clamp(effective, 0d, rule.EffectiveCap);
    }

    public static double ConvertEffectiveValueToNormalizedRating(
        AttributeType stat,
        double effectiveValue)
    {
        var rule = Get(stat);
        if (rule.ScalingKind != EquipmentStatScalingKind.ProgressionNormalizedRating)
            throw new InvalidOperationException($"Attribute '{stat}' is not an equipment rating in v17.");

        var effective = Math.Clamp(effectiveValue, 0d, rule.EffectiveCap);
        if (effective <= 0d)
            return 0d;
        if (effective >= rule.EffectiveCap)
            return double.MaxValue;

        return rule.HalfCapNormalizedRating
            * effective
            / (rule.EffectiveCap - effective);
    }

    public static float ConvertNormalizedRatingToEffectiveValue(
        AttributeType stat,
        double normalizedRating)
    {
        var rule = Get(stat);
        if (rule.ScalingKind != EquipmentStatScalingKind.ProgressionNormalizedRating)
            throw new InvalidOperationException($"Attribute '{stat}' is not an equipment rating in v17.");
        if (normalizedRating <= 0d)
            return 0f;
        if (!double.IsFinite(normalizedRating))
            return rule.EffectiveCap;
        var effective = rule.EffectiveCap
            * normalizedRating
            / (rule.HalfCapNormalizedRating + normalizedRating);
        return (float)Math.Clamp(effective, 0d, rule.EffectiveCap);
    }

    private static EquipmentStatBudgetDefinition Flat(double costPerPoint) =>
        new(costPerPoint, float.MaxValue, EquipmentStatScalingKind.Flat, 0f, 0d);

    private static EquipmentStatBudgetDefinition Rating(
        double costPerPoint,
        float effectiveCap,
        double halfCapNormalizedRating) =>
        new(
            costPerPoint,
            float.MaxValue,
            EquipmentStatScalingKind.ProgressionNormalizedRating,
            effectiveCap,
            halfCapNormalizedRating);

    private static EquipmentStatBudgetDefinition Percentage(
        double costPerPercentagePoint,
        float perItemHardCap) =>
        new(
            costPerPercentagePoint,
            perItemHardCap,
            EquipmentStatScalingKind.DirectPercentage,
            perItemHardCap,
            0d);
}

public enum EquipmentStatScalingKind
{
    Flat = 0,
    ProgressionNormalizedRating = 1,
    DirectPercentage = 2
}

public sealed record EquipmentStatBudgetDefinition(
    double CostPerPoint,
    float PerItemHardCap,
    EquipmentStatScalingKind ScalingKind,
    float EffectiveCap,
    double HalfCapNormalizedRating);

public sealed record EquipmentStatCostAnchor(int Tier, double CostPerPoint);

public sealed record EquipmentStatBudgetRule(
    double CostPerPoint,
    float PerItemHardCap,
    EquipmentStatScalingKind ScalingKind,
    float EffectiveCap,
    double HalfCapNormalizedRating)
{
    public float HardCap => PerItemHardCap;
}
