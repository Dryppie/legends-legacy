using Domain.Models.Professions.Gathering.GatheringNodes;
using Domain.Models.Rewards;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record CombatGatheringNode(
    string Id,
    string Name,
    GatheringType Type,
    int? LevelRequirement,
    float ProcChance,
    string? RewardTableId = null,
    RewardTableDefinition? RewardTable = null)
{
    public bool HasRewards =>
        !string.IsNullOrWhiteSpace(RewardTableId) ||
        RewardTable is not null;
}
