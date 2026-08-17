using Domain.Models.Regions.Areas;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record IdleCombatPlan(
    Guid CharacterId,
    DateTimeOffset From,
    DateTimeOffset RequestedTo,
    DateTimeOffset ExecutableUntil,
    TimeSpan EncounterCadence,
    long ScheduleGeneration,
    IReadOnlyList<Guid> PlayerEntityIds,
    Area Area,
    int PlannedEncounterCount);
