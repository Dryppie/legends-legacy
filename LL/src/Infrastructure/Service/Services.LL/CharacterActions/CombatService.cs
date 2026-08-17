using Application.Interfaces.Services.LL.CharacterActions;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.Sessions;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Reward;
using Common.Randomness;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Services.LL.CharacterActions;

public class CombatService : ICombatService
{
    private readonly ICombatOrchestrationCoordinator _orchestrationCoordinator;
    private readonly ICombatOutcomeCoordinator _outcomeCoordinator;
    private readonly IResolutionRandomSource? _resolutionRandom;
    private readonly IdleCombatProgressionOptions _options;

    public CombatService(
        ICombatOrchestrationCoordinator orchestrationCoordinator,
        ICombatOutcomeCoordinator outcomeCoordinator,
        IResolutionRandomSource? resolutionRandom = null,
        IOptions<IdleCombatProgressionOptions>? options = null)
    {
        _orchestrationCoordinator = orchestrationCoordinator;
        _outcomeCoordinator = outcomeCoordinator;
        _resolutionRandom = resolutionRandom;
        _options = options?.Value ?? new IdleCombatProgressionOptions();
    }

    public async Task<CombatSession> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var accumulator = new CombatSessionAccumulator();
        IdleCombatOrchestrationDetails? lastDetails = null;
        var processedCount = 0;

        // MaximumEncountersPerResolution remains the memory/CPU size of one
        // orchestration batch. Continuation happens here, inside the same command
        // and transaction, so clients receive one compact result instead of making
        // dozens of round trips for a normal 24-hour return.
        for (var batch = 0; batch < _options.MaximumBatchesPerResolution; batch++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var previousBoundary = characterAction.NextResolutionAtUtc
                ?? throw new InvalidOperationException(
                    "Active idle combat requires a next-resolution boundary.");
            var orchestrationRequest = new IdleCombatOrchestrationRequest(
                characterAction,
                now);

            using var randomScope = _resolutionRandom?.UseSeed(StableRandom.Seed(
                "idle-combat-batch-v1",
                characterAction.CharacterId.ToString("N"),
                characterAction.ScheduleGeneration.ToString(CultureInfo.InvariantCulture),
                previousBoundary.UtcTicks.ToString(CultureInfo.InvariantCulture)));

            var orchestrationResult = await _orchestrationCoordinator.OrchestrateAsync(
                orchestrationRequest,
                cancellationToken);
            var details = orchestrationResult.Details as IdleCombatOrchestrationDetails
                ?? throw new InvalidOperationException(
                    "Idle combat orchestration returned incompatible details.");

            if (orchestrationResult.EncounterCount > 0 && details.ProcessedUntil <= previousBoundary)
            {
                throw new InvalidOperationException(
                    "Idle combat resolution did not advance its persisted boundary.");
            }

            characterAction.NextResolutionAtUtc = details.ProcessedUntil;
            processedCount = checked(processedCount + orchestrationResult.EncounterCount);
            lastDetails = details;

            var session = await _outcomeCoordinator.ApplyAsync(
                new CombatOutcomeRequest(orchestrationRequest, orchestrationResult),
                cancellationToken);
            accumulator.Add(session);

            if (orchestrationResult.EncounterCount == 0 || details.ProcessedUntil > now)
                break;
        }

        characterAction.ProcessedCount = processedCount;
        characterAction.HasMoreDueWork = characterAction.NextResolutionAtUtc <= now;
        characterAction.ResolutionIntervalMs = checked(
            (int)(lastDetails?.EncounterCadence.TotalMilliseconds
                ?? TimeSpan.FromSeconds(_options.EncounterCadenceSeconds).TotalMilliseconds));

        return accumulator.Build();
    }
}
