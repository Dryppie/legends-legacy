using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Quests;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;
using Domain.Models.Quests;
using Domain.Models.Essences;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Quests;

public sealed class QuestEncounterService(
    IEntityService entityService,
    IAreaService areaService,
    ICombatSetupService combatSetupService,
    ICombatEngineExecutor combatEngineExecutor,
    ICombatEncounterResultFactory combatEncounterResultFactory,
    ICombatAreaAccessService accessService,
    IQuestRepository questRepository,
    IQuestDefinitionProvider questDefinitions,
    IQuestProgressionService progressionService) : IQuestEncounterService
{
    private static readonly TimeSpan TrainingEncounterCadence = TimeSpan.FromSeconds(10);
    private const int TrainingEnemyMaxHealth = 10;

    public async Task<CombatResult?> StartAsync(
        Guid characterId,
        string questId,
        string encounterKey,
        CancellationToken cancellationToken)
    {
        if (!questId.Equals(QuestConstants.TrainingDay, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var questProgress = await questRepository.GetProgressAsync(
            characterId,
            questId,
            cancellationToken);
        if (questProgress is null || questProgress.Status != QuestStatus.Active)
        {
            return null;
        }

        var questDefinition = questDefinitions.Get(
            questProgress.QuestId,
            questProgress.DefinitionVersion);
        QuestChoiceOptionDefinition? selectedOption = null;
        if (questDefinition.Choice is not null)
        {
            selectedOption = questDefinition.Choice.Options.FirstOrDefault(option =>
                option.Key.Equals(questProgress.SelectedOptionKey, StringComparison.OrdinalIgnoreCase));
            if (selectedOption is null ||
                !selectedOption.EncounterKey.Equals(encounterKey, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }
        else if (!encounterKey.Equals("training", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var access = await accessService.GetAccessAsync(
            characterId,
            QuestConstants.TrainingGroundsAreaId,
            cancellationToken);
        if (!access.CanAccess)
        {
            return null;
        }

        var area = await areaService.GetAreaByIdAsync(QuestConstants.TrainingGroundsAreaId);
        if (area is null)
        {
            return null;
        }

        var creatureId = selectedOption?.CreatureId ?? area.Creatures
            .OrderByDescending(x => x.WeightedSpawnRate)
            .Select(x => x.CreatureId)
            .FirstOrDefault();
        if (creatureId == Guid.Empty)
        {
            return null;
        }

        var playerTeam = await entityService.GetEntitiesByIdsForCombatAsync([characterId], cancellationToken);
        var enemyTeam = await entityService.GetEntitiesByIdsForCombatAsync([creatureId], cancellationToken);
        if (playerTeam.Count == 0 || enemyTeam.Count == 0 || enemyTeam[0] is not Creature)
        {
            return null;
        }

        var combatPlayers = combatSetupService.CreatePlayerCombatEntities(playerTeam);
        var combatEnemies = combatSetupService.CreateCreatureCombatEntities(enemyTeam, area);
        await combatSetupService.PrepareEntitiesForCombat(
            [.. combatPlayers, .. combatEnemies],
            EssenceCombatActivity.IdleCombat);
        SetTrainingEnemyHealth(combatEnemies.Single());

        var startsAt = DateTimeOffset.UtcNow;
        var encounterId = Guid.NewGuid();
        var plan = new CombatEncounterPlan(
            EncounterId: encounterId,
            Mode: CombatMode.Idle,
            Sequence: 1,
            StartsAt: startsAt,
            Participants:
            [
                new CombatParticipantSlot(characterId.ToString(), characterId, CombatSide.Friendly),
                new CombatParticipantSlot(creatureId.ToString(), creatureId, CombatSide.Hostile)
            ],
            SourceContext: new IdleEncounterSourceContext(
                characterId,
                area,
                TrainingEncounterCadence));
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), playerTeam.Single(), combatPlayers.Single())],
            [new CombatRuntimeParticipant(plan.HostileParticipants.Single(), enemyTeam.Single(), combatEnemies.Single())]);

        var result = await combatEngineExecutor.ExecuteAsync(runtime, cancellationToken);
        result = combatEncounterResultFactory.Create(runtime, result).CombatResult;
        var progression = await progressionService.ProcessAsync(
            characterId,
            QuestTrigger.CombatCompleted(
                QuestConstants.TrainingGroundsAreaId,
                result.Outcome == BattleOutcome.Victory),
            null,
            "quest.training_encounter_completed",
            cancellationToken);
        result.Loot.AddRange(progression.Loot);
        return result;
    }

    private static void SetTrainingEnemyHealth(CombatEntity enemy)
    {
        enemy.BaseCombatAttributes[AttributeType.MaxHealth] = TrainingEnemyMaxHealth;
        enemy.CombatAttributes[AttributeType.MaxHealth] = TrainingEnemyMaxHealth;
        enemy.SetCurrentHealth(TrainingEnemyMaxHealth);
    }
}
