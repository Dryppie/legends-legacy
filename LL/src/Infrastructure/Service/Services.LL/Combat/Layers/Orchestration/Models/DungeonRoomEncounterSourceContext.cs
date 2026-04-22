namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonRoomEncounterSourceContext(
    Guid DungeonRunId,
    int RoomIndex,
    string RoomType)
    : CombatEncounterSourceContext(CombatMode.DungeonRoom);
