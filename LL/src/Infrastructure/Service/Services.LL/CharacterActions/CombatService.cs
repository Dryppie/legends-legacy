using Application.Interfaces.Services.LL;
using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Force.DeepCloner;
using Services.LL.Combat;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class CombatService : ICombatService
{
    private readonly IEntityService _entityService;

    public CombatService(IEntityService entityService)
    {
        _entityService = entityService;
    }

    public async Task<CombatResult> PerformCombatAsync(CombatAction combatAction, CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Initialize combatants
        var playerCharacters = await GetPlayerCharactersAsync(combatAction.CharacterTeam, cancellationToken);
        var enemyCharacters = await GetEnemyCharactersAsync(combatAction.EnemyTeam);

        // Prepare entities for combat
        await PrepareEntitiesForCombat(playerCharacters.Concat(enemyCharacters));

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
            if (characterAction.UpdatedAt < now) ResetEntitiesForCombat([.. playerCharacters, .. enemyCharacters]);

            //TODO: OPTIMIZE PERHAPS?
            // https://chatgpt.com/c/671943b1-0958-800d-9234-32c45632490e
        }

        lastCombatResult.PlayerTeam = CreateCombatEntities(playerCharacters);
        lastCombatResult.EnemyTeam = CreateCombatEntities(enemyCharacters);


        return lastCombatResult;
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
        return await _entityService.GetEntitiesByIdsAsync(characterTeam);
    }

    private async Task<List<Entity>> GetEnemyCharactersAsync(List<Guid> enemyTeam)
    {
        return await _entityService.GetEntitiesByIdsAsync(enemyTeam);
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
        // Load abilities
        var loadedAttributeTasks = entities.Select(entity => Task.Run(() => AbilityLoader.LoadAbilitiesForEntity(entity)));

        // Calculate attributes
        var calculationTasks = entities.Select(entity => Task.Run(() => AttributeCalculator.CalculateBaseCombatAttributes(entity)));

        await Task.WhenAll(loadedAttributeTasks);
        await Task.WhenAll(calculationTasks);
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