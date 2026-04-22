using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution;

public sealed class DefaultCombatEncounterResolver : ICombatEncounterResolver
{
    private readonly IEncounterEntityLoader _entityLoader;
    private readonly ICombatEncounterRuntimeFactory _runtimeFactory;
    private readonly ICombatEngineExecutor _engineExecutor;
    private readonly ICombatEncounterResultFactory _resultFactory;

    public DefaultCombatEncounterResolver(
        IEncounterEntityLoader entityLoader,
        ICombatEncounterRuntimeFactory runtimeFactory,
        ICombatEngineExecutor engineExecutor,
        ICombatEncounterResultFactory resultFactory)
    {
        _entityLoader = entityLoader;
        _runtimeFactory = runtimeFactory;
        _engineExecutor = engineExecutor;
        _resultFactory = resultFactory;
    }

    public async Task<CombatEncounterResolutionResult> ResolveAsync(
        CombatEncounterPlan encounterPlan,
        CancellationToken cancellationToken)
    {
        var loadedEntities = await _entityLoader.LoadAsync(encounterPlan, cancellationToken);
        var runtime = await _runtimeFactory.CreateAsync(encounterPlan, loadedEntities, cancellationToken);
        var combatResult = await _engineExecutor.ExecuteAsync(runtime, cancellationToken);

        return _resultFactory.Create(runtime, combatResult);
    }
}
