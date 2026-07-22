using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Services.LL.Combat.Layers.Resolution.Dungeon;

public static class DungeonEnemyDifficultyScaling
{
    public const float StrengthMultiplierPerTier = 5f;

    private static readonly HashSet<AttributeType> ScaledAttributes =
    [
        AttributeType.MaxHealth,
        AttributeType.Power,
        AttributeType.Fortitude,
        AttributeType.Precision,
        AttributeType.Spirit,
        AttributeType.WeaponDamage,
        AttributeType.Armor,
        AttributeType.Resistance,
        AttributeType.ArmorPenetration,
        AttributeType.MagicPenetration,
        AttributeType.HealthRegeneration,
        AttributeType.SummonPower,
        AttributeType.SummonHealth
    ];

    public static float GetStrengthMultiplier(int dungeonTier) =>
        MathF.Pow(StrengthMultiplierPerTier, Math.Max(1, dungeonTier) - 1);

    public static void Apply(CombatEntity enemy, int dungeonTier)
    {
        var multiplier = GetStrengthMultiplier(dungeonTier);
        if (multiplier <= 1f)
            return;

        var modifierAmount = (multiplier - 1f) * 100f;
        foreach (var attributeType in ScaledAttributes)
        {
            enemy.TemporaryModifiers.Add(new DungeonAttributeModifier(
                attributeType,
                modifierAmount,
                ModifierType.Multiplicative));
        }
    }
}
