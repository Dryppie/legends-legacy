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

        // First calculate all baseCombatAttributes based on BaseAttributes + Temporary modifiers (Equipment, Essences, etc.)

        // Then find the derivedAttributes based on all Primary Attributes, and add those to BaseCombatAttributes
        var derivedAttributes = CalculateSecondaryAttributesFromPrimaryAttributes(entity.BaseCombatAttributes);

        foreach (var (attributeType, attributeValue) in derivedAttributes)
        {
            entity.BaseCombatAttributes[attributeType] += attributeValue;
        }

        entity.BaseCombatAttributes[AttributeType.Health] = entity.BaseCombatAttributes[AttributeType.MaxHealth];
        entity.BaseCombatAttributes[AttributeType.Mana] = entity.BaseCombatAttributes[AttributeType.MaxMana];
    }

    /// <summary>
    /// This calculates the secondary attributes based on base primary attributes. They should then be added to the final list of BaseCombatAttributes
    /// </summary>
    /// <param name="baseAttributes"></param>
    /// <returns></returns>
    private static Dictionary<AttributeType, float> CalculateSecondaryAttributesFromPrimaryAttributes(IReadOnlyDictionary<AttributeType, float> baseAttributes)
    {

        var derived = new Dictionary<AttributeType, float>();

        float constitution = baseAttributes.GetValueOrDefault(AttributeType.Constitution);
        float endurance = baseAttributes.GetValueOrDefault(AttributeType.Endurance);
        float willpower = baseAttributes.GetValueOrDefault(AttributeType.Willpower);
        float strength = baseAttributes.GetValueOrDefault(AttributeType.Strength);
        float fightingSpirit = baseAttributes.GetValueOrDefault(AttributeType.FightingSpirit);
        float dexterity = baseAttributes.GetValueOrDefault(AttributeType.Dexterity);
        float agility = baseAttributes.GetValueOrDefault(AttributeType.Agility);
        float intelligence = baseAttributes.GetValueOrDefault(AttributeType.Intelligence);
        float wisdom = baseAttributes.GetValueOrDefault(AttributeType.Wisdom);
        float instinct = baseAttributes.GetValueOrDefault(AttributeType.Instinct);
        float perception = baseAttributes.GetValueOrDefault(AttributeType.Perception);
        float luck = baseAttributes.GetValueOrDefault(AttributeType.Luck);

        // ---------------------------------------------------------------------
        //  - 1 Constitution = 8 MaxHealth
        //  - 1 Endurance    = 2 MaxHealth
        // ---------------------------------------------------------------------
        float maxHealthFromCon = constitution * 8.0f;
        float maxHealthFromEnd = endurance * 2.0f;
        float totalMaxHealth = maxHealthFromCon + maxHealthFromEnd;

        // ---------------------------------------------------------------------
        //  - Every 10 Constitution = +2 HP Regen
        //  - Every 10 FightingSpirit = +1 HP Regen
        // ---------------------------------------------------------------------
        float hpRegenFromCon = (int)(constitution / 10 * 0.5f); // TODO: Revert back to 2.0f?
        float hpRegenFromSpirit = (int)(fightingSpirit / 10 * 0.2f); // TODO: Revert back to 1.0f?
        float totalHPRegen = hpRegenFromCon + hpRegenFromSpirit;

        // ---------------------------------------------------------------------
        //  - 1 Intelligence = 2 MaxMana
        //  - 1 Wisdom       = 1 MaxMana
        // ---------------------------------------------------------------------
        float maxManaFromInt = intelligence * 2.0f;
        float maxManaFromWis = wisdom * 1.0f;
        float totalMaxMana = maxManaFromInt + maxManaFromWis;

        // ---------------------------------------------------------------------
        //  - Every 10 Willpower = +2 MP Regen
        //  - Every 10 Wisdom    = +1 MP Regen
        // ---------------------------------------------------------------------
        float mpRegenFromWil = (int)(willpower / 10 * 0.5f); // TODO: Revert back to 2.0f?
        float mpRegenFromWis = (int)(wisdom / 10 * 0.2f); // TODO: Revert back to 1.0f?
        float totalMPRegen = mpRegenFromWil + mpRegenFromWis;

        // ---------------------------------------------------------------------
        //  - Every 12 Constitution = +1
        //  - Every 20 FightingSpirit = +1
        // ---------------------------------------------------------------------
        float ccResFromCon = (int)(constitution / 12);
        float ccResFromSpirit = (int)(fightingSpirit / 20);
        float totalCCResistance = ccResFromCon + ccResFromSpirit;

        // ---------------------------------------------------------------------
        //  - Every 5 Dexterity = +0.4% Crit Chance
        //  - Every 3 Perception = +0.1% Crit Chance
        //  - Every 3 Luck = +0.2% Crit Chance
        // ---------------------------------------------------------------------
        float critChance =
            ((int)(dexterity / 5) * 0.4f)
          + ((int)(perception / 3) * 0.1f)
          + ((int)(luck / 3) * 0.2f);

        // ---------------------------------------------------------------------
        //  - Every 10 Strength = +1% Crit Damage
        //  - Every 10 Intelligence = +1% Crit Damage
        //  - Every 10 Perception = +1% Crit Damage
        // ---------------------------------------------------------------------
        float critDamage =
            (int)(strength / 10)
          + (int)(intelligence / 10)
          + (int)(perception / 10);

        // ---------------------------------------------------------------------
        //  - Every 10 Agility = +1% Dodge
        //  - Every 10 Instinct = +1% Dodge
        // ---------------------------------------------------------------------
        float dodgeChance =
            (int)(agility / 10)
          + (int)(instinct / 10);

        // ---------------------------------------------------------------------
        //  - Every 25 Agility = +1 BasicAttackSpeed
        // ---------------------------------------------------------------------
        float basicAttackSpeed =
            (int)(agility / 25);

        // ---------------------------------------------------------------------
        //  - Every 4 Endurance = +3 Physical Defense
        // ---------------------------------------------------------------------
        float physicalDefense =
          + ((int)(endurance / 4) * 3.0f);

        // ---------------------------------------------------------------------
        //  - Every 4 Willpower = +3 Magical Defense
        // ---------------------------------------------------------------------
        float magicalDefense =
          + ((int)(willpower / 4) * 3.0f);

        // ---------------------------------------------------------------------
        //  - Every 10 Endurance = +0.5 Crit Damage Reduction
        //  - Every 10 Willpower = +0.5 Crit Damage Reduction
        // ---------------------------------------------------------------------
        float critDamageReduction =
          + ((int)(endurance / 10) * 0.5f)
          + ((int)(willpower / 10) * 0.5f);

        // ---------------------------------------------------------------------
        //  - Every 1 Strength = +1 Physical Defense
        // ---------------------------------------------------------------------
        float block =
          + (int)strength;

        // ---------------------------------------------------------------------
        //  - Every 4 Fighting Spirit = +1 Parry
        //  - Every 4 Dexterity = +1 Parry
        //  - Every 2 Instinct = +1 Parry
        // ---------------------------------------------------------------------
        float parry =
          + (int)(fightingSpirit / 4)
          + (int)(dexterity / 4)
          + (int)(instinct / 2);

        // ---------------------------------------------------------------------
        // Add these derived stats into entity.CombatAttributes
        // ---------------------------------------------------------------------
        derived.Add(AttributeType.MaxHealth, (int)totalMaxHealth);
        derived.Add(AttributeType.HealthRegeneration, (int)totalHPRegen);
        derived.Add(AttributeType.MaxMana, (int)totalMaxMana);
        derived.Add(AttributeType.ManaRegeneration, (int)totalMPRegen);
        derived.Add(AttributeType.CrowdControlResistance, totalCCResistance);
        derived.Add(AttributeType.CritChance, critChance);
        derived.Add(AttributeType.CritDamage, critDamage);
        derived.Add(AttributeType.Dodge, dodgeChance);
        derived.Add(AttributeType.BasicAttackSpeed, (int)basicAttackSpeed);
        derived.Add(AttributeType.PhysicalDefense, (int)physicalDefense);
        derived.Add(AttributeType.MagicalDefense, (int)magicalDefense);
        derived.Add(AttributeType.CritDamageReduction, critDamageReduction);
        derived.Add(AttributeType.Block, block);
        derived.Add(AttributeType.Parry, parry);

        return derived;
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

        var derivedAttributes = CalculateSecondaryAttributesFromPrimaryAttributes(entity.BaseCombatAttributes);

        // Iterate over each attribute of the entity, and add the derivedAttributes to the BaseCombatAttributes
        foreach (var (attributeType, attributeValue) in derivedAttributes)
        {
            entity.BaseCombatAttributes[attributeType] += attributeValue;
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