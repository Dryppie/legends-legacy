namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonRoomCombatOrchestrationRequest(
    Guid DungeonRunId,
    Guid CharacterId,
    int CurrentRoomIndex,
    DateTimeOffset Now)
    : CombatOrchestrationRequest(CombatMode.DungeonRoom);