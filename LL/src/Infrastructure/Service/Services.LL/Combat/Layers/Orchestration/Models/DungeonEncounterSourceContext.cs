namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonEncounterSourceContext(
    Guid DungeonRunId)
    : CombatEncounterSourceContext(CombatMode.Dungeon);
