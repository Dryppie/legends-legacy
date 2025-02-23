using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Events;
using Common.Helpers;
using Domain.Components.Attributes;
using Domain.Extensions;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Regions.Areas;
using MediatR;
using Services.LL.Combat;

namespace Services.LL.CharacterActions;
public class CombatService : ICombatService
{
    private readonly IEntityService _entityService;
    private readonly ILootService _lootService;
    private readonly ISpawningService _spawningService;
    private readonly IPublisher _publisher;

    public CombatService(IEntityService entityService, ILootService lootService, ISpawningService spawningService, IPublisher publisher)
    {
        _entityService = entityService;
        _lootService = lootService;
        _spawningService = spawningService;
        _publisher = publisher;
    }

    public async Task<CombatResult> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var totalLoot = new List<InventoryItem>();
        var totalExp = 0;

        var combatAction = characterAction.ActionDetails as CombatActionDetails;
        var monsterCount = _spawningService.HowManyMonstersToSpawn(combatAction!.Area.SpawnProbabilities);
        var areaCreatures = _spawningService.WhatAreaCreaturesToSpawn([.. combatAction.Area.Creatures], monsterCount);
        var enemyIds = areaCreatures.Select(c => c.CreatureId).ToList();

        // Initialize combatants
        var playerCharacters = await GetPlayerCharactersAsync([.. combatAction.CharacterTeam], cancellationToken);
        // TODO: Instead of getting a random selection here, it should be done in the while loop
        // otherwise it'll be the same selection for 12 hours of idle, instead of random mobs each combat
        var enemyCharacters = await GetEnemyCharactersAsync(enemyIds, cancellationToken);

        var combatPlayerEntities = CreateCombatEntities(playerCharacters);
        var combatEnemyEntities = CreateCombatEntities(enemyCharacters);

        // Prepare entities for combat
        await PrepareEntitiesForCombat([.. combatPlayerEntities, .. combatEnemyEntities]);

        var lastCombatResult = new CombatResult();

        while (characterAction.UpdatedAt < now)
        {

            var combatSimulation = new CombatSimulation(combatPlayerEntities, combatEnemyEntities);
            lastCombatResult = combatSimulation.RunSimulation();

            // StartedAt is 1 second after the action is initialized, so as to have a 'combat starting' screen
            lastCombatResult.StartedAt = characterAction.UpdatedAt.AddSeconds(1);

            // Update the UpdatedAt timestamp based on combat duration
            // And add 2 seconds to have a delay of one second before and after the fight
            // To display the victory/defeat screen before a new fight is initialized
            characterAction.UpdatedAt += TimeSpan.FromSeconds((lastCombatResult.Duration * 0.1) + 2);


            // Accumulate loot
            //totalLoot.AddRange(lastCombatResult.Loot);

            //combatsPerformed++;

            // Reset entities when combat is over
            // Also process loot, since it's fight that should have already happened
            // If it's a fight where the frontend has yet to display the outcome, the loot should first be processed when the fight is 'over'
            
            ResetEntitiesForCombat([.. combatPlayerEntities, .. combatEnemyEntities]);
            
            if (lastCombatResult.Outcome.Equals(BattleOutcome.Victory))
            {
                lastCombatResult.Loot = _lootService.GenerateIdleCombatLootAsync(enemyCharacters);
                totalLoot.AddRange(lastCombatResult.Loot);

                totalExp += enemyCharacters.OfType<Creature>().Sum(e => e.ExperienceReward);
            }

            //TODO: OPTIMIZE PERHAPS?
            // https://chatgpt.com/c/671943b1-0958-800d-9234-32c45632490e
        }

        lastCombatResult.Loot = _lootService.GenerateIdleCombatLootAsync(enemyCharacters);

        // Create CombatEntities to keep track of simple data over each entity, such as id, health, mana
        lastCombatResult.PlayerTeam = CreateSimpleCombatEntities(combatPlayerEntities);
        lastCombatResult.EnemyTeam = CreateSimpleCombatEntities(combatEnemyEntities);
        lastCombatResult.ExperienceGained = totalExp;

        await UpdateCharacterStatsAsync(playerCharacters, totalExp, cancellationToken);
        await ProcessLootAsync(characterAction.CharacterId, totalLoot, cancellationToken);

        return lastCombatResult;
    }
    private static List<Guid> SelectRandom(List<Guid> enemyTeam)
    {
        int randomCount = GetWeightedRandom();
        return enemyTeam.OrderBy(x => Guid.NewGuid()).Take(randomCount).ToList();
    }

    private static int GetWeightedRandom()
    {
        Random rand = new();
        int randomValue = rand.Next(0, 100);

        if (randomValue < 25) // 25% chance
            return 1;
        else if (randomValue < 80) // 55% chance
            return 2;
        else if (randomValue < 95) // 15% chance
            return 3;
        else // 5% chance
            return 4;
    }

    private static List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> playerCharacters)
    {
        var combatEntities = new List<SimpleCombatEntity>();
        foreach (var entity in playerCharacters)
        {
            combatEntities.Add(new SimpleCombatEntity(entity.Id, entity.Name, (int)entity.BaseCombatAttributes[AttributeType.MaxHealth], (int)entity.BaseCombatAttributes[AttributeType.MaxMana], (int)entity.BaseCombatAttributes[AttributeType.Barrier]));
        }

        return combatEntities;
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
        var increment = 1;
        foreach (var entity in entities)
        {
            var combatEntity = new CombatEntity(entity);
            combatEntity.Id = $"{entity.Id}_{increment}";
            combatEntities.Add(combatEntity);
            increment++;
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
        LoadAbilitiesFromEssences(entities);

        // Load abilities
        var loadedAttributeTasks = entities.Select(entity => Task.Run(() => EssenceLoader.Instance.LoadEssencesForCombatEntity(entity)));

        // Calculate attributes
        var calculationTasks = entities.Select(entity => Task.Run(() => AttributeCalculator.CalculateBaseCombatAttributes(entity)));

        await Task.WhenAll(loadedAttributeTasks);
        await Task.WhenAll(calculationTasks);
    }

    private static void LoadAbilitiesFromEssences(IEnumerable<CombatEntity> entities)
    {
        foreach (var entity in entities)
        {
            foreach (var essence in entity.EquippedEssences)
            {
                entity.AbilityIds.Add(essence.ActiveAbilityId);
                entity.AbilityIds.Add(essence.PassiveAbilityId);
            }
        }
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
            character.UpdateCharacterLevel();
        }
        await _entityService.UpdateEntities(playerCharacters, cancellationToken);
    }
}