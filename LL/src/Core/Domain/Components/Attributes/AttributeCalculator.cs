using Domain.Extensions;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Items.Equipments;

namespace Domain.Components.Attributes;
public static class AttributeCalculator
{
    /// <summary>
    /// This is used to get an overview of the entity's attributes after applying equipment, essences, etc.
    /// </summary>
    /// <param name="entity"></param>
    public static void CalculateBaseAttributes(Entity entity)
    {
        entity.BaseCombatAttributes.Clear();

        var baseAttributes = entity.BaseAttributes.ToDictionary(a => a.AttributeType, a => a.Value);

        foreach (var (attributeType, attributeValue) in baseAttributes )
        {
            entity.BaseCombatAttributes.Add(attributeType, attributeValue);
        }

        foreach (var attributeModifier in entity.EssenceSlots.ActiveSlotsWithOccupiedEssences().Select(es => es.OccupiedEssence).SelectMany(e => e.AttributeModifiers))
        {
            entity.BaseCombatAttributes[attributeModifier.AttributeType] += attributeModifier.Amount;
        }

        foreach (var equipment in entity.EquipmentSlots.Where(es => es.EquipmentInstance != null).Select(es => es.EquipmentInstance!))
        {
            foreach (var attributeModifier in equipment.AttributeModifiers)
            {
                entity.BaseCombatAttributes[attributeModifier.AttributeType] += attributeModifier.Amount;
            }
        }

        entity.BaseCombatAttributes[AttributeType.Health] = entity.BaseCombatAttributes[AttributeType.MaxHealth];
        entity.BaseCombatAttributes[AttributeType.Mana] = entity.BaseCombatAttributes[AttributeType.MaxMana];
    }

    // Calculates all combat attributes for a given entity
    public static void CalculateBaseCombatAttributes(CombatEntity entity)
    {
        entity.BaseCombatAttributes.Clear();
        entity.CombatAttributes.Clear();
        // Convert raw attributes to a dictionary for quick access
        var baseAttributes = entity.BaseAttributes.ToDictionary(a => a.AttributeType, a => a.Value);

        foreach (var (attributeType, attributeValue) in baseAttributes)
        {
            entity.BaseCombatAttributes.Add(attributeType, attributeValue);
        }

        foreach (var attributeModifier in entity.EquippedEssences.SelectMany(ee => ee.AttributeModifiers))
        {
            entity.BaseCombatAttributes[attributeModifier.AttributeType] += attributeModifier.Amount;
        }

        foreach (var equipment in entity.Equipment)
        {
            foreach (var attributeModifier in equipment.AttributeModifiers)
            {
                entity.BaseCombatAttributes[attributeModifier.AttributeType] += attributeModifier.Amount;
            }
        }

        entity.BaseCombatAttributes[AttributeType.Health] = entity.BaseCombatAttributes[AttributeType.MaxHealth];
        entity.BaseCombatAttributes[AttributeType.Mana] = entity.BaseCombatAttributes[AttributeType.MaxMana];

        // Iterate over each attribute of the entity, and calculate baseAttributes
        foreach (var (attributeType, attributeValue) in entity.BaseCombatAttributes)
        {
            var calculatedValue = GetCombatAttributeValue(entity, attributeType, attributeValue);

            entity.CombatAttributes.TryAdd(attributeType, attributeValue);
        }
    }

    // Recalculate a specific attribute for the entity by attribute type
    public static void CalculateCombatAttributeByType(CombatEntity entity, AttributeType attributeType)
    {
        // Find the attribute in BaseAttributes or CombatAttributes
        if (!entity.BaseCombatAttributes.TryGetValue(attributeType, out var attribute)) return;

        var calculatedValue = GetCombatAttributeValue(entity, attributeType, attribute);

        MaxHealthOrMaxMana(entity, attributeType, calculatedValue);

        if (entity.CombatAttributes.TryAdd(attributeType, calculatedValue))
            return;

        entity.CombatAttributes[attributeType] = calculatedValue;

        HealthOrMana(entity, attributeType);
    }

    private static float GetCombatAttributeValue(CombatEntity entity, AttributeType attributeType, float baseValue)
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

                    if (entity.CombatAttributes.TryGetValue(AttributeType.Health, out var currentHealth))
                    {
                        if (difference < 0) // This is only applied to scenarios where MaxHealth is decreased
                        {
                            // currentHealth is higher than new MaxHealth, cap hp to new MaxHealth
                            if (currentHealth > calculatedValue)
                                entity.CombatAttributes[AttributeType.Health] = calculatedValue;

                            break;
                        }

                        float newHealth = currentHealth + difference;

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

                    if (entity.CombatAttributes.TryGetValue(AttributeType.Mana, out var currentMana))
                    {
                        if (difference < 0) // This is only applied to scenarios where MaxMana is decreased
                        {
                            // If currentMana is higher than new MaxMana, cap mp to new MaxMana
                            if (currentMana > calculatedValue)
                                entity.CombatAttributes[AttributeType.Mana] = calculatedValue;

                            break;
                        }

                        float newMana = currentMana + difference;

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

    private static void HealthOrMana(CombatEntity entity, AttributeType attribute)
    {
        switch (attribute)
        {
            case AttributeType.Health:
                {
                    if (entity.CombatAttributes[AttributeType.Health] > entity.CombatAttributes[AttributeType.MaxHealth])
                        entity.CombatAttributes[AttributeType.Health] = entity.CombatAttributes[AttributeType.MaxHealth];
                    break;
                }

            case AttributeType.Mana:
                {
                    if (entity.CombatAttributes[AttributeType.Mana] > entity.CombatAttributes[AttributeType.MaxMana])
                        entity.CombatAttributes[AttributeType.Mana] = entity.CombatAttributes[AttributeType.MaxMana];
                    break;
                }

            default:
                break;
        }
    }
}