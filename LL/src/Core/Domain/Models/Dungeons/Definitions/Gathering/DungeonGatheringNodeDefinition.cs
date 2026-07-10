using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Domain.Models.Dungeons.Definitions.Gathering;

public sealed class DungeonGatheringNodeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GatheringType Type { get; set; }
    public int? LevelRequirement { get; set; }
    public float ProcChance { get; set; } = 1.0f;
    public string? RewardTableId { get; set; }
    public List<DungeonGatheringLootEntryDefinition> Loot { get; set; } = [];
}
