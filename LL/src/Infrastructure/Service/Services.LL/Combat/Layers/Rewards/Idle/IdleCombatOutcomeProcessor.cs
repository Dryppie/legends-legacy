using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Prophecies;
using Application.UseCases.Outbox;
using Application.UseCases.Prophecies.Events;
using Domain.Models.CharacterActions.Sessions;
using MediatR;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Combat;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleCombatOutcomeProcessor : ICombatOutcomeProcessor
{
    private readonly IIdleCombatRewardFactBuilder _factBuilder;
    private readonly IIdleCombatRewardCalculator _calculator;
    private readonly IIdleCombatRewardApplier _applier;
    private readonly IIdleCombatSessionFactory _sessionFactory;
    private readonly IPublisher _publisher;
    private readonly IGameEventOutbox _outbox;
    private readonly ICreatureArchiveService _creatureArchiveService;
    private readonly List<IdleCombatSettlementBatch> _pendingSettlement = [];

    public IdleCombatOutcomeProcessor(
        IIdleCombatRewardFactBuilder factBuilder,
        IIdleCombatRewardCalculator calculator,
        IIdleCombatRewardApplier applier,
        IIdleCombatSessionFactory sessionFactory,
        IGameEventOutbox outbox,
        IPublisher publisher,
        ICreatureArchiveService creatureArchiveService)
    {
        _factBuilder = factBuilder;
        _calculator = calculator;
        _applier = applier;
        _sessionFactory = sessionFactory;
        _publisher = publisher;
        _outbox = outbox;
        _creatureArchiveService = creatureArchiveService;
    }

    public CombatMode Mode => CombatMode.Idle;

    public async Task<CombatSession> ApplyAsync(
        CombatOutcomeRequest request,
        CancellationToken cancellationToken)
    {
        var context = CreateContext(request);

        var facts = await _factBuilder.BuildAsync(context, cancellationToken);
        var calculationStartedAt = IdleCombatTelemetry.Start();
        var calculatedOutcome = await _calculator.CalculateAsync(facts, cancellationToken);
        IdleCombatTelemetry.RecordRewardCalculation(calculationStartedAt);

        var progressionStartedAt = IdleCombatTelemetry.Start();
        await _applier.ApplyProgressionAsync(facts, calculatedOutcome, cancellationToken);
        IdleCombatTelemetry.RecordProgressionApply(progressionStartedAt);

        _pendingSettlement.Add(CreateSettlementBatch(facts, calculatedOutcome));
        if (context.OrchestrationRequest.CaptureFinalEncounterLog)
        {
            var settlementStartedAt = IdleCombatTelemetry.Start();
            await FlushSettlementAsync(cancellationToken);
            IdleCombatTelemetry.RecordSettlement(settlementStartedAt);
        }

        return _sessionFactory.Create(facts, calculatedOutcome);
    }

    private async Task FlushSettlementAsync(CancellationToken cancellationToken)
    {
        if (_pendingSettlement.Count == 0)
        {
            return;
        }

        var batches = _pendingSettlement.ToArray();
        await _applier.ApplySettlementAsync(batches, cancellationToken);
        await RecordCreatureArchiveProgressAsync(batches, cancellationToken);
        await PublishProphecyProgressAsync(batches, cancellationToken);
        await EnqueueOutboxProgressAsync(batches, cancellationToken);
        _pendingSettlement.Clear();
    }

    private async Task RecordCreatureArchiveProgressAsync(
        IReadOnlyList<IdleCombatSettlementBatch> batches,
        CancellationToken cancellationToken)
    {
        var defeatBatches = batches
            .Select(batch => new CreatureDefeatBatch(
                batch.DefeatedCreatures,
                batch.ProcessedUntil))
            .Where(batch => batch.Creatures.Count > 0)
            .ToList();

        if (defeatBatches.Count == 0)
        {
            return;
        }

        await _creatureArchiveService.RecordDefeatedCreatureBatchesAsync(
            batches[0].CharacterId,
            defeatBatches,
            cancellationToken);
    }

    private Task EnqueueOutboxProgressAsync(
        IReadOnlyList<IdleCombatSettlementBatch> batches,
        CancellationToken cancellationToken)
    {
        var actionCount = checked(batches.Sum(batch => batch.ActionCount));
        if (actionCount == 0)
        {
            return Task.CompletedTask;
        }

        var defeatedCreatureFamilyKeys = batches
            .SelectMany(batch => batch.DefeatedCreatureFamilyKeys)
            .ToArray();
        var lowestWinningHealthPercent = batches
            .Where(batch => batch.LowestWinningHealthPercent.HasValue)
            .Select(batch => batch.LowestWinningHealthPercent!.Value)
            .DefaultIfEmpty()
            .Min();
        var winningEncounterCount = checked(batches.Sum(batch => batch.WinningEncounterCount));
        var gatheredResourceCount = checked(batches
            .SelectMany(batch => batch.ProphecyProgressEvents)
            .Where(progress => progress.Kind == ProphecyProgressKind.ResourceGathered)
            .Sum(progress => progress.Amount));

        return _outbox.EnqueueAsync(
            GameEventTypes.IdleCombatEncounterCompleted,
            new IdleCombatEncounterCompletedPayload(
                batches[0].CharacterId,
                batches[0].AreaId,
                winningEncounterCount > 0,
                batches.Sum(batch => batch.DefeatedCreatures.Count),
                defeatedCreatureFamilyKeys,
                batches.Sum(batch => batch.PlayerDefeats),
                lowestWinningHealthPercent == 0 ? null : lowestWinningHealthPercent,
                actionCount,
                batches[^1].EquippedGatheringType,
                winningEncounterCount,
                gatheredResourceCount),
            batches[0].CharacterId,
            null,
            cancellationToken);
    }

    private static int? GetLowestWinningHealthPercent(IdleEncounterRewardFacts encounter)
    {
        var lowestHealthPercent = encounter.CombatResult.PlayerTeam
            .Where(x => x.MaxHealth > 0 && x.Health > 0)
            .Select(x => (int)Math.Ceiling((double)x.Health * 100 / x.MaxHealth))
            .DefaultIfEmpty()
            .Min();

        return lowestHealthPercent == 0 ? null : lowestHealthPercent;
    }

    private async Task PublishProphecyProgressAsync(
        IReadOnlyList<IdleCombatSettlementBatch> batches,
        CancellationToken cancellationToken)
    {
        var progressEvents = new List<ProphecyProgressEvent>();

        foreach (var batch in batches)
        {
            progressEvents.AddRange(batch.ProphecyProgressEvents);
        }

        if (progressEvents.Count > 0)
        {
            await _publisher.Publish(new ProphecyProgressBatchNotification(progressEvents), cancellationToken);
        }
    }

    private static IdleCombatSettlementBatch CreateSettlementBatch(
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome)
    {
        var defeatedCreatures = facts.Encounters
            .Where(x => x.IsVictory)
            .SelectMany(x => x.HostileCreatures)
            .ToArray();
        var lowestWinningHealthPercent = facts.Encounters
            .Where(x => x.IsVictory)
            .Select(GetLowestWinningHealthPercent)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .DefaultIfEmpty()
            .Min();
        var prophecyProgressEvents = new List<ProphecyProgressEvent>();
        AddCombatProphecyProgress(prophecyProgressEvents, facts);
        AddRewardProphecyProgress(prophecyProgressEvents, facts, outcome);

        return new IdleCombatSettlementBatch(
            facts.CharacterId,
            facts.From,
            facts.ProcessedUntil,
            facts.Area.Id,
            facts.Area.Name,
            facts.EquippedTool?.GatheringType.ToString(),
            outcome.TotalLoot,
            outcome.TotalCinders,
            outcome.TotalSoulstones,
            defeatedCreatures,
            [.. defeatedCreatures.Select(GetCreatureFamilyKey)],
            facts.Encounters.Count(x => x.Outcome == BattleOutcome.Defeat),
            lowestWinningHealthPercent == 0 ? null : lowestWinningHealthPercent,
            facts.Encounters.Count,
            facts.Encounters.Count(x => x.Outcome == BattleOutcome.Victory),
            prophecyProgressEvents);
    }

    private static void AddCombatProphecyProgress(
        ICollection<ProphecyProgressEvent> progressEvents,
        IdleCombatRewardFacts facts)
    {
        foreach (var encounter in facts.Encounters)
        {
            if (encounter.IsVictory)
            {
                progressEvents.Add(new ProphecyProgressEvent(
                    facts.CharacterId,
                    encounter.StartedAt,
                    ProphecyProgressKind.EncounterWon,
                    EnemyCount: encounter.HostileCreatures.Count));
            }
            else
            {
                progressEvents.Add(new ProphecyProgressEvent(
                    facts.CharacterId,
                    encounter.StartedAt,
                    ProphecyProgressKind.EncounterLost,
                    EnemyCount: encounter.HostileCreatures.Count));
            }

            var defeatedCreatureIds = new List<string>();
            for (var index = 0; index < encounter.HostileCreatures.Count; index++)
            {
                var wasDefeated = encounter.IsVictory ||
                    (index < encounter.CombatResult.EnemyTeam.Count &&
                     encounter.CombatResult.EnemyTeam[index].Health <= 0);

                if (!wasDefeated)
                {
                    continue;
                }

                defeatedCreatureIds.Add(encounter.HostileCreatures[index].Id.ToString());
            }

            foreach (var creatureGroup in defeatedCreatureIds.GroupBy(
                id => id,
                StringComparer.OrdinalIgnoreCase))
            {
                progressEvents.Add(new ProphecyProgressEvent(
                    facts.CharacterId,
                    encounter.StartedAt,
                    ProphecyProgressKind.CreatureDefeated,
                    Amount: creatureGroup.Count(),
                    CreatureDefinitionId: creatureGroup.Key));
            }
        }
    }

    private static void AddRewardProphecyProgress(
        ICollection<ProphecyProgressEvent> progressEvents,
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome)
    {
        foreach (var gathered in outcome.GatheringRewards.Where(x => x.Success))
        {
            var amount = gathered.ItemsGained.Sum(x => x.Quantity);
            if (amount <= 0)
            {
                continue;
            }

            progressEvents.Add(new ProphecyProgressEvent(
                facts.CharacterId,
                outcome.ProcessedUntil,
                ProphecyProgressKind.ResourceGathered,
                amount,
                Profession: gathered.ToolType.ToString()));
        }

        if (outcome.TotalLoot.Count > 0)
        {
            var treasureProgress = outcome.TotalLoot.Sum(item => Math.Max(1, item.Quantity));
            progressEvents.Add(new ProphecyProgressEvent(
                facts.CharacterId,
                outcome.ProcessedUntil,
                ProphecyProgressKind.TreasureProgress,
                treasureProgress));
        }
    }

    private static string GetCreatureFamilyKey(Creature creature)
    {
        var name = creature.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var firstToken = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return firstToken?.Trim('\'', '"', ',', '.', ':', ';') ?? string.Empty;
    }

    private static IdleCombatOutcomeContext CreateContext(CombatOutcomeRequest request)
    {
        if (request.OrchestrationRequest is not IdleCombatOrchestrationRequest idleRequest)
        {
            throw new InvalidOperationException(
                $"Expected {nameof(IdleCombatOrchestrationRequest)} but got {request.OrchestrationRequest.GetType().Name}.");
        }

        if (request.OrchestrationResult.Mode != CombatMode.Idle)
        {
            throw new InvalidOperationException(
                $"Expected orchestration result mode '{CombatMode.Idle}' but got '{request.OrchestrationResult.Mode}'.");
        }

        if (request.OrchestrationResult.Details is not IdleCombatOrchestrationDetails idleDetails)
        {
            throw new InvalidOperationException(
                $"Expected {nameof(IdleCombatOrchestrationDetails)} but got {request.OrchestrationResult.Details?.GetType().Name ?? "null"}.");
        }

        return new IdleCombatOutcomeContext(
            idleRequest,
            request.OrchestrationResult,
            idleDetails);
    }
}
