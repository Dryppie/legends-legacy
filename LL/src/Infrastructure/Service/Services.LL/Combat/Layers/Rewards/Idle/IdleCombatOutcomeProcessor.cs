using Domain.Models.CharacterActions.Sessions;
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

    public IdleCombatOutcomeProcessor(
        IIdleCombatRewardFactBuilder factBuilder,
        IIdleCombatRewardCalculator calculator,
        IIdleCombatRewardApplier applier,
        IIdleCombatSessionFactory sessionFactory)
    {
        _factBuilder = factBuilder;
        _calculator = calculator;
        _applier = applier;
        _sessionFactory = sessionFactory;
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

        return _sessionFactory.Create(facts, calculatedOutcome);
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