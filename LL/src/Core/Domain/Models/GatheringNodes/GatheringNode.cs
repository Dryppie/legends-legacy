using Domain.Models.LootTables;

namespace Domain.Models.GatheringNodes;
public class GatheringNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int LevelRequirement { get; set; }
    public GatheringType GatheringType { get; set; }
    public Guid LootTableId { get; set; }
    public LootTable LootTable { get; set; } = null!;
}