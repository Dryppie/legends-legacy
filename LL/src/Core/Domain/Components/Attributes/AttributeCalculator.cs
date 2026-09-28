using Domain.Extensions;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;

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
        AddUniversalBaseAttributes(baseAttributes);
        var equipment = entity.EquipmentSlots
            .Where(es => es.EquipmentInstance != null)
            .Select(es => es.EquipmentInstance!)
            .DistinctBy(equipment => equipment.Id)
            .ToList();
        var equipmentModifiers = ProjectEquipmentModifiers(equipment, entity.Level, entity.AttributeRulesVersion);
        var modifiers = equipmentModifiers.Concat(additionalModifiers ?? []).ToList();

        foreach (var (attributeType, attributeValue) in CalculateProjectedAttributes(baseAttributes, modifiers, entity.AttributeRulesVersion))
            entity.BaseCombatAttributes[attributeType] = attributeValue;

        if (entity.AttributeRulesVersion == AttributeRules.CurrentVersion)
        {
            var raw = CalculateUncappedProjectedAttributes(baseAttributes, modifiers, entity.AttributeRulesVersion);
            var interval = entity.EquipmentSlots.FirstOrDefault(x => x.EquipmentSlotType == EquipmentSlotType.MainHand)
                ?.EquipmentInstance?.ProgressionData?.Behavior.BasicAttackIntervalMultiplier ?? 1d;
            entity.BaseCombatAttributes[AttributeType.AttackSpeed] = Math.Clamp(raw.GetValueOrDefault(AttributeType.AttackSpeed),
                0, AttributeCombatRules.CalculateUsefulAttackSpeedCapPercent(interval));
        }

        SyncBaseResources(entity.BaseCombatAttributes);
    }

    // Calculates all combat attributes for a given entity - used to initialize players before combat
    public static void CalculateBaseCombatAttributes(
        CombatEntity entity,
        IEnumerable<AttributeModifierBase>? additionalBaseModifiers = null)
    {
        entity.BaseCombatAttributes.Clear();
        entity.CombatAttributes.Clear();
        // Convert raw attributes to a dictionary for quick access
        var baseAttributes = entity.BaseAttributes.ToDictionary(a => a.AttributeType, a => a.Value);
        AddUniversalBaseAttributes(baseAttributes);
        var equipmentModifiers = ProjectEquipmentModifiers(entity.Equipment, entity.Level, entity.AttributeRulesVersion)
            .Concat(additionalBaseModifiers ?? [])
            .ToArray();

        foreach (var (attributeType, attributeValue) in CalculateUncappedProjectedAttributes(baseAttributes, equipmentModifiers, entity.AttributeRulesVersion))
            entity.BaseCombatAttributes[attributeType] = attributeValue;

        SyncBaseResources(entity.BaseCombatAttributes);

        foreach (var (attributeType, attributeValue) in CalculateRuntimeAttributes(entity))
            entity.CombatAttributes[attributeType] = attributeValue;

        entity.SyncCurrentHealthToMax();
    }

    // Recalculate a specific attribute for the entity by attribute type
    public static void CalculateCombatAttributeByType(CombatEntity entity, AttributeType attributeType)
    {
        if (entity.AttributeRulesVersion == AttributeRules.CurrentVersion)
        {
            // Retired aliases and derived defense values can affect more than one key.
            // Reproject from the unchanged base so removal restores overcap contributions.
            var previousHealth = entity.CombatAttributes.GetValueOrDefault(AttributeType.MaxHealth);
            var projected = CalculateRuntimeAttributes(entity);
            entity.CombatAttributes.Clear();
            foreach (var pair in projected) entity.CombatAttributes[pair.Key] = pair.Value;
            if (attributeType == AttributeType.MaxHealth)
                entity.SyncCurrentHealthAfterMaxHealthChange(previousHealth, projected.GetValueOrDefault(AttributeType.MaxHealth));
            return;
        }
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
            CalculateModifiedValue(baseValue, validModifiers), entity.AttributeRulesVersion);
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
        int characterLevel,
        int rulesVersion = AttributeRules.CurrentVersion)
    {
        AttributeRules.ValidateVersion(rulesVersion);
        var directModifiers = new List<AttributeModifierBase>();
        var rawRatings = new Dictionary<AttributeType, double>();

        foreach (var item in equipment.DistinctBy(item => item.Id))
        {
            foreach (var modifier in item.AttributeModifiers)
            {
                if (item.UsesProgressionNormalizedRatings
                    && modifier.ModifierType == ModifierType.Flat
                    && EquipmentStatBudgetCatalog.IsRating(modifier.AttributeType))
                {
                    rawRatings[modifier.AttributeType] =
                        rawRatings.GetValueOrDefault(modifier.AttributeType)
                        + (rulesVersion == AttributeRules.CurrentVersion
                            ? modifier.Amount / EquipmentTierBudgetCurve.GetScale(Math.Max(1, item.Tier))
                            : modifier.Amount);
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
                rulesVersion == AttributeRules.CurrentVersion ? AttributeRules.RatingAttribute(attribute) : attribute,
                rulesVersion == AttributeRules.CurrentVersion ? (float)rawRating : EquipmentStatBudgetCatalog.ConvertRatingToEffectiveValue(
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
        IEnumerable<AttributeModifierBase>? additionalModifiers = null, int rulesVersion = AttributeRules.CurrentVersion) =>
        CalculateProjectedAttributes(
            baseAttributes,
            ProjectEquipmentModifiers(equipment, characterLevel, rulesVersion)
                .Concat(additionalModifiers ?? []), rulesVersion);

    public static Dictionary<AttributeType, float> CalculateUncappedEquipmentAttributes(
        IReadOnlyDictionary<AttributeType, float> baseAttributes, IEnumerable<EquipmentInstance> equipment,
        int characterLevel, IEnumerable<AttributeModifierBase>? additionalModifiers = null, int rulesVersion = AttributeRules.CurrentVersion) =>
        CalculateUncappedProjectedAttributes(baseAttributes,
            ProjectEquipmentModifiers(equipment, characterLevel, rulesVersion).Concat(additionalModifiers ?? []), rulesVersion);

    public static Dictionary<AttributeType, float> CalculateProjectedAttributes(
        IReadOnlyDictionary<AttributeType, float> baseAttributes,
        IEnumerable<AttributeModifierBase> modifiers,
        int rulesVersion = AttributeRules.CurrentVersion)
    {
        var projected = CalculateUncappedProjectedAttributes(baseAttributes, modifiers, rulesVersion);

        foreach (var attribute in projected.Keys.ToArray())
            projected[attribute] = ClampAttributeValue(attribute, projected[attribute], rulesVersion);

        return projected;
    }

    private static Dictionary<AttributeType, float> CalculateUncappedProjectedAttributes(
        IReadOnlyDictionary<AttributeType, float> baseAttributes,
        IEnumerable<AttributeModifierBase> modifiers,
        int rulesVersion = AttributeRules.CurrentVersion)
    {
        AttributeRules.ValidateVersion(rulesVersion);
        var modifierList = modifiers.ToList();
        var values = baseAttributes.ToDictionary(x => x.Key, x => x.Value);
        if (rulesVersion == AttributeRules.CurrentVersion)
        {
            foreach (var defense in new[] { AttributeType.Armor, AttributeType.Resistance })
            {
                var rating = AttributeRules.RatingAttribute(defense);
                // Authored creature/legacy bases are effective percentages; items already
                // enter as normalized ratings. Do not invert the derived display twice.
                if (values.GetValueOrDefault(rating) == 0)
                    values[rating] = AttributeRules.RatingFromLegacyPercent(values.GetValueOrDefault(defense));
                values.Remove(defense);
            }
            modifierList = modifierList.Select(modifier => (AttributeModifierBase)new InstanceAttributeModifier(
                AttributeRules.RatingAttribute(modifier.AttributeType), modifier.Amount, modifier.ModifierType)).ToList();
        }
        var projected = values.Keys
            .Concat(modifierList.Select(x => x.AttributeType))
            .Distinct()
            .ToDictionary(
                attributeType => attributeType,
                attributeType => CalculateModifiedValue(
                    values.GetValueOrDefault(attributeType),
                    modifierList.Where(x => x.AttributeType == attributeType)));
        if (rulesVersion == AttributeRules.CurrentVersion)
        {
            // Convert the complete old allocation once: per-item CDR conversion is nonlinear.
            Move(AttributeType.Cooldown, AttributeType.AbilityHaste, AttributeRules.HasteFromCooldownReduction);
            Move(AttributeType.HealingPowerPercent, AttributeType.Restoration, value => value);
            var oldResistance = Math.Max(projected.GetValueOrDefault(AttributeType.StatusResistance),
                projected.GetValueOrDefault(AttributeType.CrowdControlResistance));
            projected.Remove(AttributeType.StatusResistance);
            projected.Remove(AttributeType.CrowdControlResistance);
            projected[AttributeType.Tenacity] = projected.GetValueOrDefault(AttributeType.Tenacity) + oldResistance;
            projected[AttributeType.Armor] = 100f * AttributeRules.Mitigation(projected.GetValueOrDefault(AttributeType.ArmorRating));
            projected[AttributeType.Resistance] = 100f * AttributeRules.Mitigation(projected.GetValueOrDefault(AttributeType.ResistanceRating));
        }
        return projected;

        void Move(AttributeType from, AttributeType to, Func<float, float> convert)
        {
            if (projected.Remove(from, out var value))
                projected[to] = projected.GetValueOrDefault(to) + convert(value);
        }
    }

    private static float ClampAttributeValue(AttributeType attribute, float value, int rulesVersion)
    {
        if (!AttributeCatalog.IsKnown(attribute))
            return Math.Max(0f, value);

        var definition = AttributeCatalog.Get(attribute, rulesVersion);
        return definition.MaximumValue is { } maximum
               && definition.CapKind is AttributeCapKind.Fixed
                   or AttributeCapKind.ContextDependent
            ? Math.Clamp(value, definition.MinimumValue, maximum)
            : Math.Max(definition.MinimumValue, value);
    }

    private static Dictionary<AttributeType, float> CalculateRuntimeAttributes(CombatEntity entity)
    {
        if (entity.AttributeRulesVersion == AttributeRules.CurrentVersion)
            return CalculateUncappedProjectedAttributes(entity.BaseCombatAttributes, entity.TemporaryModifiers, entity.AttributeRulesVersion);
        return CalculateProjectedAttributes(
            entity.BaseCombatAttributes,
            entity.TemporaryModifiers,
            entity.AttributeRulesVersion);
    }

    private static void SyncBaseResources(Dictionary<AttributeType, float> attributes)
    {
        attributes[AttributeType.MaxHealth] = attributes.GetValueOrDefault(AttributeType.MaxHealth);
    }

    private static void AddUniversalBaseAttributes(Dictionary<AttributeType, float> attributes)
    {
        attributes.TryAdd(
            AttributeType.Threat,
            EntityBaseAttributeHelper.GetBaseValueForAttribute(AttributeType.Threat));
    }

}
