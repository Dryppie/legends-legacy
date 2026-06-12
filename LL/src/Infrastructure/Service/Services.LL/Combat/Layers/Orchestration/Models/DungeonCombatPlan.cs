using Domain.Models.Snapshots;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonCombatPlan(
    Guid DungeonRunId,
    Guid CharacterId,
    CharacterSnapshot CharacterSnapshot,
    IReadOnlyList<Guid> PlayerEntityIds,
    IReadOnlyList<Guid> EnemySourceEntityIds);
