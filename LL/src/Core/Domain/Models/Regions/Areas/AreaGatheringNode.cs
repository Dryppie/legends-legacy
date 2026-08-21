using System.Text.Json.Serialization;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Domain.Models.Regions.Areas;
public class AreaGatheringNode
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string AreaId { get; set; } = string.Empty;
    [JsonIgnore]
    public Area Area { get; set; } = default!;
    public GatheringType Type { get; set; }
    public int? LevelRequirement { get; set; }
    public float ProcChance { get; set; } = 1.0f;
    public double YieldBonusPercent { get; set; }
    public string? RewardTableId { get; set; }
}
