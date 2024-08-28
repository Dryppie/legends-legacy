using Domain.Models.LootTables;

namespace Domain.Models.GatheringNodes;
public class GatheringNode
{
    public Guid Id { get; set; }
    public Guid LootTableId { get; set; }
    public LootTable LootTable { get; set; } = null!;
}