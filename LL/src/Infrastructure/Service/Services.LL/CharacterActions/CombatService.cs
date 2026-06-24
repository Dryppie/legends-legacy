using Application.Interfaces.Services.LL.CharacterActions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.CharacterActions;

public class CombatService : ICombatService
{
    private readonly ICombatOrchestrationCoordinator _orchestrationCoordinator;
    private readonly ICombatOutcomeCoordinator _outcomeCoordinator;

    public CombatService(ICombatOrchestrationCoordinator orchestrationCoordinator, ICombatOutcomeCoordinator outcomeCoordinator)
    {
        _orchestrationCoordinator = orchestrationCoordinator;
        _outcomeCoordinator = outcomeCoordinator;
    }

    public async Task<CombatSession> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var orchestrationRequest = new IdleCombatOrchestrationRequest(
            characterAction,
            now);

        var orchestrationResult = await _orchestrationCoordinator.OrchestrateAsync(
            orchestrationRequest,
            cancellationToken);

        // For combat actions, UpdatedAt is the next encounter due time. A new
        // action starts due immediately, then each processed encounter advances
        // the scheduler by the idle cadence.
        characterAction.UpdatedAt = (orchestrationResult.Details as IdleCombatOrchestrationDetails)!.ProcessedUntil;

        var outcomeRequest = new CombatOutcomeRequest(
            orchestrationRequest,
            orchestrationResult);

        return await _outcomeCoordinator.ApplyAsync(
            outcomeRequest,
            cancellationToken);
    }
}
