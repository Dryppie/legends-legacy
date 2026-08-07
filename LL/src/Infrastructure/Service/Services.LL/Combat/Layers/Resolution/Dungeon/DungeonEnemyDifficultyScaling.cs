using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Services.LL.Combat.Layers.Resolution.Dungeon;

public static class DungeonEnemyDifficultyScaling
{
    // Tier I deliberately starts above the authored creature baseline. Every
    // higher difficulty compounds from that dungeon baseline, not from the
    // original world-creature baseline.
    public const float TierOneStrengthMultiplier = 2.75f;
    public const float StrengthMultiplierPerTier = 5f;

    private static readonly HashSet<AttributeType> ScaledAttributes =
    [
        AttributeType.MaxHealth,
        AttributeType.Power,
        AttributeType.Armor,
        AttributeType.Resistance,
        AttributeType.ArmorPenetration,
        AttributeType.MagicPenetration,
        AttributeType.HealthRegeneration,
        AttributeType.SummonPower,
        AttributeType.SummonHealth
    ];

    public static float GetStrengthMultiplier(int dungeonTier) =>
        TierOneStrengthMultiplier
        * MathF.Pow(StrengthMultiplierPerTier, Math.Max(0, dungeonTier - 1));

    // Armor and Resistance now stop at 80% instead of approaching 100% as ratings.
    // Move the removed Heroic/Mythic effective health into visible Max Health so
    // dungeon durability does not depend on an opaque defense curve.
    public static float GetDurabilityCompensation(int dungeonTier) => dungeonTier switch
    {
        <= 2 => 1f,
        _ => 2.45f
    };

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

        var durabilityCompensation = GetDurabilityCompensation(dungeonTier);
        if (durabilityCompensation > 1f)
        {
            enemy.TemporaryModifiers.Add(new DungeonAttributeModifier(
                AttributeType.MaxHealth,
                (durabilityCompensation - 1f) * 100f,
                ModifierType.Multiplicative));
        }
    }
}
