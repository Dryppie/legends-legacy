using Domain.Models.Snapshots;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonCombatOrchestrationRequest(
    Guid DungeonRunId,
    Guid CharacterId,
    CharacterSnapshot CharacterSnapshot,
    int CurrentRoomIndex,
    IReadOnlyList<string> EnemyCreatureKeys)
    : CombatOrchestrationRequest(CombatMode.Dungeon);
