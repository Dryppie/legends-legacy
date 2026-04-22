using Domain.Models.Combat;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public interface ICombatEngineExecutor
{
    Task<CombatResult> ExecuteAsync(
        CombatEncounterRuntime runtime,
        CancellationToken cancellationToken);
}