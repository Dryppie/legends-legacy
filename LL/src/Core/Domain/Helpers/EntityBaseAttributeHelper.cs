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
            case AttributeType.Perception:
            case AttributeType.Luck:
                return 10; // Base value for primary stats

            // Combat Stats
            case AttributeType.Health:
            case AttributeType.MaxHealth:
                return 100; // Base health
            case AttributeType.Mana:
            case AttributeType.MaxMana:
                return 50; // Base mana
            case AttributeType.HealthRegeneration:
                return 10;
            case AttributeType.ManaRegeneration:
                return 10;
            case AttributeType.BasicAttackSpeed:
                return 30;
            case AttributeType.AttackPower:
            case AttributeType.MagicPower:
                return 15; // Base power
            case AttributeType.Defense:
            case AttributeType.MagicDefense:
                return 5; // Base defense
            case AttributeType.Speed:
                return 10; // Base speed
            case AttributeType.CritChance:
                return 0; // Base speed
            case AttributeType.CritDamage:
                return 150; // Base speed
            case AttributeType.CritResistance:
                return 10; // Base speed
            case AttributeType.Threat:
                return 100; // Base speed
            case AttributeType.CrowdControlResistance:
                return 0; // Base speed
            case AttributeType.Accuracy:
                return 10; // Base speed
            case AttributeType.Dodge:
                return 10; // Base speed
            case AttributeType.Block:
                return 0; // Base speed
            case AttributeType.CooldownReduction:
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
}