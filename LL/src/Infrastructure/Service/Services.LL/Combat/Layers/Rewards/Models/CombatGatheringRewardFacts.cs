namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record CombatGatheringRewardFacts(
    Guid CharacterId,
    int Victories,
    EquippedGatheringTool? EquippedTool,
    IReadOnlyList<CombatGatheringNode> GatheringNodes);
