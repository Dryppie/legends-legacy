using Domain.Models.Attributes;

namespace Domain.Helpers;
public static class EntityBaseAttributeHelper
{
    public static List<EntityAttribute> CreateEntityAttributesWithIncrease(Guid entityId, float percentage)
    {
        var entityAttributes = CreateEntityAttributes(entityId);
        foreach (var entityAttribute in entityAttributes)
        {
            // Increase the value by the specified percentage
            if (entityAttribute.AttributeType != AttributeType.BasicAttackSpeed && entityAttribute.AttributeType != AttributeType.RecoveryRate)
            {
                entityAttribute.Value = (int)(entityAttribute.Value * (1 + percentage));
            }
        }

        return entityAttributes;
    }
    public static List<EntityAttribute> CreateEntityAttributes(Guid entityId)
    {
        var entityAttributes = Enum.GetValues(typeof(AttributeType))
            .Cast<AttributeType>()
            .Select(attributeType => new EntityAttribute
            {
                EntityId = entityId,
                AttributeType = attributeType,
                Value = GetBaseValueForAttribute(attributeType)
            })
            .ToList();

        return entityAttributes;
    }

    private static int GetBaseValueForAttribute(AttributeType attributeType)
    {
        // Define base values for each attribute type
        switch (attributeType)
        {
            // Primary Stats
            case AttributeType.Constitution:
            case AttributeType.Endurance:
            case AttributeType.Willpower:
            case AttributeType.Strength:
            case AttributeType.FightingSpirit:
            case AttributeType.Dexterity:
            case AttributeType.Agility:
            case AttributeType.Intelligence:
            case AttributeType.Wisdom:
            case AttributeType.Instinct:
            case AttributeType.Perception:
            case AttributeType.Luck:
                return 0; // Base value for primary stats

            // Combat Stats
            case AttributeType.MaxHealth:
            case AttributeType.Health:
                return 100;
            case AttributeType.HealthRegeneration:
                return 2;
            case AttributeType.MaxMana:
            case AttributeType.Mana:
                return 100;
            case AttributeType.ManaRegeneration:
                return 2;
            case AttributeType.BasicAttackSpeed:
                return 10;
            case AttributeType.RecoveryRate:
                return 10; // Determines how often you naturally recover health and mana (HealthRegeneration, ManaRegeneration)
            case AttributeType.Power:
                return 10; // Base power
            case AttributeType.PhysicalDefense:
            case AttributeType.MagicalDefense:
                return 10; // Base defense
            case AttributeType.FlatDamageReduction:
                return 0;
            case AttributeType.DamageReduction:
                return 0;
            case AttributeType.CritChance:
                return 0; // Base speed
            case AttributeType.CritDamage:
                return 100; // Base speed
            case AttributeType.CritDamageReduction:
                return 0; // Base speed
            case AttributeType.Threat:
                return 10; // Base speed
            case AttributeType.CrowdControlResistance:
                return 0; // Base speed
            case AttributeType.Accuracy:
                return 100; // Base speed
            case AttributeType.Dodge:
                return 0; // Base speed
            case AttributeType.Block:
            case AttributeType.Parry:
            case AttributeType.Barrier:
            case AttributeType.MultiStrike:
            case AttributeType.MultiCast:
            case AttributeType.CooldownReduction:
            case AttributeType.ArmorPenetration:
            case AttributeType.ManaPenetration:
            case AttributeType.LifeSteal:
                return 0; // Base speed

            // Resistances
            case AttributeType.FireResistance:
            case AttributeType.WaterResistance:
            case AttributeType.EarthResistance:
            case AttributeType.AirResistance:
            case AttributeType.PoisonResistance:
                return 0; // Base resistance

            default:
                return 0; // Default base value
        }
    }

    public static List<EntityAttribute> CreateSimulatedAttributes(int tier)
    {
        var entityAttributes = Enum.GetValues(typeof(AttributeType))
            .Cast<AttributeType>()
            .Select(attributeType => new EntityAttribute
            {
                EntityId = new Guid(),
                AttributeType = attributeType,
                Value = GetBaseValueForAttribute(attributeType) * tier
            })
            .ToList();

        return entityAttributes;
    }
}