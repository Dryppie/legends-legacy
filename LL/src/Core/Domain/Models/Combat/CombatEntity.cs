using Domain.Components.Attributes;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects.StatusEffects;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Entities;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Combat;
[NotMapped]
public class CombatEntity
{
    public string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Essence> EquippedEssences { get; set; } = [];
    [NotMapped]
    public List<string> AbilityIds { get; set; } = [];
    public List<AbilityDefinition> Abilities { get; set; } = [];
    public int NextBasicAttackIn = 300; // TODO: Turn 300 into a Constant somewhere, as it is also stored in the CombatSimulator class
                                        // Every tick, this decrements by BaseAttackSpeed.
                                        // Start at 300. Whenever it is equal to or lower than 0, perform the attack.
                                        // If you increase attack speed by 100%, BasicAttackSpeed goes from 10 to 20,
                                        // and thust counting down faster to the next attack each tick
    public ICollection<EntityAttribute> BaseAttributes { get; set; } = [];
    public bool IsAlive => CombatAttributes.FirstOrDefault(cm => cm.Key.Equals(AttributeType.Health)).Value > 0;
    public List<Equipment> Equipment { get; set; } = [];
    public Dictionary<AttributeType, float> BaseCombatAttributes { get; } = [];
    public Dictionary<AttributeType, float> CombatAttributes { get; } = [];
    public List<AttributeModifier> TemporaryModifiers { get; set; } = [];
    public Dictionary<StatusEffectType, int> Statuses { get; } = [];
    public int Level { get; set; }
    public bool IsSummoned = false;

    public CombatEntity(Entity entity)
    {
        Id = entity.Id.ToString();
        Name = entity.Name;
        Abilities = new List<AbilityDefinition>(entity.Abilities);
        BaseAttributes = new List<EntityAttribute>(entity.BaseAttributes);
        BaseCombatAttributes = new Dictionary<AttributeType, float>(entity.BaseCombatAttributes);
        CombatAttributes = new Dictionary<AttributeType, float>(entity.CombatAttributes);
        EquippedEssences = new List<Essence>(entity.EquippedEssences);
        Level = entity.Level;
    }

    public void IncrementStep()
    {
        UpdateAbilities();
    }

    private void UpdateAbilities()
    {
        foreach (var ability in Abilities)
        {
            ability.Usage.Recharge();
            ability.RemainingTimeUntilUse--;
        }
    }

    public void ModifyAttribute(AttributeModifier attributeModifier, bool remove = false)
    {
        if (remove)
            TemporaryModifiers.Remove(attributeModifier);
        else
            TemporaryModifiers.Add(attributeModifier);

        AttributeCalculator.CalculateCombatAttributeByType(this, attributeModifier.AttributeType);
    }

    public void ModifyStatuses(StatusEffectType status, int amount)
    {
        if (amount == 0)
            return;

        // If we already have this status in the dictionary:
        if (Statuses.ContainsKey(status))
        {
            // Update its stack count
            Statuses[status] += amount;

            // If stacks drop to 0 or below, remove the status entirely
            if (Statuses[status] <= 0)
                Statuses.Remove(status);
        }
        else
        {
            // We only add if we have a positive amount
            if (amount > 0)
                Statuses[status] = amount;
        }
    }

    public bool CanAct()
    {
        return !Statuses.ContainsKey(StatusEffectType.Stunned) && !Statuses.ContainsKey(StatusEffectType.Frozen);
    }

    public void Reset()
    {
        NextBasicAttackIn = 0;
        CombatAttributes.Clear();

        foreach (var kvp in BaseCombatAttributes)
        {
            CombatAttributes.Add(kvp.Key, kvp.Value);
        }

        foreach (var ability in Abilities)
        {
            ability.Usage.Reset();

            ability.RemainingTimeUntilUse = ability.Cooldown;

            foreach (var effect in ability.Effects)
            {
                effect.Usage.Reset();
            }
        }

        TemporaryModifiers.Clear();
        Statuses.Clear();
    }

    /// <summary>
    /// Return the value of the attribute, or 0 if the attribute isn't found
    /// </summary>
    /// <param name="attributeType"></param>
    /// <returns></returns>
    public int GetAttributeValue(AttributeType attributeType)
    {
        return CombatAttributes.TryGetValue(attributeType, out var attributeValue) ? (int)attributeValue : 0;
    }

    /// <summary>
    /// Sets the value of the specified attribute, creating or updating it in the dictionary.
    /// Primarily used for Health and Mana
    /// </summary>
    /// <param name="attributeType">The type of attribute to set.</param>
    /// <param name="value">The new value to set for the attribute.</param>
    public void SetAttributeValue(AttributeType attributeType, int value)
    {
        CombatAttributes[attributeType] = value;
    }
}