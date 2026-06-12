using Domain.Components.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Combat.Abilities.Statuses;
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
    public List<CombatAbilityInstance> Abilities { get; set; } = [];
    public List<StatusInstance> Statuses { get; set; } = [];
    public string SourceMonsterId { get; set; } = string.Empty;
    public HashSet<string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<StatusEffectType, int> StatusEffects { get; } = [];
    public int NextBasicAttackIn = 300; // TODO: Turn 300 into a Constant somewhere, as it is also stored in the CombatSimulator class
                                        // Every tick, this decrements by BaseAttackSpeed.
                                        // Start at 300. Whenever it is equal to or lower than 0, perform the attack.
                                        // If you increase attack speed by 100%, BasicAttackSpeed goes from 10 to 20,
                                        // and thust counting down faster to the next attack each tick
    public int NextRecoveryIn = 500; // This defines when the character regenerates health.
    public ICollection<EntityAttribute> BaseAttributes { get; set; } = [];
    public float CurrentHealth { get; private set; }
    public float CurrentBarrier { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public List<EquipmentInstance> Equipment { get; set; } = [];
    public List<PlayerEssence> EquippedEssences { get; set; } = [];
    public bool HasEquippedEssenceSnapshot { get; set; }
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
        CurrentHealth = entity.CombatAttributes.GetValueOrDefault(
            AttributeType.MaxHealth,
            entity.BaseCombatAttributes.GetValueOrDefault(AttributeType.MaxHealth));
        Equipment = entity.EquipmentSlots.Where(es => es.EquipmentInstance != null).Select(es => es.EquipmentInstance!).ToList();
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

        SetCurrentHealth(GetAttributeValue(AttributeType.MaxHealth));
        SetCurrentBarrier(0);

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

    public int GetCurrentHealthValue()
    {
        return (int)CurrentHealth;
    }

    public int GetCurrentBarrierValue()
    {
        return (int)CurrentBarrier;
    }

    public void SetCurrentHealth(float value)
    {
        CurrentHealth = Math.Clamp(value, 0, GetAttributeValue(AttributeType.MaxHealth));
    }

    public void AdjustCurrentHealth(float amount)
    {
        SetCurrentHealth(CurrentHealth + amount);
    }

    public void SetCurrentBarrier(float value)
    {
        CurrentBarrier = Math.Max(0, value);
    }

    public void AdjustCurrentBarrier(float amount)
    {
        SetCurrentBarrier(CurrentBarrier + amount);
    }

    public void SyncCurrentHealthToMax()
    {
        SetCurrentHealth(GetAttributeValue(AttributeType.MaxHealth));
    }

    public void SyncCurrentHealthAfterMaxHealthChange(float oldMaxHealth, float newMaxHealth)
    {
        if (oldMaxHealth <= 0)
        {
            SetCurrentHealth(newMaxHealth);
            return;
        }

        if (CurrentHealth <= 0)
        {
            SetCurrentHealth(0);
            return;
        }

        if (newMaxHealth > oldMaxHealth)
        {
            AdjustCurrentHealth(newMaxHealth - oldMaxHealth);
            return;
        }

        SetCurrentHealth(CurrentHealth);
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
        Abilities = [.. entity.Abilities.Select(a => new CombatAbilityInstance(a.Definition))];
        NextBasicAttackIn = entity.NextBasicAttackIn;
        NextRecoveryIn = entity.NextRecoveryIn;
        Equipment = entity.Equipment.Select(e => e).ToList();
        TemporaryModifiers = entity.TemporaryModifiers.Select(tm => tm).ToList();
        BaseAttributes = [.. entity.BaseAttributes];
        BaseCombatAttributes = new Dictionary<AttributeType, float>(entity.BaseCombatAttributes);
        CombatAttributes = new Dictionary<AttributeType, float>(entity.CombatAttributes);
        CurrentHealth = entity.CurrentHealth;
        CurrentBarrier = entity.CurrentBarrier;
        StatusEffects = new Dictionary<StatusEffectType, int>(entity.StatusEffects);
        Statuses = [];
        SourceMonsterId = entity.SourceMonsterId;
        Tags = new HashSet<string>(entity.Tags, StringComparer.OrdinalIgnoreCase);
        EquippedEssences = [.. entity.EquippedEssences];
        HasEquippedEssenceSnapshot = entity.HasEquippedEssenceSnapshot;
        Level = entity.Level;
        IsSummoned = entity.IsSummoned;
    }
}
