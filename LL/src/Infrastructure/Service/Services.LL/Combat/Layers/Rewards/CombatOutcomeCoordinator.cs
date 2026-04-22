using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards;

public sealed class CombatOutcomeCoordinator : ICombatOutcomeCoordinator
{
    private readonly Dictionary<CombatMode, ICombatOutcomeProcessor> _processors;

    public CombatOutcomeCoordinator(IEnumerable<ICombatOutcomeProcessor> processors)
    {
        _processors = processors.ToDictionary(x => x.Mode);
    }

    public Task<CombatSession> ApplyAsync(
        CombatOutcomeRequest request,
        CancellationToken cancellationToken)
    {
        if (!_processors.TryGetValue(request.OrchestrationResult.Mode, out var processor))
        {
            throw new InvalidOperationException(
                $"No outcome processor is registered for combat mode '{request.OrchestrationResult.Mode}'.");
        }

        return processor.ApplyAsync(request, cancellationToken);
    }
}
