using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Services.LL.Combat.Layers.Resolution.Dungeon;

public static class DungeonEnemyDifficultyScaling
{
    // Each dungeon difficulty is anchored to the matching full Epic equipment
    // milestone: Tier I equipment for Normal, Tier II for Heroic, and Tier III
    // for Mythic. Keep these explicit so later tiers cannot accidentally inherit
    // an exponential multiplier unrelated to attainable equipment progression.
    public const float TierOneStrengthMultiplier = 3.6f;
    public const float TierTwoStrengthMultiplier = 6.25f;
    public const float TierThreeStrengthMultiplier = 8.2f;

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

        if (authoredMultiplier is { } multiplier)
        {
            if (!float.IsFinite(multiplier) || multiplier <= 1f)
                throw new ArgumentOutOfRangeException(nameof(authoredMultiplier));

            return multiplier;
        }

        return dungeonTier switch
        {
            1 => TierOneStrengthMultiplier,
            2 => TierTwoStrengthMultiplier,
            3 => TierThreeStrengthMultiplier,
            _ => throw new ArgumentOutOfRangeException(nameof(dungeonTier))
        };
    }

    public static void Apply(CombatEntity enemy, int dungeonTier, float? authoredMultiplier = null)
    {
        var multiplier = GetStrengthMultiplier(dungeonTier, authoredMultiplier);
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
