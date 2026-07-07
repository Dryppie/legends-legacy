using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Tutorials;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Tutorials;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Tutorials;

public sealed class TutorialBattleService : ITutorialBattleService
{
    private static readonly TimeSpan TrainingEncounterCadence = TimeSpan.FromSeconds(10);

    private readonly IEntityService _entityService;
    private readonly IAreaService _areaService;
    private readonly ICombatSetupService _combatSetupService;
    private readonly ICombatEngineExecutor _combatEngineExecutor;
    private readonly ICombatEncounterResultFactory _combatEncounterResultFactory;
    private readonly ITutorialService _tutorialService;
    private readonly ITutorialProgressionService _progressionService;

    public TutorialBattleService(
        IEntityService entityService,
        IAreaService areaService,
        ICombatSetupService combatSetupService,
        ICombatEngineExecutor combatEngineExecutor,
        ICombatEncounterResultFactory combatEncounterResultFactory,
        ITutorialService tutorialService,
        ITutorialProgressionService progressionService)
    {
        _entityService = entityService;
        _areaService = areaService;
        _combatSetupService = combatSetupService;
        _combatEngineExecutor = combatEngineExecutor;
        _combatEncounterResultFactory = combatEncounterResultFactory;
        _tutorialService = tutorialService;
        _progressionService = progressionService;
    }

    public async Task<CombatResult?> StartTrainingBattleAsync(Guid characterId, CancellationToken cancellationToken)
    {
        if (!await _tutorialService.CanStartCombatAreaAsync(
                characterId,
                TutorialConstants.TrainingGroundsAreaId,
                cancellationToken))
        {
            return null;
        }

        var area = await _areaService.GetAreaByIdAsync(TutorialConstants.TrainingGroundsAreaId);
        if (area is null)
        {
            return null;
        }

        var trainingCreatureId = area.Creatures
            .OrderByDescending(creature => creature.WeightedSpawnRate)
            .Select(creature => creature.CreatureId)
            .FirstOrDefault();

        if (trainingCreatureId == Guid.Empty)
        {
            return null;
        }

        var playerTeam = await _entityService.GetEntitiesByIdsForCombatAsync([characterId], cancellationToken);
        var enemyTeam = await _entityService.GetEntitiesByIdsForCombatAsync([trainingCreatureId], cancellationToken);
        if (playerTeam.Count == 0 || enemyTeam.Count == 0 || enemyTeam[0] is not Creature)
        {
            return null;
        }

        var combatPlayerEntities = _combatSetupService.CreatePlayerCombatEntities(playerTeam);
        var combatEnemyEntities = _combatSetupService.CreateCreatureCombatEntities(enemyTeam, area);
        await _combatSetupService.PrepareEntitiesForCombat([.. combatPlayerEntities, .. combatEnemyEntities]);

        var now = DateTimeOffset.UtcNow;
        var encounterPlan = CreateTrainingEncounterPlan(characterId, trainingCreatureId, area, now);
        var runtime = new CombatEncounterRuntime(
            encounterPlan,
            [
                new CombatRuntimeParticipant(
                    encounterPlan.FriendlyParticipants.Single(),
                    playerTeam.Single(),
                    combatPlayerEntities.Single())
            ],
            [
                new CombatRuntimeParticipant(
                    encounterPlan.HostileParticipants.Single(),
                    enemyTeam.Single(),
                    combatEnemyEntities.Single())
            ]);

        var combatResult = await _combatEngineExecutor.ExecuteAsync(runtime, cancellationToken);
        combatResult = _combatEncounterResultFactory.Create(runtime, combatResult).CombatResult;

        var tutorialProgress = await _progressionService.TryProgressAsync(
            characterId,
            TutorialTrigger.IdleCombatCompleted(
                TutorialConstants.TrainingGroundsAreaId,
                combatResult.Outcome == BattleOutcome.Victory),
            cancellationToken);
        combatResult.Loot.AddRange(tutorialProgress?.Loot ?? []);

        return combatResult;
    }

    private static CombatEncounterPlan CreateTrainingEncounterPlan(
        Guid characterId,
        Guid trainingCreatureId,
        Domain.Models.Regions.Areas.Area area,
        DateTimeOffset startsAt)
    {
        var encounterId = Guid.NewGuid();
        return new CombatEncounterPlan(
            EncounterId: encounterId,
            Mode: CombatMode.Idle,
            Sequence: 1,
            StartsAt: startsAt,
            Participants:
            [
                new CombatParticipantSlot(characterId.ToString(), characterId, CombatSide.Friendly),
                new CombatParticipantSlot(trainingCreatureId.ToString(), trainingCreatureId, CombatSide.Hostile)
            ],
            SourceContext: new IdleEncounterSourceContext(characterId, area, TrainingEncounterCadence));
    }
}
