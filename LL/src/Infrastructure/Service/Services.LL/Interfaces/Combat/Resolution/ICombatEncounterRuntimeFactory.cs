using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public interface ICombatEncounterRuntimeFactory
{
    Task<CombatEncounterRuntime> CreateAsync(
        CombatEncounterPlan encounterPlan,
        LoadedEncounterEntities loadedEntities,
        CancellationToken cancellationToken);
}
