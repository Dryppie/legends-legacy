using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Events;
using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Inventories;
using MediatR;
using Services.LL.Combat;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class CombatService : ICombatService
{
    private readonly IEntityService _entityService;
    private readonly ILootService _lootService;
    private readonly IPublisher _publisher;

    public CombatService(IEntityService entityService, ILootService lootService, IPublisher publisher)
    {
        _entityService = entityService;
        _lootService = lootService;
        _publisher = publisher;
    }

    public async Task<CombatResult> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var totalLoot = new List<InventoryItem>();

        var combatAction = characterAction.ActionDetails as CombatActionDetails;

        // Initialize combatants
        var playerCharacters = await GetPlayerCharactersAsync(combatAction!.CharacterTeam.ToList(), cancellationToken);
        // TODO: Instead of getting a random selection here, it should be done in the while loop
        // otherwise it'll be the same selection for 12 hours of idle, instead of random mobs each combat
        var enemyCharacters = await GetEnemyCharactersAsync(SelectRandom(combatAction.EnemyTeam.ToList()));

        // Prepare entities for combat
        await PrepareEntitiesForCombat([.. playerCharacters, ..enemyCharacters]);

        var lastCombatResult = new CombatResult();

        while (characterAction.UpdatedAt < now)
        {

            var combatSimulation = new CombatSimulation(playerCharacters, enemyCharacters);
            lastCombatResult = await combatSimulation.RunSimulation();

            // StartedAt is 1 second after the action is initialized, so as to have a 'combat starting' screen
            lastCombatResult.StartedAt = characterAction.UpdatedAt.AddSeconds(1);

            // Update the UpdatedAt timestamp based on combat duration
            // And add 2 seconds to have a delay of one second before and after the fight
            // To display the victory/defeat screen before a new fight is initialized
            characterAction.UpdatedAt += TimeSpan.FromSeconds((lastCombatResult.Duration * 0.1) + 2);

            // Accumulate loot
            //totalLoot.AddRange(lastCombatResult.Loot);

            //combatsPerformed++;

            // Reset entities when combat is over, but only if there's going to be another fight
            // Also process loot, since it's fight that should have already happened
            // If it's a fight where the frontend has yet to display the outcome, the loot should first be processed when the fight is 'over'
            if (characterAction.UpdatedAt < now)
            {
                ResetEntitiesForCombat([.. playerCharacters, .. enemyCharacters]);
            }

            if (lastCombatResult.Outcome.Equals(BattleOutcome.Victory))
            {
                lastCombatResult.Loot = _lootService.GenerateIdleCombatLootAsync(enemyCharacters);
                totalLoot.AddRange(lastCombatResult.Loot);
            }

            //TODO: OPTIMIZE PERHAPS?
            // https://chatgpt.com/c/671943b1-0958-800d-9234-32c45632490e
        }

        lastCombatResult.Loot = _lootService.GenerateIdleCombatLootAsync(enemyCharacters);

        // Create CombatEntities to keep track of simple data over each entity, such as id, health, mana
        lastCombatResult.PlayerTeam = CreateCombatEntities(playerCharacters);
        lastCombatResult.EnemyTeam = CreateCombatEntities(enemyCharacters);

        if (totalLoot.Count > 0)
        {
            await ProcessLootAsync(characterAction.CharacterId, totalLoot, cancellationToken);
        }

        return lastCombatResult;
    }
    private List<Guid> SelectRandom(List<Guid> enemyTeam)
    {
        int randomCount = GetWeightedRandom();
        return enemyTeam.OrderBy(x => Guid.NewGuid()).Take(randomCount).ToList();
    }

    private int GetWeightedRandom()
    {
        Random rand = new Random();
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

    private List<CombatEntity> CreateCombatEntities(List<Entity> playerCharacters)
    {
        var combatEntities = new List<CombatEntity>();

        foreach (var entity in playerCharacters)
        {
            combatEntities.Add(new CombatEntity(entity.Id, entity.Name, (int)entity.BaseCombatAttributes[AttributeType.MaxHealth], (int)entity.BaseCombatAttributes[AttributeType.MaxMana]));
        }

        return combatEntities;
    }

    private async Task<List<Entity>> GetPlayerCharactersAsync(List<Guid> characterTeam, CancellationToken cancellationToken)
    {
        return await _entityService.GetEntitiesByIdsForCombatAsync(characterTeam);
    }

    private async Task<List<Entity>> GetEnemyCharactersAsync(List<Guid> enemyTeam)
    {
        return await _entityService.GetEntitiesByIdsForCombatAsync(enemyTeam);
    }

    private void ResetEntitiesForCombat(List<Entity> allEntities)
    {
        foreach (var entity in allEntities)
        {
            entity.Reset();
        }
    }

    private async Task PrepareEntitiesForCombat(IEnumerable<Entity> entities)
    {
        LoadAbilitiesFromEssences(entities);

        // Load abilities
        var loadedAttributeTasks = entities.Select(entity => Task.Run(() => EssenceLoader.LoadEssencesForEntity(entity)));

        // Calculate attributes
        var calculationTasks = entities.Select(entity => Task.Run(() => AttributeCalculator.CalculateBaseCombatAttributes(entity)));

        await Task.WhenAll(loadedAttributeTasks);
        await Task.WhenAll(calculationTasks);
    }

    private void LoadAbilitiesFromEssences(IEnumerable<Entity> entities)
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
        // Implement how to update the character or game state with the loot
        // For example, updating the character inventory
        //await _InventoryService.AddLootAsync(loot, cancellationToken);
        await _publisher.Publish(new LootGeneratedEvent(characterId, loot), cancellationToken);
    }

    private async Task UpdateCharacterStatsAsync(List<Entity> playerCharacters, CombatResult combatResult, CancellationToken cancellationToken)
    {
        //foreach (var combatant in playerCharacters)
        //{
        //    // Retrieve character entity from database
        //    var character = await _characterRepository.GetCharacterByIdAsync(combatant.Id, cancellationToken);

        //    if (combatResult.Outcome == BattleOutcome.Victory)
        //    {
        //        // Apply experience gain
        //        character.Experience += combatResult.ExperienceGained;
        //        // Handle level-up logic if necessary
        //    }

        //    // Save changes
        //    await _characterRepository.UpdateCharacterAsync(character, cancellationToken);
        //}
    }
}