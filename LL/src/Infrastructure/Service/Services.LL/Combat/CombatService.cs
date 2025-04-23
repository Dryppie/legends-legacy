using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Events;
using Common.Helpers.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using MediatR;
using Services.LL.CharacterActions;
using Services.LL.Interfaces;

namespace Services.LL.Combat;
public class CombatService : ICombatService
{
    private readonly IEntityService _entityService;
    private readonly ILevelingService _levelingService;
    private readonly ILootService _lootService;
    private readonly ISpawningService _spawningService;
    private readonly IPublisher _publisher;

    public CombatService(IEntityService entityService, ILevelingService levelingService, ILootService lootService, ISpawningService spawningService, IPublisher publisher)
    {
        _entityService = entityService;
        _levelingService = levelingService;
        _lootService = lootService;
        _spawningService = spawningService;
        _publisher = publisher;
    }

    public async Task<CombatSession> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var totalLoot = new List<InventoryItem>();
        var totalExp = 0;

        var sessionStartedAt = characterAction.UpdatedAt;

        var combatAction = characterAction.ActionDetails as CombatActionDetails;

        // Initialize combatants
        var playerCharacters = await GetPlayerCharactersAsync([.. combatAction.CharacterTeam], cancellationToken);

        var allEnemyCharacters = await GetEnemyCharactersAsync(combatAction.Area.Creatures.Select(c => c.CreatureId).ToList(), cancellationToken);

        var combatPlayerEntities = CreateCombatEntities(playerCharacters);
        var allCombatEnemyEntities = CreateCombatEntities(allEnemyCharacters);

        // Prepare entities for combat
        await PrepareEntitiesForCombat([.. combatPlayerEntities, .. allCombatEnemyEntities]);

        var lastCombatResult = new CombatResult();
        var combatSummary = new CombatSummary();
        var selectedCombatEnemyEntities = new List<CombatEntity>();

        while (characterAction.UpdatedAt < now)
        {
            // Initialize both teams
            var monsterCount = _spawningService.HowManyMonstersToSpawn(combatAction!.Area.SpawnProbabilities);
            var selectedAreaCreatures = _spawningService.WhatAreaCreaturesToSpawn([.. combatAction.Area.Creatures], monsterCount);
            var selectedEnemyIds = selectedAreaCreatures.Select(c => c.CreatureId).ToList();

            selectedCombatEnemyEntities = [.. selectedEnemyIds.Select(id => allCombatEnemyEntities.First(ee => ee.OriginalId.Equals(id)).Copy())];
            AppendPrefixToId(selectedCombatEnemyEntities);
            var combatSimulation = new CombatSimulation(combatPlayerEntities, selectedCombatEnemyEntities);
            lastCombatResult = combatSimulation.RunSimulation();

            // StartedAt is 1 second after the action is initialized, so as to have a 'combat starting' screen
            lastCombatResult.StartedAt = characterAction.UpdatedAt.AddSeconds(1);

            // Update the UpdatedAt timestamp based on combat duration
            // And add 2 seconds to have a delay of one second before and after the fight
            // To display the victory/defeat screen before a new fight is initialized
            characterAction.UpdatedAt += TimeSpan.FromSeconds(lastCombatResult.Duration * 0.1 + 2);


            // Accumulate loot
            //totalLoot.AddRange(lastCombatResult.Loot);

            //combatsPerformed++;

            // Reset entities when combat is over
            // Also process loot, since it's fight that should have already happened
            // If it's a fight where the frontend has yet to display the outcome, the loot should first be processed when the fight is 'over'

            ResetEntitiesForCombat([.. combatPlayerEntities, .. selectedCombatEnemyEntities]);

            if (lastCombatResult.Outcome.Equals(BattleOutcome.Victory))
            {
                var selectedEnemyEntities = new List<Entity>();
                selectedEnemyIds.ForEach(id => selectedEnemyEntities.Add(allEnemyCharacters.First(ee => ee.Id.Equals(id))));

                var lootThisBattle = _lootService.GenerateIdleCombatLootAsync(selectedEnemyEntities);
                lastCombatResult.Loot = lootThisBattle;

                // Accumulate total loot
                totalLoot.AddRange(lootThisBattle);

                lastCombatResult.ExperienceGained = selectedEnemyEntities.OfType<Creature>().Sum(e => e.ExperienceReward);
            }
            AddToCombatSummary(combatSummary, lastCombatResult);

            //TODO: OPTIMIZE PERHAPS?
            // https://chatgpt.com/c/671943b1-0958-800d-9234-32c45632490e
        }

        // Create CombatEntities to keep track of simple data over each entity, such as id, health, mana
        lastCombatResult.PlayerTeam = CreateSimpleCombatEntities(combatPlayerEntities);
        lastCombatResult.EnemyTeam = CreateSimpleCombatEntities(selectedCombatEnemyEntities);

