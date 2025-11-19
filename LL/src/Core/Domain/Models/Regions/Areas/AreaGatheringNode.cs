using Domain.Models.LootTables;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Domain.Models.Regions.Areas;
public class AreaGatheringNode
{
    public string Id { get; set; } = string.Empty;

    public string AreaId { get; set; } = string.Empty;
    public Area Area { get; set; } = default!;
    public GatheringType Type { get; set; }
    public int? LevelRequirement { get; set; }
    public float ProcChance { get; set; } = 1.0f;
    public LootTable LootTable { get; set; } = null!;
}
