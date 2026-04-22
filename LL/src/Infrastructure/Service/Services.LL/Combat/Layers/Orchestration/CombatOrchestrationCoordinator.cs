using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Orchestration;

namespace Services.LL.Combat.Layers.Orchestration;

public sealed class CombatOrchestrationCoordinator : ICombatOrchestrationCoordinator
{
    private readonly Dictionary<CombatMode, ICombatOrchestrator> _orchestrators;

    public CombatOrchestrationCoordinator(IEnumerable<ICombatOrchestrator> orchestrators)
    {
        _orchestrators = orchestrators.ToDictionary(x => x.Mode);
    }

    public Task<CombatOrchestrationResult> OrchestrateAsync(
        CombatOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!_orchestrators.TryGetValue(request.Mode, out var orchestrator))
        {
            throw new InvalidOperationException(
                $"No combat orchestrator is registered for mode '{request.Mode}'.");
        }

        return orchestrator.OrchestrateAsync(request, cancellationToken);
    }
}