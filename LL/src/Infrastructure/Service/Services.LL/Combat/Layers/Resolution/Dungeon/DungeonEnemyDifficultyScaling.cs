using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Services.LL.Combat.Layers.Resolution.Dungeon;

public static class DungeonEnemyDifficultyScaling
{
    // Each difficulty uses its matching region end as a global baseline, with the
    // dungeon's content region acting as a floor. Content pressure is applied after
    // the shared stat curves.
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

    public static int GetProgressionPosition(int dungeonTier, int dungeonRegion = 1)
    {
        if (dungeonTier is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dungeonTier),
                dungeonTier,
                "Dungeon tiers must be between 1 and 3.");
        }

        if (dungeonRegion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dungeonRegion),
                dungeonRegion,
                "Dungeon regions must be positive.");
        }

        // A difficulty grade may raise a dungeon into a later progression band,
        // but it must never pull a later-region dungeon below its own region.
        return checked(Math.Max(dungeonTier, dungeonRegion) * AreasPerRegion);
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
