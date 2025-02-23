using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Attributes;
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

        if (entityType.Equals("shadowImage"))
        {
            summonedCombatEntity.Name = "Shadow Image";
            var maxHealth = summonedCombatEntity.BaseAttributes.First(ba => ba.AttributeType.Equals(AttributeType.MaxHealth));
            var health = summonedCombatEntity.BaseAttributes.First(ba => ba.AttributeType.Equals(AttributeType.Health));
            var baseAttack = summonedCombatEntity.BaseAttributes.First(ba => ba.AttributeType.Equals(AttributeType.BasicAttackSpeed));
            maxHealth.Value = 1;
            health.Value = 1;
            baseAttack.Value = 0;
        }

        AttributeCalculator.CalculateBaseCombatAttributes(summonedCombatEntity);

        return summonedCombatEntity;
    }
}