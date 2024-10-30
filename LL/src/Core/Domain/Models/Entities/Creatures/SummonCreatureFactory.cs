using Domain.Components.Attributes;
using Domain.Helpers;

namespace Domain.Models.Entities.Creatures;
public static class SummonCreatureFactory
{
    public static Creature CreateCreature(string entityType)
    {
        // Load entity data from a data source (e.g., JSON file, database)
        // For simplicity, create an entity with default values

        var summonedCreature = new Creature
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = "Blood Imp",
            IsSummoned = true,
            // Set other properties like health, abilities, stats
        };

        summonedCreature.BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(summonedCreature.Id);
        AttributeCalculator.CalculateBaseCombatAttributes(summonedCreature);

        return summonedCreature;
    }
}