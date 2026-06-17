using Domain.Models.Inventories;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Domain.Models.Combat;

public sealed class GatheringRewardResult
{
    public GatheringType ToolType { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ExperienceGained { get; set; }
    public List<InventoryItem> ItemsGained { get; set; } = [];
    public string? Message { get; set; }
}
