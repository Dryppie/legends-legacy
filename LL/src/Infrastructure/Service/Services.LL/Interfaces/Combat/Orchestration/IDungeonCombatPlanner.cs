using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Interfaces.Combat.Orchestration;

public interface IDungeonCombatPlanner
{
    DungeonCombatPlan CreatePlan(
        Guid dungeonRunId,
        Guid characterId,
        IReadOnlyList<Guid> playerEntityIds,
        IReadOnlyList<Guid> enemySourceEntityIds);

    CombatEncounterPlan CreateEncounterPlan(
        DungeonCombatPlan plan,
        int sequence,
        DateTimeOffset startsAt);
}