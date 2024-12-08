using Application.Interfaces.Services.LL;
using Application.UseCases.Inventories.Events;
using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Inventories;
using Services.LL.Combat;
using Services.LL.Interfaces;

namespace Services.LL._Simulator;
public class SimulatorService : ISimulatorService
{
    private readonly ICombatService _combatService;
    private readonly IEntityService _entityService;
    public SimulatorService(ICombatService combatService)
    {
        _combatService = combatService;
    }



    public async Task SimulateCombat(int fights = 1, int tier = 1)
    {


        // Initialize combatants
        var playerCharacters = await GetPlayerCharactersAsync(combatAction!.CharacterTeam.ToList(), cancellationToken);
        // TODO: Instead of getting a random selection here, it should be done in the while loop
        // otherwise it'll be the same selection for 12 hours of idle, instead of random mobs each combat
        var enemyCharacters = await GetEnemyCharactersAsync(SelectRandom(combatAction.EnemyTeam.ToList()));

        // Prepare entities for combat
        await PrepareEntitiesForCombat([.. playerCharacters, .. enemyCharacters]);

        var lastCombatResult = new CombatResult();

        while (fights > 0)
        {

            var combatSimulation = new CombatSimulation(playerCharacters, enemyCharacters);
            lastCombatResult = await combatSimulation.RunSimulation();

            if (fights > 1)
            {
                ResetEntitiesForCombat([.. playerCharacters, .. enemyCharacters]);
            }


            // TODO: Should I simulate loot at some point? To test drops?
            //if (lastCombatResult.Outcome.Equals(BattleOutcome.Victory))
            //{
            //    lastCombatResult.Loot = _lootService.GenerateIdleCombatLootAsync(enemyCharacters);
            //    totalLoot.AddRange(lastCombatResult.Loot);
            //}

            //TODO: OPTIMIZE PERHAPS?
            // https://chatgpt.com/c/671943b1-0958-800d-9234-32c45632490e

            fights--;
        }
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
        var loadedAttributeTasks = entities.Select(entity => Task.Run(() => AbilityLoader.LoadAbilitiesForEntity(entity)));

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
}