using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.WorldTower;

namespace Services.LL.WorldTower;

public static class WorldTowerGuardianScaling
{
    public const float HealthContentMultiplier = 4.5f;
    public const float OffenseContentMultiplier = 16f;
    public const float DurabilityContentMultiplier = 4f;

    public static void Apply(
        CombatEntity guardian,
        TowerGuardianScalingDefinition scaling,
        int participantCount)
    {
        ArgumentNullException.ThrowIfNull(guardian);
        ArgumentNullException.ThrowIfNull(scaling);
        if (participantCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(participantCount));

        var participantHealth = MathF.Pow(participantCount, 0.85f);
        var participantOffense = 1f + 0.05f * (participantCount - 1);
        var participantDurability = MathF.Pow(participantCount / 5f, 0.25f);

        Add(guardian, AttributeType.MaxHealth,
            HealthContentMultiplier * participantHealth * scaling.Health);
        Add(guardian, AttributeType.Power,
            OffenseContentMultiplier * participantOffense * scaling.Offense);
        Add(guardian, AttributeType.Armor,
            DurabilityContentMultiplier * participantDurability * scaling.Defense);
        Add(guardian, AttributeType.Resistance,
            DurabilityContentMultiplier * participantDurability * scaling.Resistance);
        Add(guardian, AttributeType.ArmorPenetration, scaling.Penetration);
        Add(guardian, AttributeType.MagicPenetration, scaling.Penetration);
        Add(guardian, AttributeType.HealthRegeneration, scaling.Regeneration);
    }

    private static void Add(
        CombatEntity guardian,
        AttributeType attribute,
        float multiplier)
    {
        if (Math.Abs(multiplier - 1f) < float.Epsilon)
            return;

        guardian.TemporaryModifiers.Add(new DungeonAttributeModifier(
            attribute,
            (multiplier - 1f) * 100f,
            ModifierType.Multiplicative));
    }
}
