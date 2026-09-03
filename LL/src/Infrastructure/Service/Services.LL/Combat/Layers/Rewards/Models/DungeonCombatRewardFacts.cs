using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Items;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record DungeonCombatRewardFacts(
    Guid DungeonRunId,
    Guid CharacterId,
    int CurrentRoomIndex,
    int DungeonTier,
    RoomType RoomType,
    string? FeaturedEssenceMonsterDefinitionId,
    IReadOnlyDictionary<ItemType, double> MonsterLootModifiers,
    IReadOnlyList<Guid> PlayerEntityIds,
    IReadOnlyList<DungeonEncounterRewardFacts> Encounters)
{
    public DungeonEncounterRewardFacts? LastEncounter =>
        Encounters.Count == 0 ? null : Encounters[^1];
}
