using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Combat;

namespace Domain.Models.Entities.Creatures;
public static class SummonCreatureFactory
{
    public static CombatEntity CreateCreature(string entityType)
    {
        // Load entity data from a data source (e.g., JSON file, database)
        // For simplicity, create an entity with default values

        var summonedCreature = new Creature
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = "Blood Imp"
            // Set other properties like health, abilities, stats
        };

        var summonedCombatEntity = new CombatEntity(summonedCreature);
        summonedCombatEntity.IsSummoned = true;

        summonedCombatEntity.BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(Guid.Parse(summonedCombatEntity.Id));
        AttributeCalculator.CalculateBaseCombatAttributes(summonedCombatEntity);

        return summonedCombatEntity;
    }
}