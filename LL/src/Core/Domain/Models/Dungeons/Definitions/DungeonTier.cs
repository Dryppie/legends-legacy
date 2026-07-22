namespace Domain.Models.Dungeons.Definitions;

public enum DungeonTier
{
    Normal = 0,
    Heroic = 1,
    Mythic = 2
}

public static class DungeonTierExtensions
{
    public static int ToDefinitionTier(this DungeonTier tier) => tier switch
    {
        DungeonTier.Normal => 1,
        DungeonTier.Heroic => 2,
        DungeonTier.Mythic => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unsupported dungeon tier.")
    };

    public static DungeonTier ToDungeonTier(this int definitionTier) => definitionTier switch
    {
        1 => DungeonTier.Normal,
        2 => DungeonTier.Heroic,
        3 => DungeonTier.Mythic,
        _ => throw new ArgumentOutOfRangeException(
            nameof(definitionTier),
            definitionTier,
            "Dungeon definition tiers must be between 1 and 3.")
    };
}
