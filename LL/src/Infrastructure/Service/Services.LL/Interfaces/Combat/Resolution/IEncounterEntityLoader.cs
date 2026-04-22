using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public interface IEncounterEntityLoader
{
    Task<LoadedEncounterEntities> LoadAsync(
        CombatEncounterPlan encounterPlan,
        CancellationToken cancellationToken);
}
