using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record CombatEncounterRecord(
    CombatEncounterPlan Plan,
    CombatEncounterResolutionResult Resolution);
