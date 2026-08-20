using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Raids;

namespace Services.LL.Raids;

public static class RaidCombatScaling
{
    public static void Apply(CombatEntity entity, RaidAttributeScalingDefinition scaling)
    {
        Add(entity, AttributeType.MaxHealth, scaling.Health);
        Add(entity, AttributeType.Power, scaling.Offense);
        Add(entity, AttributeType.Armor, scaling.Defense);
        Add(entity, AttributeType.Resistance, scaling.Resistance);
        Add(entity, AttributeType.ArmorPenetration, scaling.Penetration);
        Add(entity, AttributeType.MagicPenetration, scaling.Penetration);
        Add(entity, AttributeType.HealthRegeneration, scaling.Regeneration);
    }

    public static void AddPercent(CombatEntity entity, AttributeType attribute, decimal percent)
    {
        if (percent == 0)
            return;
        entity.TemporaryModifiers.Add(new DungeonAttributeModifier(
            attribute,
            (float)percent,
            ModifierType.Multiplicative));
    }

    private static void Add(CombatEntity entity, AttributeType attribute, float multiplier)
    {
        if (Math.Abs(multiplier - 1f) < float.Epsilon)
            return;
        AddPercent(entity, attribute, (decimal)((multiplier - 1f) * 100f));
    }
}
