using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Services.LL.Combat.Layers.Resolution.Dungeon;

public static class DungeonEnemyDifficultyScaling
{
    // Each difficulty uses the end of its matching Region as its global baseline.
    // These multipliers are content pressure, applied after the shared stat curves.
    public const int AreasPerRegion = 10;
    public const float TierOneStrengthMultiplier = 1.6f;
    public const float TierTwoStrengthMultiplier = 2.0f;
    public const float TierThreeStrengthMultiplier = 2.0f;

    private static readonly HashSet<AttributeType> ScaledAttributes =
    [
        AttributeType.MaxHealth,
        AttributeType.Power,
        AttributeType.Armor,
        AttributeType.Resistance,
        AttributeType.ArmorPenetration,
        AttributeType.MagicPenetration,
        AttributeType.HealthRegeneration
    ];

    public static float GetStrengthMultiplier(int dungeonTier, float? authoredMultiplier = null)
    {
        if (dungeonTier is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dungeonTier),
                dungeonTier,
                "Dungeon tiers must be between 1 and 3.");
        }

        var contentMultiplier = dungeonTier switch
        {
            1 => TierOneStrengthMultiplier,
            2 => TierTwoStrengthMultiplier,
            3 => TierThreeStrengthMultiplier,
            _ => throw new ArgumentOutOfRangeException(nameof(dungeonTier))
        };

        if (authoredMultiplier is { } multiplier)
        {
            if (!float.IsFinite(multiplier) || multiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(authoredMultiplier));

            contentMultiplier *= multiplier;
        }

        return contentMultiplier;
    }

    public static int GetProgressionPosition(int dungeonTier)
    {
        if (dungeonTier is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dungeonTier),
                dungeonTier,
                "Dungeon tiers must be between 1 and 3.");
        }

        return checked(dungeonTier * AreasPerRegion);
    }

    public static void Apply(CombatEntity enemy, int dungeonTier, float? authoredMultiplier = null)
    {
        var multiplier = GetStrengthMultiplier(dungeonTier, authoredMultiplier);
        if (Math.Abs(multiplier - 1f) < float.Epsilon)
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
