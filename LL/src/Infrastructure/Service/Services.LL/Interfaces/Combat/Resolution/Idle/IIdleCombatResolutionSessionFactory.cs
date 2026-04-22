using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Interfaces.Combat.Resolution.Idle;

public interface IIdleCombatResolutionSessionFactory
{
    Task<ICombatResolutionSession> CreateAsync(
        IdleCombatPlan plan,
        CancellationToken cancellationToken);
}