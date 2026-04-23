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

    public bool FullyProcessed => ProcessedUntil >= RequestedTo;

    public TimeSpan ProcessedDuration => ProcessedUntil - From;
}