        var combatSession = new CombatSession()
        {
            From = sessionStartedAt,
            To = now,
            CombatResult = lastCombatResult,
            CombatSummary = combatSummary,
        };

        await UpdateCharacterStatsAsync(playerCharacters, combatSession.CombatSummary.TotalExperience, cancellationToken);
        await ProcessLootAsync(characterAction.CharacterId, totalLoot, cancellationToken);

        return combatSession;
    }

    private static void AddToCombatSummary(CombatSummary combatSummary, CombatResult lastCombatResult)
    {
        combatSummary.TotalBattles++;

        if (lastCombatResult.Outcome.Equals(BattleOutcome.Victory)) combatSummary.Wins++;
        else if (lastCombatResult.Outcome.Equals(BattleOutcome.Defeat)) combatSummary.Losses--;
        else combatSummary.Draws++;

        combatSummary.TotalExperience += lastCombatResult.ExperienceGained;
    }

    private static List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> playerCharacters)
    {
        var combatEntities = new List<SimpleCombatEntity>();
        foreach (var entity in playerCharacters)
        {
            combatEntities.Add(new SimpleCombatEntity(entity.Id, entity.Name, entity.ImagePath, (int)entity.BaseCombatAttributes[AttributeType.MaxHealth], (int)entity.BaseCombatAttributes[AttributeType.MaxMana], (int)entity.BaseCombatAttributes[AttributeType.Barrier]));
        }

        return combatEntities;
    }
    private static void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities)
    {
        var groupedEntities = selectedCombatEnemyEntities
            .GroupBy(e => e.Id);

        foreach (var group in groupedEntities)
        {
            int increment = 1;
            foreach (var entity in group)
            {
                entity.Id = $"{entity.Id}_{increment}";
                increment++;
            }
        }
    }


    private async Task<List<Entity>> GetPlayerCharactersAsync(List<Guid> characterTeam, CancellationToken cancellationToken)
    {
        return await _entityService.GetEntitiesByIdsForCombatAsync(characterTeam, cancellationToken);
    }

    private async Task<List<Entity>> GetEnemyCharactersAsync(List<Guid> enemyTeam, CancellationToken cancellationToken)
    {
        return await _entityService.GetEntitiesByIdsForCombatAsync(enemyTeam, cancellationToken);
    }

    private static List<CombatEntity> CreateCombatEntities(List<Entity> entities)
    {
        var combatEntities = new List<CombatEntity>();
        foreach (var entity in entities)
        {
            var combatEntity = new CombatEntity(entity);
            combatEntities.Add(combatEntity);
        }
        return combatEntities;
    }

    private static void ResetEntitiesForCombat(List<CombatEntity> allEntities)
    {
        foreach (var entity in allEntities)
        {
            entity.Reset();
        }
    }

    private static async Task PrepareEntitiesForCombat(IEnumerable<CombatEntity> entities)
    {
        // Load abilities
        var loadedAttributeTasks = entities.Select(entity => Task.Run(() => EssenceLoader.Instance.LoadEssencesForCombatEntity(entity)));

        // Calculate attributes
        var calculationTasks = entities.Select(entity => Task.Run(() => AttributeCalculator.CalculateBaseCombatAttributes(entity)));

        await Task.WhenAll(loadedAttributeTasks);
        await Task.WhenAll(calculationTasks);
    }

    private async Task ProcessLootAsync(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
    {
        if (loot.Count == 0) return;
        // Implement how to update the character or game state with the loot
        // For example, updating the character inventory
        //await _InventoryService.AddLootAsync(loot, cancellationToken);
        await _publisher.Publish(new LootGeneratedEvent(characterId, loot), cancellationToken);
    }

    private async Task UpdateCharacterStatsAsync(List<Entity> playerCharacters, int totalExp, CancellationToken cancellationToken)
    {
        if (totalExp == 0) return;

        var characters = playerCharacters.OfType<Character>();
        foreach (var character in characters)
        {
            character.Experience += totalExp / characters.Count();
            var wepAndShield = character.EquipmentSlots.Where(eq => (eq.EquipmentType == EquipmentType.MainHand || eq.EquipmentType == EquipmentType.OffHand) && eq.EquipmentInstance != null).ToList();
            foreach (var mastery in character.Masteries)
            {
                if (wepAndShield != null && wepAndShield.Select(we => (we.EquipmentInstance.ItemBase as EquipmentBase).CombatMastery).ToList().Contains(mastery.MasteryType))
                {
                    mastery.CurrentXP++;
                    if (mastery.CurrentXP >= mastery.XPThresholdForNextLevel)
                    {
                        mastery.CurrentXP -= mastery.XPThresholdForNextLevel;
                        mastery.Level++;
                    }
                }
            }
            await _levelingService.UpdateCharacterLevel(character);
        }
        await _entityService.UpdateEntities(playerCharacters, cancellationToken);
    }
}