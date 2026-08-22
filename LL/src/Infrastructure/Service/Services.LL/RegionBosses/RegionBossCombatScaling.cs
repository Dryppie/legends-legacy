using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.RegionBosses;

namespace Services.LL.RegionBosses;

public static class RegionBossCombatScaling
{
    public static void Apply(CombatEntity entity, RegionBossDefinition definition, int bossLevel, int partySize)
    {
        var levelOffset = Math.Max(0, bossLevel - 1);
        var partyHealth = 0.6d + 0.1d * Math.Clamp(partySize - 1, 0, 4);
        var partyPower = 0.8d + 0.05d * Math.Clamp(partySize - 1, 0, 4);
        AddMultiplier(entity, AttributeType.MaxHealth,
            definition.BaseScaling.Health * Math.Pow(definition.LevelScaling.HealthGrowth, levelOffset) * partyHealth);
        AddMultiplier(entity, AttributeType.Power,
            definition.BaseScaling.Power * Math.Pow(definition.LevelScaling.PowerGrowth, levelOffset) * partyPower);
        AddMultiplier(entity, AttributeType.Armor,
            definition.BaseScaling.Armor * (1 + definition.LevelScaling.ArmorGrowthPerLevel * levelOffset));
        AddMultiplier(entity, AttributeType.Resistance,
            definition.BaseScaling.Resistance * (1 + definition.LevelScaling.ResistanceGrowthPerLevel * levelOffset));
        AddMultiplier(entity, AttributeType.ArmorPenetration,
            definition.BaseScaling.Penetration * (1 + definition.LevelScaling.PenetrationGrowthPerLevel * levelOffset));
        AddMultiplier(entity, AttributeType.MagicPenetration,
            definition.BaseScaling.Penetration * (1 + definition.LevelScaling.PenetrationGrowthPerLevel * levelOffset));
        AddMultiplier(entity, AttributeType.HealthRegeneration, definition.BaseScaling.Regeneration);
    }

    private static void AddMultiplier(CombatEntity entity, AttributeType attribute, double multiplier)
    {
        if (!double.IsFinite(multiplier) || multiplier <= 0)
            throw new InvalidOperationException($"Invalid Region Boss {attribute} multiplier '{multiplier}'.");
        var percent = Math.Clamp((multiplier - 1d) * 100d, -99d, 1_000_000d);
        if (Math.Abs(percent) < 0.0001d)
            return;
        entity.ModifyAttribute(new DungeonAttributeModifier(
            attribute,
            (float)percent,
            ModifierType.Multiplicative));
    }
}
