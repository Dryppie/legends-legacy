using Domain.Models.Regions.Areas;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record IdleEncounterSourceContext(
    Guid CharacterId,
    Area Area,
    TimeSpan EncounterCadence)
    : CombatEncounterSourceContext(CombatMode.Idle);