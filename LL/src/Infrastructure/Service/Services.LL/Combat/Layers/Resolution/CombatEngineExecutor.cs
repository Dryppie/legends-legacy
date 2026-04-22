using Domain.Interfaces.Combat;
using Domain.Models.Combat;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution;

public sealed class CombatEngineExecutor : ICombatEngineExecutor
{
    private readonly ICombatContext _combatContext;

    public CombatEngineExecutor(ICombatContext combatContext)
    {
        _combatContext = combatContext;
    }

    public Task<CombatResult> ExecuteAsync(
        CombatEncounterRuntime runtime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var combatResult = _combatContext.InstantiateAndRunCombat(
            runtime.FriendlyParticipants.Select(x => x.Combatant).ToList(),
            runtime.HostileParticipants.Select(x => x.Combatant).ToList());

        combatResult.StartedAt = runtime.Plan.StartsAt;

        return Task.FromResult(combatResult);
    }
}