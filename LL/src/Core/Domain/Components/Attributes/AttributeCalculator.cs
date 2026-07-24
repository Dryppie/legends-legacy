using Domain.Extensions;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Domain.Components.Attributes;
public static class AttributeCalculator
{
    /// <summary>
    /// This is used on Creatures
    /// </summary>
    /// <param name="entity"></param>
    public static void InitializeCombatAttributesFromBase(CombatEntity entity)
    {
        entity.CombatAttributes.Clear();

        foreach (var (attributeType, attributeValue) in CalculateRuntimeAttributes(entity))
            entity.CombatAttributes[attributeType] = attributeValue;

        entity.SyncCurrentHealthToMax();
    }

    /// <summary>
    /// This is used to get an overview of the entity's attributes after applying equipment, essences, etc.
    /// </summary>
    /// <param name="entity"></param>
    public static void CalculateBaseAttributes(Entity entity, IEnumerable<AttributeModifierBase>? additionalModifiers = null)
    {
        entity.BaseCombatAttributes.Clear();

        var baseAttributes = entity.BaseAttributes.ToDictionary(a => a.AttributeType, a => a.Value);
        var equipmentModifiers = entity.EquipmentSlots
            .Where(es => es.EquipmentInstance != null)
            .Select(es => es.EquipmentInstance!)
            .DistinctBy(equipment => equipment.Id)
            .SelectMany(equipment => equipment.AttributeModifiers)
            .Cast<AttributeModifierBase>()
            .ToList();
        var modifiers = equipmentModifiers.Concat(additionalModifiers ?? []).ToList();

        foreach (var (attributeType, attributeValue) in CalculateProjectedAttributes(baseAttributes, modifiers))
            entity.BaseCombatAttributes[attributeType] = attributeValue;

        SyncBaseResources(entity.BaseCombatAttributes);
    }

    // Calculates all combat attributes for a given entity - used to initialize players before combat
    public static void CalculateBaseCombatAttributes(CombatEntity entity)
    {
        entity.BaseCombatAttributes.Clear();
        entity.CombatAttributes.Clear();
        // Convert raw attributes to a dictionary for quick access
        var baseAttributes = entity.BaseAttributes.ToDictionary(a => a.AttributeType, a => a.Value);
        var equipmentModifiers = entity.Equipment
            .SelectMany(equipment => equipment.AttributeModifiers)
            .Cast<AttributeModifierBase>()
            .ToList();

        foreach (var (attributeType, attributeValue) in CalculateProjectedAttributes(baseAttributes, equipmentModifiers))
            entity.BaseCombatAttributes[attributeType] = attributeValue;

        SyncBaseResources(entity.BaseCombatAttributes);

        foreach (var (attributeType, attributeValue) in CalculateRuntimeAttributes(entity))
            entity.CombatAttributes[attributeType] = attributeValue;

        entity.SyncCurrentHealthToMax();
    }

    // Recalculate a specific attribute for the entity by attribute type
    public static void CalculateCombatAttributeByType(CombatEntity entity, AttributeType attributeType)
    {
        var attribute = entity.BaseCombatAttributes.GetValueOrDefault(attributeType);

        var oldMaxHealth = entity.CombatAttributes.GetValueOrDefault(AttributeType.MaxHealth);
        var oldValue = entity.CombatAttributes.GetValueOrDefault(attributeType);
        var calculatedValue = GetCombatAttributeValue(entity, attributeType, attribute);

        if (!AttributeCombatRules.IsPrimary(attributeType))
        {
            calculatedValue += GetTemporaryPrimaryContribution(entity, attributeType);
            entity.CombatAttributes[attributeType] = calculatedValue;

            if (attributeType == AttributeType.MaxHealth)
                entity.SyncCurrentHealthAfterMaxHealthChange(oldMaxHealth, calculatedValue);

            return;
        }

        entity.CombatAttributes[attributeType] = calculatedValue;
        AttributeCombatRules.ApplyPrimaryDelta(
            entity.CombatAttributes,
            attributeType,
            calculatedValue - oldValue);

        if (attributeType == AttributeType.Fortitude)
        {
            entity.SyncCurrentHealthAfterMaxHealthChange(
                oldMaxHealth,
                entity.CombatAttributes.GetValueOrDefault(AttributeType.MaxHealth));
        }
    }

    private static float GetCombatAttributeValue(CombatEntity entity, AttributeType attributeType, float baseValue)
    {
        // Filter modifiers that apply to the given attribute
        var validModifiers = entity.TemporaryModifiers
            .Where(tm => tm.AttributeType.Equals(attributeType))
            .ToList();

        return CalculateModifiedValue(baseValue, validModifiers);
    }

    public static float CalculateModifiedValue(float baseValue, IEnumerable<AttributeModifierBase> modifiers)
    {
        float flatSum = 0f;
        float additiveSum = 0f;
        float multiplicativeProduct = 1f;

        // Iterate through each modifier once and calculate sums and product
        foreach (var modifier in modifiers)
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

    public static Dictionary<AttributeType, float> CalculateProjectedAttributes(
        IReadOnlyDictionary<AttributeType, float> baseAttributes,
        IEnumerable<AttributeModifierBase> modifiers,
        bool includePrimaryContributions = true)
    {
        var modifierList = modifiers.ToList();
        var projected = baseAttributes.Keys
            .Concat(modifierList.Select(x => x.AttributeType))
            .Distinct()
            .ToDictionary(
                attributeType => attributeType,
                attributeType => CalculateModifiedValue(
                    baseAttributes.GetValueOrDefault(attributeType),
                    modifierList.Where(x => x.AttributeType == attributeType)));

        if (includePrimaryContributions)
            AttributeCombatRules.ApplyPrimaryContributions(projected);

        return projected;
    }

    private static Dictionary<AttributeType, float> CalculateRuntimeAttributes(CombatEntity entity)
    {
        var calculated = CalculateProjectedAttributes(
            entity.BaseCombatAttributes,
            entity.TemporaryModifiers,
            includePrimaryContributions: false);

        foreach (var primaryAttribute in Enum.GetValues<AttributeType>().Where(AttributeCombatRules.IsPrimary))
        {
            var delta =
                calculated.GetValueOrDefault(primaryAttribute)
                - entity.BaseCombatAttributes.GetValueOrDefault(primaryAttribute);
            AttributeCombatRules.ApplyPrimaryDelta(calculated, primaryAttribute, delta);
        }

        return calculated;
    }

    private static float GetTemporaryPrimaryContribution(
        CombatEntity entity,
        AttributeType derivedAttribute)
    {
        var contribution = 0f;
        foreach (var primaryAttribute in Enum.GetValues<AttributeType>().Where(AttributeCombatRules.IsPrimary))
        {
            var primaryDelta =
                entity.CombatAttributes.GetValueOrDefault(primaryAttribute)
                - entity.BaseCombatAttributes.GetValueOrDefault(primaryAttribute);
            contribution +=
                primaryDelta
                * AttributeCombatRules.GetContributionPerPoint(primaryAttribute, derivedAttribute);
        }

        return contribution;
    }

    private static void SyncBaseResources(Dictionary<AttributeType, float> attributes)
    {
        attributes[AttributeType.MaxHealth] = attributes.GetValueOrDefault(AttributeType.MaxHealth);
    }

}
