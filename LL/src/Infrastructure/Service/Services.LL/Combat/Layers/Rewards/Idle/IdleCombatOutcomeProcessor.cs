using Application.Interfaces.Services.LL.Achievements;
using Domain.Models.CharacterActions.Sessions;
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
    private readonly IAchievementService _achievementService;

    public IdleCombatOutcomeProcessor(
        IIdleCombatRewardFactBuilder factBuilder,
        IIdleCombatRewardCalculator calculator,
        IIdleCombatRewardApplier applier,
        IIdleCombatSessionFactory sessionFactory,
        IAchievementService achievementService)
    {
        _factBuilder = factBuilder;
        _calculator = calculator;
        _applier = applier;
        _sessionFactory = sessionFactory;
        _achievementService = achievementService;
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
        await RecordAchievementsAsync(facts, cancellationToken);

        return _sessionFactory.Create(facts, calculatedOutcome);
    }

    private async Task RecordAchievementsAsync(IdleCombatRewardFacts facts, CancellationToken cancellationToken)
    {
        var winningEncounters = facts.Encounters
            .Where(x => x.Outcome == BattleOutcome.Victory)
            .ToList();

        var defeatedCreatures = winningEncounters
            .SelectMany(x => x.HostileCreatures)
            .ToList();

        var lowestWinningHealthPercent = winningEncounters
            .SelectMany(x => x.CombatResult.PlayerTeam)
            .Where(x => x.MaxHealth > 0 && x.Health > 0)
            .Select(x => (int)Math.Ceiling((double)x.Health * 100 / x.MaxHealth))
            .DefaultIfEmpty()
            .Min();

        await _achievementService.RecordIdleCombatAsync(
            facts.CharacterId,
            defeatedCreatures.Count,
            [.. defeatedCreatures.Select(GetCreatureFamilyKey)],
            facts.Encounters.Count(x => x.Outcome == BattleOutcome.Defeat),
            lowestWinningHealthPercent == 0 ? null : lowestWinningHealthPercent,
            cancellationToken);
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
