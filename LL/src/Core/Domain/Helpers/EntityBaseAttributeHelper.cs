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
            if (entityAttribute.AttributeType != AttributeType.AttackSpeed && entityAttribute.AttributeType != AttributeType.RecoveryRate)
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
            /* ===== VITALITY ===== */
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
            case AttributeType.RecoveryRate:
                return 10; // Determines how often you naturally recover health and mana (HealthRegeneration, ManaRegeneration)
            case AttributeType.Barrier:
                return 0;

            /* ===== OFFENSE =====*/
            case AttributeType.AttackPower:
                return 10; // Base attack power
            case AttributeType.SpellPower:
                return 10; // Base spell power
            case AttributeType.AttackSpeed:
                return 10;
            case AttributeType.Accuracy:
                return 100;
            case AttributeType.CritChance:
                return 0;
            case AttributeType.CritDamage:
                return 100;
            case AttributeType.MultiStrike:
            case AttributeType.MultiCast:
                return 0;
            case AttributeType.ArmorPenetration:
            case AttributeType.ManaPenetration:
                return 0;

            /* ===== DEFENSE ===== */
            case AttributeType.PhysicalDefense:
            case AttributeType.MagicalDefense:
            case AttributeType.DamageReduction:
            case AttributeType.CritDamageReduction:
            case AttributeType.CrowdControlResistance:
            case AttributeType.Dodge:
            case AttributeType.Block:
            case AttributeType.Parry:
                return 0;

            /* ===== CONTROL & UTILITY ===== */
            case AttributeType.Threat:
                return 10;
            case AttributeType.CooldownReduction:
                return 0;

            /* ===== Resistances ===== */
            case AttributeType.FireResistance:
            case AttributeType.WaterResistance:
            case AttributeType.EarthResistance:
            case AttributeType.AirResistance:
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