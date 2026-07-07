using Application.Interfaces.Outbox;
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

    public IdleCombatOutcomeProcessor(
        IIdleCombatRewardFactBuilder factBuilder,
        IIdleCombatRewardCalculator calculator,
        IIdleCombatRewardApplier applier,
        IIdleCombatSessionFactory sessionFactory,
        IGameEventOutbox outbox,
        IPublisher publisher)
    {
        _factBuilder = factBuilder;
        _calculator = calculator;
        _applier = applier;
        _sessionFactory = sessionFactory;
        _publisher = publisher;
        _outbox = outbox;
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
        await PublishProphecyProgressAsync(facts, calculatedOutcome, cancellationToken);
        await EnqueueOutboxProgressAsync(facts, cancellationToken);

        return _sessionFactory.Create(facts, calculatedOutcome);
    }

    private async Task EnqueueOutboxProgressAsync(IdleCombatRewardFacts facts, CancellationToken cancellationToken)
    {
        foreach (var encounter in facts.Encounters)
        {
            var defeatedCreatures = encounter.IsVictory
                ? encounter.HostileCreatures
                : [];
            var lowestWinningHealthPercent = encounter.IsVictory
                ? encounter.CombatResult.PlayerTeam
                    .Where(x => x.MaxHealth > 0 && x.Health > 0)
                    .Select(x => (int)Math.Ceiling((double)x.Health * 100 / x.MaxHealth))
                    .DefaultIfEmpty()
                    .Min()
                : (int?)null;

            await _outbox.EnqueueAsync(
                GameEventTypes.IdleCombatEncounterCompleted,
                new IdleCombatEncounterCompletedPayload(
                    facts.CharacterId,
                    facts.Area.Id,
                    encounter.Outcome == BattleOutcome.Victory,
                    defeatedCreatures.Count,
                    [.. defeatedCreatures.Select(GetCreatureFamilyKey)],
                    encounter.Outcome == BattleOutcome.Defeat ? 1 : 0,
                    lowestWinningHealthPercent == 0 ? null : lowestWinningHealthPercent),
                facts.CharacterId,
                null,
                cancellationToken);
        }
    }

    private async Task PublishProphecyProgressAsync(
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken)
    {
        foreach (var encounter in facts.Encounters)
        {
            if (encounter.IsVictory)
            {
                await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                    facts.CharacterId,
                    encounter.StartedAt,
                    ProphecyProgressKind.EncounterWon,
                    EnemyCount: encounter.HostileCreatures.Count)), cancellationToken);

                foreach (var creature in encounter.HostileCreatures)
                {
                    await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                        facts.CharacterId,
                        encounter.StartedAt,
                        ProphecyProgressKind.CreatureDefeated,
                        CreatureDefinitionId: creature.Id.ToString())), cancellationToken);
                }
            }
            else
            {
                await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                    facts.CharacterId,
                    encounter.StartedAt,
                    ProphecyProgressKind.EncounterLost,
                    EnemyCount: encounter.HostileCreatures.Count)), cancellationToken);
            }
        }

        foreach (var gathered in outcome.GatheringRewards.Where(x => x.Success))
        {
            var amount = gathered.ItemsGained.Sum(x => x.Quantity);
            if (amount <= 0)
            {
                continue;
            }

            await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                facts.CharacterId,
                outcome.ProcessedUntil,
                ProphecyProgressKind.ResourceGathered,
                amount,
                Profession: gathered.ToolType.ToString())), cancellationToken);
        }

        if (outcome.TotalLoot.Count > 0)
        {
            await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                facts.CharacterId,
                outcome.ProcessedUntil,
                ProphecyProgressKind.TreasureProgress,
                outcome.TotalLoot.Count)), cancellationToken);
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
