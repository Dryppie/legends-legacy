using Domain.Models.Attributes;

namespace Domain.Helpers;
public static class EntityBaseAttributeHelper
{
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
        // You can customize this logic as per your game's mechanics
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
                return 10; // Base value for primary stats

            // Combat Stats
            case AttributeType.MaxHealth:
            case AttributeType.Health:
                return 100; // 10 Constition x 10 = 100
            case AttributeType.HealthRegeneration:
                return 3;
            case AttributeType.MaxMana:
            case AttributeType.Mana:
                return 50; // Base mana
            case AttributeType.ManaRegeneration:
                return 3;
            case AttributeType.BasicAttackSpeed:
                return 10;
            case AttributeType.Power:
                return 50; // Base power
            case AttributeType.PhysicalDefense:
            case AttributeType.MagicalDefense:
                return 50; // Base defense
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