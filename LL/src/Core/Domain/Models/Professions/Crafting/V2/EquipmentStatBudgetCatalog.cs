using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public static class EquipmentStatBudgetCatalog
{
    public const int BalanceVersion = 16;
    public const int LegacyBalanceVersion = 15;
    public const int MinimumTier = 1;

    // Source-compatible only. V16 never uses this value to clamp progression.
    public const int MaximumTier = int.MaxValue;

    private static readonly IReadOnlyDictionary<AttributeType, EquipmentStatBudgetDefinition> Definitions =
        new Dictionary<AttributeType, EquipmentStatBudgetDefinition>
        {
            [AttributeType.Power] = Flat(24d),
            [AttributeType.MaxHealth] = Flat(0.2d),
            [AttributeType.Armor] = Rating(0.68d, AttributeCombatRules.TypedMitigationCapPercent, 80d),
            [AttributeType.Resistance] = Rating(0.68d, AttributeCombatRules.TypedMitigationCapPercent, 80d),
            [AttributeType.CritChance] = Rating(4d, AttributeCombatRules.CritChanceCapPercent, 100d),
            [AttributeType.CritDamage] = Rating(2d, 300f, 300d),
            [AttributeType.ArmorPenetration] = Rating(3d, AttributeCombatRules.TypedPenetrationCapPercent, 60d),
            [AttributeType.MagicPenetration] = Rating(3d, AttributeCombatRules.TypedPenetrationCapPercent, 60d),
            [AttributeType.DodgeChance] = Rating(5d, AttributeCombatRules.DodgeChanceCapPercent, 50d),
            [AttributeType.BlockChance] = Rating(5d, AttributeCombatRules.BlockChanceCapPercent, 50d),
            [AttributeType.DamageReduction] = Rating(6d, AttributeCombatRules.DamageReductionCapPercent, 40d),
            [AttributeType.HealingPowerPercent] = Rating(3d, 300f, 300d),
            [AttributeType.HealthRegeneration] = Flat(1.5d),
            [AttributeType.LifeSteal] = Rating(6d, 50f, 50d),
            [AttributeType.Cooldown] = Rating(
                6d,
                AttributeCombatRules.CooldownReductionCapPercent,
                AttributeCombatRules.CooldownRateConstant),
            // Short authored status windows quantize to whole combat ticks. A lower half-cap
            // keeps a 10% marginal equipment investment measurable without weakening the cap.
            [AttributeType.StatusResistance] = Rating(2d, 80f, 20d),
            [AttributeType.CrowdControlResistance] = Rating(2d, AttributeCombatRules.CrowdControlResistanceCapPercent, 20d),
            [AttributeType.AttackSpeed] = Rating(2.8d, 300f, 300d)
        };

    public static IReadOnlyCollection<AttributeType> Attributes => Definitions.Keys.ToArray();

    public static EquipmentStatBudgetRule Get(AttributeType stat)
    {
        if (!Definitions.TryGetValue(stat, out var definition))
            throw new InvalidOperationException($"No equipment budget rule exists for '{stat}'.");

        return new EquipmentStatBudgetRule(
            definition.CostPerPoint,
            float.MaxValue,
            definition.ScalingKind,
            definition.EffectiveCap,
            definition.HalfCapNormalizedRating);
    }

    /// <summary>Compatibility overload. Tier is intentionally ignored in v16.</summary>
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

    public static float ConvertRatingToEffectiveValue(
        AttributeType stat,
        double rawRating,
        int progressionTier)
    {
        var rule = Get(stat);
        if (rule.ScalingKind != EquipmentStatScalingKind.ProgressionNormalizedRating)
            return (float)Math.Max(0d, rawRating);

        var normalizedRating = Math.Max(0d, rawRating)
            / EquipmentTierBudgetCurve.GetScale(Math.Max(MinimumTier, progressionTier));
        if (normalizedRating <= 0d)
            return 0f;
        if (stat == AttributeType.Cooldown)
            return AttributeCombatRules.CalculateCooldownReductionPercent(normalizedRating);

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
            return Math.Max(0d, effectiveValue);

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
            return (float)Math.Max(0d, normalizedRating);
        if (normalizedRating <= 0d)
            return 0f;
        if (!double.IsFinite(normalizedRating))
            return rule.EffectiveCap;
        if (stat == AttributeType.Cooldown)
            return AttributeCombatRules.CalculateCooldownReductionPercent(normalizedRating);

        var effective = rule.EffectiveCap
            * normalizedRating
            / (rule.HalfCapNormalizedRating + normalizedRating);
        return (float)Math.Clamp(effective, 0d, rule.EffectiveCap);
    }

    public static float ConvertCooldownRatingToEffectiveReduction(
        double rawRating,
        int progressionTier)
    {
        var normalizedRating = Math.Max(0d, rawRating)
            / EquipmentTierBudgetCurve.GetScale(Math.Max(MinimumTier, progressionTier));
        return AttributeCombatRules.CalculateCooldownReductionPercent(normalizedRating);
    }

    private static EquipmentStatBudgetDefinition Flat(double costPerPoint) =>
        new(costPerPoint, EquipmentStatScalingKind.Flat, 0f, 0d);

    private static EquipmentStatBudgetDefinition Rating(
        double costPerPoint,
        float effectiveCap,
        double halfCapNormalizedRating) =>
        new(
            costPerPoint,
            EquipmentStatScalingKind.ProgressionNormalizedRating,
            effectiveCap,
            halfCapNormalizedRating);
}

public enum EquipmentStatScalingKind
{
    Flat = 0,
    ProgressionNormalizedRating = 1
}

public sealed record EquipmentStatBudgetDefinition(
    double CostPerPoint,
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
