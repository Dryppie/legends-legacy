using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Prophecies;
using Application.UseCases.Prophecies.Events;
using Domain.Models.CharacterActions.Sessions;
using MediatR;
using Services.LL.Combat.Layers.Orchestration.Dungeon;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

internal class DungeonCombatOutcomeProcessor : ICombatOutcomeProcessor
{
    private readonly IDungeonCombatRewardFactBuilder _factBuilder;
    private readonly IDungeonCombatRewardCalculator _calculator;
    private readonly IDungeonCombatRewardApplier _applier;
    private readonly IDungeonCombatSessionFactory _sessionFactory;
    private readonly IPublisher _publisher;
    private readonly ICreatureArchiveService _creatureArchiveService;

    public DungeonCombatOutcomeProcessor(
        IDungeonCombatRewardFactBuilder factBuilder,
        IDungeonCombatRewardCalculator calculator,
        IDungeonCombatRewardApplier applier,
        IDungeonCombatSessionFactory sessionFactory,
        IPublisher publisher,
        ICreatureArchiveService creatureArchiveService)
    {
        _factBuilder = factBuilder;
        _calculator = calculator;
        _applier = applier;
        _sessionFactory = sessionFactory;
        _publisher = publisher;
        _creatureArchiveService = creatureArchiveService;
    }

    public CombatMode Mode => CombatMode.Dungeon;

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

        return _sessionFactory.Create(facts, calculatedOutcome);
    }

    private async Task RecordCreatureArchiveProgressAsync(
        DungeonCombatRewardFacts facts,
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
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    private async Task PublishProphecyProgressAsync(
        DungeonCombatRewardFacts facts,
        DungeonCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var encounter in facts.Encounters)
        {
            if (encounter.IsVictory)
            {
                await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                    facts.CharacterId,
                    now,
                    ProphecyProgressKind.EncounterWon,
                    EnemyCount: encounter.HostileCreatures.Count)), cancellationToken);

                foreach (var creature in encounter.HostileCreatures)
                {
                    await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                        facts.CharacterId,
                        now,
                        ProphecyProgressKind.CreatureDefeated,
                        CreatureDefinitionId: creature.Id.ToString())), cancellationToken);
                }
            }
            else
            {
                await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                    facts.CharacterId,
                    now,
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
                now,
                ProphecyProgressKind.ResourceGathered,
                amount,
                Profession: gathered.ToolType.ToString())), cancellationToken);
        }

        if (outcome.TotalLoot.Count > 0)
        {
            await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                facts.CharacterId,
                now,
                ProphecyProgressKind.TreasureProgress,
                outcome.TotalLoot.Count)), cancellationToken);
        }
    }

    private static DungeonCombatOutcomeContext CreateContext(CombatOutcomeRequest request)
    {
        if (request.OrchestrationRequest is not DungeonCombatOrchestrationRequest dungeonRequest)
        {
            throw new InvalidOperationException(
                $"Expected {nameof(DungeonCombatOrchestrationRequest)} but got {request.OrchestrationRequest.GetType().Name}.");
        }

        if (request.OrchestrationResult.Mode != CombatMode.Dungeon)
        {
            throw new InvalidOperationException(
                $"Expected orchestration result mode '{CombatMode.Dungeon}' but got '{request.OrchestrationResult.Mode}'.");
        }

        if (request.OrchestrationResult.Details is not DungeonCombatOrchestrationDetails dungeonDetails)
        {
            throw new InvalidOperationException(
                $"Expected {nameof(DungeonCombatOrchestrationDetails)} but got {request.OrchestrationResult.Details?.GetType().Name ?? "null"}.");
        }

        return new DungeonCombatOutcomeContext(
            dungeonRequest,
            request.OrchestrationResult,
            dungeonDetails);
    }
}
