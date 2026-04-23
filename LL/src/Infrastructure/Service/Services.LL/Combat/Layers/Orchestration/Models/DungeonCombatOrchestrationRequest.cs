namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonCombatOrchestrationRequest(
    Guid DungeonRunId,
    Guid CharacterId,
    int CurrentRoomIndex,
    IReadOnlyList<string> EnemyCreatureKeys)
    : CombatOrchestrationRequest(CombatMode.Dungeon);