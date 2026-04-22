using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Interfaces.Combat.Orchestration;

public interface ICombatOrchestrator
{
    CombatMode Mode { get; }

    Task<CombatOrchestrationResult> OrchestrateAsync(
        CombatOrchestrationRequest request,
        CancellationToken cancellationToken);
}