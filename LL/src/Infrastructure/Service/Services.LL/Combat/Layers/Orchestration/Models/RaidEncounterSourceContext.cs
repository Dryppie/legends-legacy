namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record RaidEncounterSourceContext(
    Guid RaidRunId,
    int PhaseIndex,
    string StageKey)
    : CombatEncounterSourceContext(CombatMode.Raid);