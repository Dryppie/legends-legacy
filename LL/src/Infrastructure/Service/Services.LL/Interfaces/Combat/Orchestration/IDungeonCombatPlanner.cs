using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Interfaces.Combat.Orchestration;

public interface IDungeonCombatPlanner
{
    DungeonCombatPlan CreatePlan(
        Guid dungeonRunId,
        Guid characterId,
        CharacterSnapshot characterSnapshot,
        IReadOnlyList<Guid> playerEntityIds,
        IReadOnlyList<Guid> enemySourceEntityIds);

    CombatEncounterPlan CreateEncounterPlan(
        DungeonCombatPlan plan,
        int sequence,
        DateTimeOffset startsAt);
}
