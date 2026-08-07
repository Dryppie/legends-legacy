using Domain.Models.Attributes;

namespace Domain.Helpers;
public static class EntityBaseAttributeHelper
{
    public const float PowerPerCharacterLevel = 0.25f;
    public const int MaxHealthPerCharacterLevel = 20;

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
        var entityAttributes = AttributeCatalog.All
            .Select(definition => definition.AttributeType)
            .Select(attributeType => new EntityAttribute
            {
                EntityId = entityId,
                AttributeType = attributeType,
                Value = GetBaseValueForAttribute(attributeType)
            })
            .ToList();

        return entityAttributes;
    }

    public static List<EntityAttribute> CreateEntityAttributesForLevel(
        Guid entityId,
        int level)
    {
        var attributes = CreateEntityAttributes(entityId);
        foreach (var attribute in attributes)
            attribute.Value = GetValueForCharacterLevel(attribute.AttributeType, level);

        return attributes;
    }

    public static float GetValueForCharacterLevel(
        AttributeType attributeType,
        int level)
    {
        var completedLevelUps = Math.Max(0, level - 1);
        var baseValue = GetBaseValueForAttribute(attributeType);

        return attributeType switch
        {
            AttributeType.Power => baseValue + (PowerPerCharacterLevel * completedLevelUps),
            AttributeType.MaxHealth => baseValue + (MaxHealthPerCharacterLevel * completedLevelUps),
            _ => baseValue
        };
    }

    private static int GetBaseValueForAttribute(AttributeType attributeType)
    {
        return attributeType switch
        {
            AttributeType.Power => 10,
            AttributeType.MaxHealth => 140,
            AttributeType.CritDamage => 100,
            AttributeType.HealthRegeneration => 2,
            _ => 0
        };
    }

    public static List<EntityAttribute> CreateSimulatedAttributes(int tier)
    {
        var entityAttributes = AttributeCatalog.All
            .Select(definition => definition.AttributeType)
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
