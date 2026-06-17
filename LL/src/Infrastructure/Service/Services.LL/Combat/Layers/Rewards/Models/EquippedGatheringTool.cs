using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record EquippedGatheringTool(
    string Name,
    GatheringType GatheringType,
    double YieldBonusPercent,
    double RareChanceBonusPercent,
    double DoubleGatherChancePercent);
