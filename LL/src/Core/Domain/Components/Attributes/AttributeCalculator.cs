using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Domain.Components.Attributes;
public static class AttributeCalculator
{
    // Calculates the attributes for a given entity
    public static void CalculateBaseCombatAttributes(CombatEntity entity)
    {
        entity.BaseCombatAttributes.Clear();
        // Convert raw attributes to a dictionary for quick access
        var baseAttributes = entity.BaseAttributes.ToDictionary(a => a.AttributeType, a => a);

        // Iterate over each attribute of the entity
        foreach (var attribute in baseAttributes.Values)
        {
            var calculatedValue = GetAttributeValue(entity, attribute.AttributeType, attribute.Value);

            entity.CombatAttributes[attribute.AttributeType] = calculatedValue;
            entity.BaseCombatAttributes[attribute.AttributeType] = calculatedValue;
        }
    }

    // Recalculate a specific attribute for the entity by attribute type
    public static void CalculateCombatAttributeByType(CombatEntity entity, AttributeType attributeType)
    {
        // Find the attribute in BaseAttributes or CombatAttributes
        var attribute = entity.BaseAttributes.FirstOrDefault(a => a.AttributeType == attributeType);

        // Calculate and update the attribute's value
        if (attribute == null) return;

        var calculatedValue = GetAttributeValue(entity, attributeType, attribute.Value);

        MaxHealthOrMaxMana(entity, attributeType, calculatedValue);

        if (entity.CombatAttributes.TryAdd(attributeType, calculatedValue))
            return;

        entity.CombatAttributes[attributeType] = calculatedValue;
    }

    private static float GetAttributeValue(CombatEntity entity, AttributeType attributeType, float baseValue)
    {
        // Filter modifiers that apply to the given attribute
        var validModifiers = entity.TemporaryModifiers
            .Where(tm => tm.AttributeType.Equals(attributeType))
            .ToList();

        if (validModifiers.Count == 0) return baseValue;

        float flatSum = 0f;
        float additiveSum = 0f;
        float multiplicativeProduct = 1f;

        // Iterate through each modifier once and calculate sums and product
        foreach (var modifier in validModifiers)
        {
            switch (modifier.ModifierType)
            {
                case ModifierType.Flat:
                    flatSum += modifier.Amount;
                    break;
                case ModifierType.Additive:
                    additiveSum += modifier.Amount / 100f;
                    break;
                case ModifierType.Multiplicative:
                    multiplicativeProduct *= (1 + modifier.Amount / 100f);
                    break;
            }
        }
        // Return the final rounded attribute value
        float result = MathF.Round((baseValue + flatSum) * (1 + additiveSum) * multiplicativeProduct, MidpointRounding.ToZero);
        return Math.Max(result, 0);
    }

    private static void MaxHealthOrMaxMana(CombatEntity entity, AttributeType attributeType, float calculatedValue)
    {
        switch (attributeType)
        {
            case AttributeType.MaxHealth:
                {
                    float oldMax = entity.CombatAttributes.TryGetValue(AttributeType.MaxHealth, out var oldMaxObj)
                        ? oldMaxObj
                        : 0;

                    float difference = calculatedValue - oldMax;

                    if (entity.CombatAttributes.TryGetValue(AttributeType.Health, out var oldHealth))
                    {
                        float newHealth = oldHealth + difference;

                        // Clamp to [0, new MaxHealth]
                        if (newHealth > calculatedValue) newHealth = calculatedValue;
                        if (newHealth < 0) newHealth = 0;

                        entity.CombatAttributes[AttributeType.Health] = newHealth;
                    }

                    break;
                }

            case AttributeType.MaxMana:
                {
                    float oldMax = entity.CombatAttributes.TryGetValue(AttributeType.MaxMana, out var oldMaxObj)
                        ? oldMaxObj
                        : 0;

                    float difference = calculatedValue - oldMax;

                    if (entity.CombatAttributes.TryGetValue(AttributeType.Mana, out var oldMana))
                    {
                        float newMana = oldMana + difference;

                        if (newMana > calculatedValue) newMana = calculatedValue;
                        if (newMana < 0) newMana = 0;

                        entity.CombatAttributes[AttributeType.Mana] = newMana;
                    }

                    break;
                }
            default:
                break;
        }
    }
}