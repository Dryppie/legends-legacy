using Domain.Models.CharacterActions.Sessions;
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

    public DungeonCombatOutcomeProcessor(
        IDungeonCombatRewardFactBuilder factBuilder,
        IDungeonCombatRewardCalculator calculator,
        IDungeonCombatRewardApplier applier,
        IDungeonCombatSessionFactory sessionFactory)
    {
        _factBuilder = factBuilder;
        _calculator = calculator;
        _applier = applier;
        _sessionFactory = sessionFactory;
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

        return _sessionFactory.Create(facts, calculatedOutcome);
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