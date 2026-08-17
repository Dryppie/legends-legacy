using Application.Interfaces.Services.LL.CharacterActions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Reward;
using Common.Randomness;
using System.Globalization;

namespace Services.LL.CharacterActions;

public class CombatService : ICombatService
{
    private readonly ICombatOrchestrationCoordinator _orchestrationCoordinator;
    private readonly ICombatOutcomeCoordinator _outcomeCoordinator;
    private readonly IResolutionRandomSource? _resolutionRandom;

    public CombatService(
        ICombatOrchestrationCoordinator orchestrationCoordinator,
        ICombatOutcomeCoordinator outcomeCoordinator,
        IResolutionRandomSource? resolutionRandom = null)
    {
        _orchestrationCoordinator = orchestrationCoordinator;
        _outcomeCoordinator = outcomeCoordinator;
        _resolutionRandom = resolutionRandom;
    }

    public async Task<CombatSession> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var orchestrationRequest = new IdleCombatOrchestrationRequest(
            characterAction,
            now);

        using var randomScope = _resolutionRandom?.UseSeed(StableRandom.Seed(
            "idle-combat-batch-v1",
            characterAction.CharacterId.ToString("N"),
            characterAction.ScheduleGeneration.ToString(CultureInfo.InvariantCulture),
            (characterAction.NextResolutionAtUtc?.UtcTicks ?? 0).ToString(CultureInfo.InvariantCulture)));

        var orchestrationResult = await _orchestrationCoordinator.OrchestrateAsync(orchestrationRequest, cancellationToken);

        var details = (orchestrationResult.Details as IdleCombatOrchestrationDetails)!;
        characterAction.NextResolutionAtUtc = details.ProcessedUntil;
        characterAction.ProcessedCount = orchestrationResult.EncounterCount;
        characterAction.HasMoreDueWork = details.ProcessedUntil <= now;
        characterAction.ResolutionIntervalMs = checked((int)details.EncounterCadence.TotalMilliseconds);

        var outcomeRequest = new CombatOutcomeRequest(
            orchestrationRequest,
            orchestrationResult);

        return await _outcomeCoordinator.ApplyAsync(
            outcomeRequest,
            cancellationToken);
    }
}
