using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Resolution.Dungeon;

namespace Services.LL.Combat.Layers.Orchestration.Dungeon;

public sealed class DungeonCombatOrchestrator : ICombatOrchestrator
{
    private readonly IDungeonCombatPlanner _planner;
    private readonly IDungeonCombatResolutionSessionFactory _resolutionSessionFactory;
    private readonly IDungeonEncounterParticipantResolver _participantResolver;

    public DungeonCombatOrchestrator(
        IDungeonCombatPlanner planner,
        IDungeonCombatResolutionSessionFactory resolutionSessionFactory,
        IDungeonEncounterParticipantResolver participantResolver)
    {
        _planner = planner;
        _resolutionSessionFactory = resolutionSessionFactory;
        _participantResolver = participantResolver;
    }

    public CombatMode Mode => CombatMode.Dungeon;

    public async Task<CombatOrchestrationResult> OrchestrateAsync(
        CombatOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is not DungeonCombatOrchestrationRequest dungeonRequest)
        {
            throw new ArgumentException(
                $"Expected {nameof(DungeonCombatOrchestrationRequest)} but got {request.GetType().Name}.",
                nameof(request));
        }

        var resolvedParticipants = await _participantResolver.ResolveAsync(
            dungeonRequest.EnemyCreatureKeys,
            cancellationToken);

        var plan = _planner.CreatePlan(
            dungeonRunId: dungeonRequest.DungeonRunId,
            characterId: dungeonRequest.CharacterId,
            characterSnapshot: dungeonRequest.CharacterSnapshot,
            dungeonTier: dungeonRequest.DungeonTier,
            playerEntityIds: [dungeonRequest.CharacterId],
            enemySourceEntityIds: resolvedParticipants,
            runAttributeModifiers: dungeonRequest.RunAttributeModifiers,
            runAbilityModifiers: dungeonRequest.RunAbilityModifiers,
            enemyAttributeModifiers: dungeonRequest.EnemyAttributeModifiers,
            enemyStrengthMultiplier: dungeonRequest.EnemyStrengthMultiplier);

        var resolutionSession = await _resolutionSessionFactory.CreateAsync(
            plan,
            cancellationToken);

        var encounterPlan = _planner.CreateEncounterPlan(plan, 1, DateTimeOffset.UtcNow);
        var resolution = await resolutionSession.ResolveAsync(encounterPlan, cancellationToken);

        var record = new CombatEncounterRecord(encounterPlan, resolution);

        return new CombatOrchestrationResult(
            SessionId: Guid.NewGuid(),
            Mode: Mode,
            Encounters: [record],
            Details: new DungeonCombatOrchestrationDetails(
                DungeonRunId: plan.DungeonRunId,
                ProgressionStatus: DungeonProgressionStatus.Active),
            SourceEntitiesById: resolutionSession.SourceEntitiesById);
    }
}
