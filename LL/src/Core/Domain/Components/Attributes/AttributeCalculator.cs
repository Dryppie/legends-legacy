using Domain.Extensions;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

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
    /// This is used to get an overview of the entity's attributes after applying equipment and other modifiers.
    /// </summary>
    /// <param name="entity"></param>
    public static void CalculateBaseAttributes(Entity entity, IEnumerable<AttributeModifierBase>? additionalModifiers = null)
    {
        entity.BaseCombatAttributes.Clear();

        var baseAttributes = entity.BaseAttributes.ToDictionary(a => a.AttributeType, a => a.Value);
        var equipment = entity.EquipmentSlots
            .Where(es => es.EquipmentInstance != null)
            .Select(es => es.EquipmentInstance!)
            .DistinctBy(equipment => equipment.Id)
            .ToList();
        var equipmentModifiers = ProjectEquipmentModifiers(equipment, entity.Level);
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
        var equipmentModifiers = ProjectEquipmentModifiers(entity.Equipment, entity.Level);

        foreach (var (attributeType, attributeValue) in CalculateUncappedProjectedAttributes(baseAttributes, equipmentModifiers))
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
        var calculatedValue = GetCombatAttributeValue(entity, attributeType, attribute);
        entity.CombatAttributes[attributeType] = calculatedValue;

        if (attributeType == AttributeType.MaxHealth)
            entity.SyncCurrentHealthAfterMaxHealthChange(oldMaxHealth, calculatedValue);

    }

    private static float GetCombatAttributeValue(CombatEntity entity, AttributeType attributeType, float baseValue)
    {
        // Filter modifiers that apply to the given attribute
        var validModifiers = entity.TemporaryModifiers
            .Where(tm => tm.AttributeType.Equals(attributeType))
            .ToList();

        return ClampAttributeValue(
            attributeType,
            CalculateModifiedValue(baseValue, validModifiers));
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
        // Preserve sub-point precision. Equipment ratings are intentionally
        // converted after aggregation and percentage attributes use decimals.
        float result = (baseValue + flatSum) * (1 + additiveSum) * multiplicativeProduct;
        return Math.Max(result, 0);
    }

    public static IReadOnlyList<AttributeModifierBase> ProjectEquipmentModifiers(
        IEnumerable<EquipmentInstance> equipment,
        int characterLevel)
    {
        var directModifiers = new List<AttributeModifierBase>();
        var rawRatings = new Dictionary<AttributeType, double>();

        foreach (var item in equipment.DistinctBy(item => item.Id))
        {
            EquipmentStatModelMigrator.MigrateToCurrent(item);
            foreach (var modifier in item.AttributeModifiers)
            {
                if (item.UsesProgressionNormalizedRatings
                    && modifier.ModifierType == ModifierType.Flat
                    && EquipmentStatBudgetCatalog.IsRating(modifier.AttributeType))
                {
                    rawRatings[modifier.AttributeType] =
                        rawRatings.GetValueOrDefault(modifier.AttributeType)
                        + modifier.Amount;
                    continue;
                }

                directModifiers.Add(modifier);
            }
        }

        var progressionTier = EquipmentTierBudgetCurve
            .GetExpectedTierForCharacterLevel(characterLevel);
        foreach (var (attribute, rawRating) in rawRatings.OrderBy(entry => entry.Key))
        {
            directModifiers.Add(new InstanceAttributeModifier(
                attribute,
                EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(
                    attribute,
                    rawRating,
                    progressionTier),
                ModifierType.Flat));
        }

        return directModifiers;
    }

    public static IReadOnlyDictionary<AttributeType, double> CollectRawEquipmentRatings(
        IEnumerable<EquipmentInstance> equipment)
    {
        var ratings = new Dictionary<AttributeType, double>();
        foreach (var item in equipment.DistinctBy(item => item.Id))
        {
            EquipmentStatModelMigrator.MigrateToCurrent(item);
            if (!item.UsesProgressionNormalizedRatings)
                continue;

            foreach (var modifier in item.AttributeModifiers)
            {
                if (modifier.ModifierType != ModifierType.Flat
                    || !EquipmentStatBudgetCatalog.IsRating(modifier.AttributeType))
                {
                    continue;
                }

                ratings[modifier.AttributeType] =
                    ratings.GetValueOrDefault(modifier.AttributeType)
                    + modifier.Amount;
            }
        }

        return ratings;
    }

    public static Dictionary<AttributeType, float> CalculateProjectedEquipmentAttributes(
        IReadOnlyDictionary<AttributeType, float> baseAttributes,
        IEnumerable<EquipmentInstance> equipment,
        int characterLevel,
        IEnumerable<AttributeModifierBase>? additionalModifiers = null) =>
        CalculateProjectedAttributes(
            baseAttributes,
            ProjectEquipmentModifiers(equipment, characterLevel)
                .Concat(additionalModifiers ?? []));

    public static Dictionary<AttributeType, float> CalculateProjectedAttributes(
        IReadOnlyDictionary<AttributeType, float> baseAttributes,
        IEnumerable<AttributeModifierBase> modifiers)
    {
        var projected = CalculateUncappedProjectedAttributes(baseAttributes, modifiers);

        foreach (var attribute in projected.Keys.ToArray())
            projected[attribute] = ClampAttributeValue(attribute, projected[attribute]);

        return projected;
    }

    private static Dictionary<AttributeType, float> CalculateUncappedProjectedAttributes(
        IReadOnlyDictionary<AttributeType, float> baseAttributes,
        IEnumerable<AttributeModifierBase> modifiers)
    {
        var modifierList = modifiers.ToList();
        return baseAttributes.Keys
            .Concat(modifierList.Select(x => x.AttributeType))
            .Distinct()
            .ToDictionary(
                attributeType => attributeType,
                attributeType => CalculateModifiedValue(
                    baseAttributes.GetValueOrDefault(attributeType),
                    modifierList.Where(x => x.AttributeType == attributeType)));

    }

    private static float ClampAttributeValue(AttributeType attribute, float value)
    {
        if (!AttributeCatalog.IsKnown(attribute))
            return Math.Max(0f, value);

        var definition = AttributeCatalog.Get(attribute);
        return definition.MaximumValue is { } maximum
               && definition.CapKind is AttributeCapKind.Fixed
                   or AttributeCapKind.ContextDependent
            ? Math.Clamp(value, definition.MinimumValue, maximum)
            : Math.Max(definition.MinimumValue, value);
    }

    private static Dictionary<AttributeType, float> CalculateRuntimeAttributes(CombatEntity entity)
    {
        return CalculateProjectedAttributes(
            entity.BaseCombatAttributes,
            entity.TemporaryModifiers);
    }

    private static void SyncBaseResources(Dictionary<AttributeType, float> attributes)
    {
        attributes[AttributeType.MaxHealth] = attributes.GetValueOrDefault(AttributeType.MaxHealth);
    }

}
