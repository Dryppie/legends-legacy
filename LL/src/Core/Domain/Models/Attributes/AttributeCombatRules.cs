namespace Domain.Models.Attributes;

public static class AttributeCombatRules
{
    public const float CooldownReductionCapPercent = 40f;
    public const float DamageReductionCapPercent = 40f;
    public const float DodgeChanceCapPercent = 50f;
    public const float BlockChanceCapPercent = 50f;
    public const float BlockDamageReductionPercent = 50f;
    public const float CritChanceCapPercent = 100f;
    public const float TypedMitigationCapPercent = 80f;
    public const float TypedPenetrationCapPercent = 60f;
    public const float MinimumBasicAttackRate = 0.25f;
    public const float MaximumBasicAttackRate = 4f;
    public const float BasicAttackPowerCoefficient = 0.5f;

    public static float CalculateDefenseMitigation(float defense, float penetration = 0)
    {
        var effectiveDefensePercent = Math.Clamp(
            defense - Math.Clamp(penetration, 0, TypedPenetrationCapPercent),
            0,
            TypedMitigationCapPercent);
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

        var reductionPercent = Math.Clamp(
            cooldownReductionPercent,
            0,
            AttributeCatalog.GetFixedCap(AttributeType.Cooldown));
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
