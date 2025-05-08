using Common.Helpers.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Services.LL.Interfaces;

namespace Services.LL.Combat;
public class CombatSetupService : ICombatSetupService
{
    public List<CombatEntity> CreateCombatEntities(List<Entity> entities)
    {
        var combatEntities = new List<CombatEntity>();
        foreach (var entity in entities)
        {
            var combatEntity = new CombatEntity(entity);
            combatEntities.Add(combatEntity);
        }
        return combatEntities;
    }
    public void AppendPrefixToId(List<CombatEntity> selectedCombatEnemyEntities)
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
    public async Task PrepareEntitiesForCombat(List<CombatEntity> entities)
    {
        // Load abilities
        var loadedAttributeTasks = entities.Select(entity => Task.Run(() => EssenceLoader.Instance.LoadEssencesForCombatEntity(entity)));

        // Calculate attributes
        var calculationTasks = entities.Select(entity => Task.Run(() => AttributeCalculator.CalculateBaseCombatAttributes(entity)));

        await Task.WhenAll(loadedAttributeTasks);
        await Task.WhenAll(calculationTasks);
    }
    public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities)
    {
        var simpleCombatEntities = new List<SimpleCombatEntity>();
        foreach (var entity in combatEntities)
        {
            simpleCombatEntities.Add(new SimpleCombatEntity(entity.Id, entity.Name, entity.ImagePath, (int)entity.BaseCombatAttributes[AttributeType.MaxHealth], (int)entity.BaseCombatAttributes[AttributeType.MaxMana], (int)entity.BaseCombatAttributes[AttributeType.Barrier]));
        }

        return simpleCombatEntities;
    }
}