using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Combat.Layers.Orchestration.Dungeon;

public sealed record DungeonCombatOrchestrationDetails(
    Guid DungeonRunId,
    DungeonProgressionStatus ProgressionStatus)
    : ICombatOrchestrationDetails
{
    public CombatMode Mode => CombatMode.Dungeon;

    public bool RunCompleted => ProgressionStatus == DungeonProgressionStatus.RunCompleted;

    public bool RunFailed => ProgressionStatus == DungeonProgressionStatus.Failed;
}
public enum DungeonProgressionStatus
{
    Active = 1,
    RestSiteReached = 2,
    RoomCleared = 3,
    RunCompleted = 4,
    Failed = 5,
    Retreated = 6
}
