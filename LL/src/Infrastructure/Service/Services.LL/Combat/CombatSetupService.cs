using Common.Helpers.Essences;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;

namespace Services.LL.Combat;
public class CombatSetupService : ICombatSetupService
{
    private readonly ICreatureScaler _creatureScaler;

    public CombatSetupService(ICreatureScaler creatureScaler)
    {
        _creatureScaler = creatureScaler;
    }

    public List<CombatEntity> CreatePlayerCombatEntities(List<Entity> entities)
    {
        var combatEntities = new List<CombatEntity>();
        foreach (var entity in entities)
        {
            var combatEntity = new CombatEntity(entity);
            combatEntities.Add(combatEntity);
        }
        return combatEntities;
    }

    public List<CombatEntity> CreateCreatureCombatEntities(List<Entity> entities, Area area)
    {
        var combatEntities = new List<CombatEntity>();
        foreach (var entity in entities)
        {
            if (entity is Creature creature)
            {
                _creatureScaler.ApplyScaling(creature, area);
                creature.BaseAttributes = creature.BaseAttributesDict
                    .Select(kv => new EntityAttribute
                    {
                        AttributeType = kv.Key,
                        Value = kv.Value
                    })
                    .ToList();
                var combatEntity = new CombatEntity(creature);
                combatEntities.Add(combatEntity);
            }
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
        var tasks = entities.Select(e => Task.Run(() =>
        {
            EssenceLoader.Instance.LoadEssencesForCombatEntity(e);
            AttributeCalculator.CalculateBaseCombatAttributes(e);
        }));

        await Task.WhenAll(tasks);
    }

    public List<SimpleCombatEntity> CreateSimpleCombatEntities(List<CombatEntity> combatEntities)
    {
        var simpleCombatEntities = new List<SimpleCombatEntity>();
        foreach (var entity in combatEntities)
        {
            simpleCombatEntities.Add(new SimpleCombatEntity(
                entity.Id,
                entity.Name,
                entity.ImagePath,
                (int)entity.BaseCombatAttributes[AttributeType.MaxHealth],
                (int)entity.BaseCombatAttributes[AttributeType.MaxMana],
                (int)entity.BaseCombatAttributes[AttributeType.Barrier])
            );
        }

        return simpleCombatEntities;
    }
}