using Domain.Models.Attributes;

namespace Domain.Helpers;
public static class EntityBaseAttributeHelper
{
    public static List<EntityAttribute> CreateEntityAttributesWithIncrease(Guid entityId, float percentage)
    {
        var entityAttributes = CreateEntityAttributes(entityId);
        foreach (var entityAttribute in entityAttributes)
        {
            entityAttribute.Value = (int)(entityAttribute.Value * (1 + percentage));
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
        return attributeType switch
        {
            AttributeType.Power => 10,
            AttributeType.Fortitude => 10,
            AttributeType.Precision => 10,
            AttributeType.Spirit => 10,
            AttributeType.MaxHealth => 100,
            AttributeType.CritDamage => 100,
            AttributeType.HealthRegeneration => 2,
            _ => 0
        };
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
