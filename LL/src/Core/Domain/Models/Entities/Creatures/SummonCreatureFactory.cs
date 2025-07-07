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
            Id = Guid.NewGuid(),
            Name = "Blood Imp",
            ImagePath = "blood_imp"
            // Set other properties like health, abilities, stats
        };

        var summonedCombatEntity = new CombatEntity(summonedCreature);
        summonedCombatEntity.IsSummoned = true;

        summonedCombatEntity.BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributesWithIncrease(Guid.Parse(summonedCombatEntity.Id), -1f);

        if (entityType.Equals("shadowImage"))
        {
            summonedCombatEntity.Name = "Shadow Image";
            summonedCombatEntity.ImagePath = "shadow_image";
            var maxHealth = summonedCombatEntity.BaseAttributes.First(ba => ba.AttributeType.Equals(AttributeType.MaxHealth));
            var health = summonedCombatEntity.BaseAttributes.First(ba => ba.AttributeType.Equals(AttributeType.Health));
            var baseAttack = summonedCombatEntity.BaseAttributes.First(ba => ba.AttributeType.Equals(AttributeType.AttackSpeed));
            maxHealth.Value = 1;
            health.Value = 1;
            baseAttack.Value = 0;
        }

        AttributeCalculator.CalculateBaseCombatAttributes(summonedCombatEntity);

        return summonedCombatEntity;
    }
}