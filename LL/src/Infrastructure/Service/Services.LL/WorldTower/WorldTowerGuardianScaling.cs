using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.WorldTower;

namespace Services.LL.WorldTower;

public static class WorldTowerGuardianScaling
{
    public static void Apply(
        CombatEntity guardian,
        TowerGuardianScalingDefinition scaling)
    {
        ArgumentNullException.ThrowIfNull(guardian);
        ArgumentNullException.ThrowIfNull(scaling);

        Add(guardian, AttributeType.MaxHealth, scaling.Health);
        Add(guardian, AttributeType.Power, scaling.Offense);
        Add(guardian, AttributeType.Armor, scaling.Defense);
        Add(guardian, AttributeType.Resistance, scaling.Resistance);
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
