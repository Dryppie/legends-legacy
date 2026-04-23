namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonCombatPlan(
    Guid DungeonRunId,
    Guid CharacterId,
    IReadOnlyList<Guid> PlayerEntityIds,
    IReadOnlyList<Guid> EnemySourceEntityIds);
