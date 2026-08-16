using Domain.Models.Professions.Crafting.V2;

namespace Domain.Models.Attributes;

public static class AttributeCombatRules
{
    public const float CooldownReductionCapPercent = 100f;
    public const double CooldownRateConstant = 160d;
    public const float DamageReductionCapPercent = 40f;
    public const float DodgeChanceCapPercent = 50f;
    public const float BlockChanceCapPercent = 50f;
    public const float BlockDamageReductionPercent = 50f;
    public const float CritChanceCapPercent = 100f;
    public const float TypedMitigationCapPercent = 80f;
    public const float TypedPenetrationCapPercent = 60f;
    public const float CrowdControlResistanceCapPercent = 80f;
    public const float MinimumBasicAttackRate = 0.25f;
    public const float MaximumBasicAttackRate = 4f;
    public const float BasicAttackPowerCoefficient = 0.5f;

    public static float CalculateDefenseMitigation(float defense, float penetration = 0)
    {
        var defenseRating = EquipmentStatBudgetCatalog.ConvertEffectiveValueToNormalizedRating(
            AttributeType.Armor,
            defense);
        var penetrationRating = EquipmentStatBudgetCatalog.ConvertEffectiveValueToNormalizedRating(
            AttributeType.ArmorPenetration,
            penetration);
        var netDefenseRating = defenseRating == double.MaxValue
            ? double.MaxValue
            : Math.Max(0d, defenseRating - penetrationRating);
        var effectiveDefensePercent = EquipmentStatBudgetCatalog
            .ConvertNormalizedRatingToEffectiveValue(AttributeType.Armor, netDefenseRating);
        return effectiveDefensePercent / 100f;
    }

    public static float CalculateEffectiveHealth(float maxHealth, float defense, float penetration = 0)
    {
        var mitigation = CalculateDefenseMitigation(defense, penetration);
        return mitigation >= 1
            ? float.PositiveInfinity
            : Math.Max(0, maxHealth) / (1 - mitigation);
    }

    public static int CalculateCooldownTicks(int authoredTicks, float cooldownReductionPercent)
    {
        if (authoredTicks <= 0)
            return 0;

        var reductionPercent = Math.Clamp(cooldownReductionPercent, 0, 100);
        var reducedTicks = authoredTicks * (100d - reductionPercent) / 100d;
        return Math.Max(1, (int)Math.Ceiling(reducedTicks - 1e-9d));
    }

    public static double CalculateCooldownRate(double normalizedCooldownRating) =>
        1d + Math.Max(0d, normalizedCooldownRating) / CooldownRateConstant;

    public static float CalculateCooldownReductionPercent(double normalizedCooldownRating)
    {
        var rate = CalculateCooldownRate(normalizedCooldownRating);
        return (float)(100d * (1d - 1d / rate));
    }

    public static int CalculateCrowdControlDurationTicks(
        int authoredTicks,
        float resistancePercent)
    {
        if (authoredTicks <= 0)
            return authoredTicks;

        var reductionPercent = Math.Clamp(
            resistancePercent,
            0,
            CrowdControlResistanceCapPercent);
        var reducedTicks = authoredTicks * (100d - reductionPercent) / 100d;
        return Math.Max(1, (int)Math.Ceiling(reducedTicks - 1e-9d));
    }

    public static float CalculateBasicAttackRate(
        float attackSpeedPercent,
        double basicAttackIntervalMultiplier)
    {
        var intervalMultiplier = Math.Max(0.01d, basicAttackIntervalMultiplier);
        var rate = (1d + attackSpeedPercent / 100d) / intervalMultiplier;
        return (float)Math.Clamp(rate, MinimumBasicAttackRate, MaximumBasicAttackRate);
    }

    public static float CalculateUsefulAttackSpeedCapPercent(double basicAttackIntervalMultiplier)
    {
        var intervalMultiplier = Math.Max(0.01d, basicAttackIntervalMultiplier);
        return (float)Math.Max(0d, (MaximumBasicAttackRate * intervalMultiplier - 1d) * 100d);
    }

    public static bool TryGetEffectiveCharacterCap(
        AttributeType attributeType,
        double basicAttackIntervalMultiplier,
        out float cap) =>
        AttributeCatalog.TryGetEffectiveCharacterCap(
            attributeType,
            basicAttackIntervalMultiplier,
            out cap);

}
