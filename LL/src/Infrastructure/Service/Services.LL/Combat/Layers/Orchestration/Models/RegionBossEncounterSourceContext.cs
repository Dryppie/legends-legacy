namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record RegionBossEncounterSourceContext(Guid RegionBossRunId)
    : CombatEncounterSourceContext(CombatMode.RegionBoss);
