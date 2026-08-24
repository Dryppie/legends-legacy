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
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using Services.LL.Combat;

namespace Services.LL.CharacterActions;

public class CombatService : ICombatService
{
    private readonly ICombatOrchestrationCoordinator _orchestrationCoordinator;
    private readonly ICombatOutcomeCoordinator _outcomeCoordinator;
    private readonly IResolutionRandomSource? _resolutionRandom;
    private readonly IdleCombatProgressionOptions _options;
    private readonly ILogger<CombatService>? _logger;

    public CombatService(
        ICombatOrchestrationCoordinator orchestrationCoordinator,
        ICombatOutcomeCoordinator outcomeCoordinator,
        IResolutionRandomSource? resolutionRandom = null,
        IOptions<IdleCombatProgressionOptions>? options = null,
        ILogger<CombatService>? logger = null)
    {
        _orchestrationCoordinator = orchestrationCoordinator;
        _outcomeCoordinator = outcomeCoordinator;
        _resolutionRandom = resolutionRandom;
        _options = options?.Value ?? new IdleCombatProgressionOptions();
        _logger = logger;
    }

    public async Task<CombatSession?> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var resolveStartedAt = IdleCombatTelemetry.Start();
        var accumulator = new CombatSessionAccumulator();
        IdleCombatOrchestrationDetails? lastDetails = null;
        var processedCount = 0;
        var processedBatches = 0;

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
            var captureFinalEncounterLog = IsFinalResponseBatch(
                previousBoundary,
                now,
                batch);
            var orchestrationRequest = new IdleCombatOrchestrationRequest(
                characterAction,
                now,
                captureFinalEncounterLog);

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

            // An early or duplicate resolver has no encounter to reward or render.
            // Treat it as a real no-op instead of manufacturing an empty combat
            // result that can temporarily replace the last completed encounter.
            if (orchestrationResult.EncounterCount == 0)
                break;

            var session = await _outcomeCoordinator.ApplyAsync(
                new CombatOutcomeRequest(orchestrationRequest, orchestrationResult),
                cancellationToken);
            accumulator.Add(session);
            processedBatches++;

            if (details.ProcessedUntil > now)
                break;
        }

        characterAction.ProcessedCount = processedCount;
        characterAction.HasMoreDueWork = characterAction.NextResolutionAtUtc <= now;
        characterAction.ResolutionIntervalMs = checked(
            (int)(lastDetails?.EncounterCadence.TotalMilliseconds
                ?? TimeSpan.FromSeconds(_options.EncounterCadenceSeconds).TotalMilliseconds));

        var elapsed = Stopwatch.GetElapsedTime(resolveStartedAt);
        IdleCombatTelemetry.RecordResolve(resolveStartedAt, processedCount, processedBatches);
        if (processedCount > 0)
        {
            _logger?.LogDebug(
                "Idle combat catch-up resolved {EncounterCount} encounters in {BatchCount} batches over {ElapsedMilliseconds} ms ({EncountersPerSecond} encounters/sec).",
                processedCount,
                processedBatches,
                elapsed.TotalMilliseconds,
                elapsed.TotalSeconds <= 0 ? processedCount : processedCount / elapsed.TotalSeconds);
        }
        return processedCount == 0 ? null : accumulator.Build();
    }

    private bool IsFinalResponseBatch(
        DateTimeOffset nextEncounterAt,
        DateTimeOffset now,
        int batchIndex)
    {
        if (batchIndex == _options.MaximumBatchesPerResolution - 1)
        {
            return true;
        }

        var cadence = TimeSpan.FromSeconds(_options.EncounterCadenceSeconds);
        if (nextEncounterAt > now + cadence)
        {
            nextEncounterAt = now;
        }

        var oldestRetainedBoundary = now - TimeSpan.FromHours(
            _options.MaximumOfflineHours);
        if (nextEncounterAt < oldestRetainedBoundary)
        {
            nextEncounterAt = oldestRetainedBoundary;
        }

        if (nextEncounterAt > now)
        {
            return true;
        }

        var dueEncounterCount = checked(
            1 + (int)((now - nextEncounterAt).Ticks / cadence.Ticks));
        return dueEncounterCount <= _options.MaximumEncountersPerResolution;
    }
}
