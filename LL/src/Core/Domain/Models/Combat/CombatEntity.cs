using Domain.Components.Attributes;
using Domain.Extensions;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.StatusEffects;
using Domain.Models.Abilities.Statuses;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Entities;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Combat;
[NotMapped]
public class CombatEntity
{
    // This is only set to ensure it's possible to compare a CombatEntity with the LocationCreature
    public Guid OriginalId { get; set; }
    public string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public ICollection<Essence> EquippedEssences { get; set; } = [];
    public List<AbilityInstance> Abilities { get; set; } = [];
    public List<StatusInstance> Statuses { get; set; } = [];

    public Dictionary<StatusEffectType, int> StatusEffects { get; } = [];
    public int NextBasicAttackIn = 300; // TODO: Turn 300 into a Constant somewhere, as it is also stored in the CombatSimulator class
                                        // Every tick, this decrements by BaseAttackSpeed.
                                        // Start at 300. Whenever it is equal to or lower than 0, perform the attack.
                                        // If you increase attack speed by 100%, BasicAttackSpeed goes from 10 to 20,
                                        // and thust counting down faster to the next attack each tick
    public int NextRecoveryIn = 500; // This defines when the character regenerates health and mana.
    public ICollection<EntityAttribute> BaseAttributes { get; set; } = [];
    public bool IsAlive => CombatAttributes.FirstOrDefault(cm => cm.Key.Equals(AttributeType.Health)).Value > 0;
    public List<EquipmentInstance> Equipment { get; set; } = [];
    public Dictionary<AttributeType, float> BaseCombatAttributes { get; } = [];
    public Dictionary<AttributeType, float> CombatAttributes { get; } = [];
    public List<AttributeModifierBase> TemporaryModifiers { get; set; } = [];
    public int Level { get; set; }
    public bool IsSummoned = false;

    public CombatEntity(Entity entity)
    {
        OriginalId = entity.Id;
        Id = entity.Id.ToString();
        Name = entity.Name;
        ImagePath = entity.ImagePath;
        Abilities = [.. entity.Abilities];
        BaseAttributes = [.. entity.BaseAttributes];
        BaseCombatAttributes = new Dictionary<AttributeType, float>(entity.BaseCombatAttributes);
        CombatAttributes = new Dictionary<AttributeType, float>(entity.CombatAttributes);
        Equipment = entity.EquipmentSlots.Where(es => es.EquipmentInstance != null).Select(es => es.EquipmentInstance!).ToList();
        EquippedEssences = [.. entity.EssenceSlots.ActiveSlotsWithOccupiedEssences().Select(es => es.OccupiedEssence!)];
        Level = entity.Level;
        var mainHand = entity.EquipmentSlots.FirstOrDefault(es => es.EquipmentSlotType == EquipmentSlotType.MainHand);
        Abilities.Add(BasicAttackLoader.LoadBasicAttack(mainHand));
    }

    public void IncrementStep()
    {
        UpdateAbilities();
    }

    private void UpdateAbilities()
    {
        foreach (var ability in Abilities)
        {
            ability.Definition.Usage.Recharge();
            ability.RemainingTimeUntilUse--;
        }
    }

    public void ModifyAttribute(AttributeModifierBase attributeModifier, bool remove = false)
    {
        if (remove)
            TemporaryModifiers.Remove(attributeModifier);
        else
            TemporaryModifiers.Add(attributeModifier);

        AttributeCalculator.CalculateCombatAttributeByType(this, attributeModifier.AttributeType);
    }

    public void ModifyStatusEffects(StatusEffectType status, int amount)
    {
        if (amount == 0)
            return;

        // If we already have this status in the dictionary:
        if (StatusEffects.ContainsKey(status))
        {
            // Update its stack count
            StatusEffects[status] += amount;

            // If stacks drop to 0 or below, remove the status entirely
            if (StatusEffects[status] <= 0)
                StatusEffects.Remove(status);
        }
        else
        {
            // We only add if we have a positive amount
            if (amount > 0)
                StatusEffects[status] = amount;
        }
    }

    public void ApplyStatus(StatusInstance status)
    {
        if (!status.Definition.IsStackable)
        {
            var existing = Statuses.FirstOrDefault(s => s.Definition.Id == status.Definition.Id);
            if (existing != null)
            {
                // Replace or refresh (based on design)
                Statuses.Remove(existing);
            }
        }

        Statuses.Add(status);
    }

    public void RemoveStatus(StatusInstance status)
    {
        Statuses.Remove(status);
    }

    public bool CanAct()
    {
        return !StatusEffects.ContainsKey(StatusEffectType.Stunned) && !StatusEffects.ContainsKey(StatusEffectType.Frozen);
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
            ability.Definition.Usage.Reset();

            ability.RemainingTimeUntilUse = ability.Definition.Cooldown;

            foreach (var effect in ability.Definition.Triggers.SelectMany(t => t.Actions))
            {
                effect.Usage.Reset();
            }
        }

        TemporaryModifiers.Clear();
        Statuses.Clear();
        StatusEffects.Clear();
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

    public CombatEntity DeepCloneForEncounter()
    {
        return new CombatEntity(this);
    }

    private CombatEntity(CombatEntity entity)
    {
        OriginalId = entity.OriginalId;
        Id = entity.Id.ToString();
        Name = entity.Name;
        ImagePath = entity.ImagePath;
        Abilities = [.. entity.Abilities.Select(a => new AbilityInstance(a.Definition))];
        NextBasicAttackIn = entity.NextBasicAttackIn;
        NextRecoveryIn = entity.NextRecoveryIn;
        Equipment = entity.Equipment.Select(e => e).ToList();
        TemporaryModifiers = entity.TemporaryModifiers.Select(tm => tm).ToList();
        BaseAttributes = [.. entity.BaseAttributes];
        BaseCombatAttributes = new Dictionary<AttributeType, float>(entity.BaseCombatAttributes);
        CombatAttributes = new Dictionary<AttributeType, float>(entity.CombatAttributes);
        EquippedEssences = [.. entity.EquippedEssences];
        StatusEffects = new Dictionary<StatusEffectType, int>(entity.StatusEffects);
        Statuses = [];
        Level = entity.Level;
        IsSummoned = entity.IsSummoned;
    }
}