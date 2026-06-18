using Domain.Models.LootTables;
using Domain.Models.Professions.Gathering.GatheringNodes;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record CombatGatheringNode(
    string Id,
    string Name,
    GatheringType Type,
    int? LevelRequirement,
    float ProcChance,
    LootTable LootTable);
