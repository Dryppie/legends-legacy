namespace Domain.Models.Attributes;

public sealed record PrimaryAttributeContribution(
    AttributeType PrimaryAttribute,
    AttributeType DerivedAttribute,
    float ContributionPerPoint);

public static class AttributeCombatRules
{
    public const float CooldownReductionCapPercent = 40f;
    public const float DamageReductionCapPercent = 40f;
    public const float DodgeChanceCapPercent = 50f;
    public const float BlockChanceCapPercent = 50f;
    public const float BlockDamageReductionPercent = 50f;
    public const float CritChanceCapPercent = 75f;
    public const float LifeStealCapPercent = 50f;
    public const float DefenseRatingScale = 100f;
    public const float BasicAttackPowerCoefficient = 0.1f;

    private static readonly IReadOnlyList<PrimaryAttributeContribution> Contributions =
    [
        new(AttributeType.Fortitude, AttributeType.MaxHealth, 4f),
        new(AttributeType.Fortitude, AttributeType.Armor, 0.5f),
        new(AttributeType.Fortitude, AttributeType.Resistance, 0.5f),

        new(AttributeType.Precision, AttributeType.CritChance, 0.1f),
        new(AttributeType.Precision, AttributeType.ArmorPenetration, 0.1f),
        new(AttributeType.Precision, AttributeType.MagicPenetration, 0.1f),
        new(AttributeType.Precision, AttributeType.AttackSpeed, 0.05f),

        new(AttributeType.Spirit, AttributeType.HealingPowerPercent, 0.15f),
        new(AttributeType.Spirit, AttributeType.HealthRegeneration, 0.05f),
        new(AttributeType.Spirit, AttributeType.StatusResistance, 0.1f),
        new(AttributeType.Spirit, AttributeType.CrowdControlResistance, 0.1f),
        new(AttributeType.Spirit, AttributeType.SummonPower, 0.05f),
        new(AttributeType.Spirit, AttributeType.SummonHealth, 0.1f)
    ];

    public static IReadOnlyList<PrimaryAttributeContribution> PrimaryContributions => Contributions;

    public static bool IsPrimary(AttributeType attributeType) =>
        attributeType is AttributeType.Power
            or AttributeType.Fortitude
            or AttributeType.Precision
            or AttributeType.Spirit;

    public static void ApplyPrimaryContributions(IDictionary<AttributeType, float> attributes)
    {
        foreach (var contribution in Contributions)
        {
            var primaryValue = GetValue(attributes, contribution.PrimaryAttribute);
            if (primaryValue == 0)
                continue;

            attributes[contribution.DerivedAttribute] =
                GetValue(attributes, contribution.DerivedAttribute)
                + primaryValue * contribution.ContributionPerPoint;
        }
    }

    public static void ApplyPrimaryDelta(
        IDictionary<AttributeType, float> attributes,
        AttributeType primaryAttribute,
        float primaryDelta)
    {
        if (primaryDelta == 0)
            return;

        foreach (var contribution in Contributions.Where(x => x.PrimaryAttribute == primaryAttribute))
        {
            attributes[contribution.DerivedAttribute] =
                GetValue(attributes, contribution.DerivedAttribute)
                + primaryDelta * contribution.ContributionPerPoint;
        }
    }

    public static float GetContributionPerPoint(
        AttributeType primaryAttribute,
        AttributeType derivedAttribute) =>
        Contributions
            .FirstOrDefault(x =>
                x.PrimaryAttribute == primaryAttribute
                && x.DerivedAttribute == derivedAttribute)
            ?.ContributionPerPoint
        ?? 0f;

    public static float CalculateDefenseMitigation(float defense, float penetration = 0)
    {
        var effectiveDefense = Math.Max(0, defense - penetration);
        return effectiveDefense <= 0
            ? 0
            : effectiveDefense / (effectiveDefense + DefenseRatingScale);
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
            CooldownReductionCapPercent);
        var reducedTicks = authoredTicks * (100d - reductionPercent) / 100d;
        return Math.Max(1, (int)Math.Ceiling(reducedTicks - 1e-9d));
    }

    private static float GetValue(
        IDictionary<AttributeType, float> attributes,
        AttributeType attributeType) =>
        attributes.TryGetValue(attributeType, out var value) ? value : 0f;
}
