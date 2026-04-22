using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Interfaces.Combat.Orchestration;

public interface ICombatOrchestrationCoordinator
{
    Task<CombatOrchestrationResult> OrchestrateAsync(
        CombatOrchestrationRequest request,
        CancellationToken cancellationToken);
}
