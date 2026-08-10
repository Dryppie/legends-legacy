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
        var calculatedOutcome = await _calculator.CalculateAsync(facts, cancellationToken);
        await _applier.ApplyAsync(facts, calculatedOutcome, cancellationToken);
        await RecordCreatureArchiveProgressAsync(facts, cancellationToken);
        await PublishProphecyProgressAsync(facts, calculatedOutcome, cancellationToken);
        await EnqueueOutboxProgressAsync(facts, cancellationToken);

        return _sessionFactory.Create(facts, calculatedOutcome);
    }

    private async Task RecordCreatureArchiveProgressAsync(
        IdleCombatRewardFacts facts,
        CancellationToken cancellationToken)
    {
        var defeatedCreatures = facts.Encounters
            .Where(x => x.IsVictory)
            .SelectMany(x => x.HostileCreatures)
            .ToList();

        if (defeatedCreatures.Count == 0)
        {
            return;
        }

        await _creatureArchiveService.RecordDefeatedCreaturesAsync(
            facts.CharacterId,
            defeatedCreatures,
            facts.ProcessedUntil,
            cancellationToken);
    }

    private Task EnqueueOutboxProgressAsync(IdleCombatRewardFacts facts, CancellationToken cancellationToken)
    {
        if (facts.Encounters.Count == 0)
        {
            return Task.CompletedTask;
        }

        var defeatedCreatures = facts.Encounters
            .Where(x => x.IsVictory)
            .SelectMany(x => x.HostileCreatures)
            .ToList();

        var lowestWinningHealthPercent = facts.Encounters
            .Where(x => x.IsVictory)
            .Select(GetLowestWinningHealthPercent)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .DefaultIfEmpty()
            .Min();

        return _outbox.EnqueueAsync(
            GameEventTypes.IdleCombatEncounterCompleted,
            new IdleCombatEncounterCompletedPayload(
                facts.CharacterId,
                facts.Area.Id,
                facts.Encounters.Any(x => x.Outcome == BattleOutcome.Victory),
                defeatedCreatures.Count,
                [.. defeatedCreatures.Select(GetCreatureFamilyKey)],
                facts.Encounters.Count(x => x.Outcome == BattleOutcome.Defeat),
                lowestWinningHealthPercent == 0 ? null : lowestWinningHealthPercent,
                facts.Encounters.Count,
                facts.EquippedTool?.GatheringType.ToString(),
                facts.Encounters.Count(x => x.Outcome == BattleOutcome.Victory)),
            facts.CharacterId,
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
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken)
    {
        var progressEvents = new List<ProphecyProgressEvent>();

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

            for (var index = 0; index < encounter.HostileCreatures.Count; index++)
            {
                var wasDefeated = encounter.IsVictory ||
                    (index < encounter.CombatResult.EnemyTeam.Count &&
                     encounter.CombatResult.EnemyTeam[index].Health <= 0);

                if (!wasDefeated)
                {
                    continue;
                }

                progressEvents.Add(new ProphecyProgressEvent(
                    facts.CharacterId,
                    encounter.StartedAt,
                    ProphecyProgressKind.CreatureDefeated,
                    CreatureDefinitionId: encounter.HostileCreatures[index].Id.ToString()));
            }
        }

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

        if (progressEvents.Count > 0)
        {
            await _publisher.Publish(new ProphecyProgressBatchNotification(progressEvents), cancellationToken);
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
