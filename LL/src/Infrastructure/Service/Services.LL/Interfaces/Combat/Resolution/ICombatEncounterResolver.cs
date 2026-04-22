using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public interface ICombatEncounterResolver
{
    Task<CombatEncounterResolutionResult> ResolveAsync(
        CombatEncounterPlan encounterPlan,
        CancellationToken cancellationToken);
}