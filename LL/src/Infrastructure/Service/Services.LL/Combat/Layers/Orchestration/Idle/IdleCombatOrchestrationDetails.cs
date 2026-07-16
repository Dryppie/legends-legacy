using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Combat.Layers.Orchestration.Idle;

public sealed record IdleCombatOrchestrationDetails(
    DateTimeOffset From,
    DateTimeOffset RequestedTo,
    DateTimeOffset ProcessedUntil,
    int PlannedEncounterCount,
    TimeSpan EncounterCadence)
    : ICombatOrchestrationDetails
{
    public CombatMode Mode => CombatMode.Idle;

    // ProcessedUntil is the next encounter due time. If it equals RequestedTo,
    // that boundary encounter is still due and must be processed.
    public bool FullyProcessed => ProcessedUntil > RequestedTo;

    public TimeSpan ProcessedDuration => ProcessedUntil - From;
}
