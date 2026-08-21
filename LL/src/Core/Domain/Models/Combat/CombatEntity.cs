using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Entities;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
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
    public string SourceMonsterId { get; set; } = string.Empty;
    public List<string> NativeAbilityIds { get; set; } = [];
    public HashSet<string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public int NextBasicAttackIn = 300; // TODO: Turn 300 into a Constant somewhere, as it is also stored in the CombatSimulator class
                                        // Every tick, this decrements by BaseAttackSpeed.
                                        // Start at 300. Whenever it is equal to or lower than 0, perform the attack.
                                        // If you increase attack speed by 100%, BasicAttackSpeed goes from 10 to 20,
                                        // and thust counting down faster to the next attack each tick
    public ICollection<EntityAttribute> BaseAttributes { get; set; } = [];
    public float CurrentHealth { get; private set; }
    public float CurrentBarrier { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public List<EquipmentInstance> Equipment { get; set; } = [];
    public EquipmentInstance? MainHandEquipment { get; set; }
    public EquipmentInstance? OffHandEquipment { get; set; }
    public List<PlayerEssence> EquippedEssences { get; set; } = [];
    public bool HasEquippedEssenceSnapshot { get; set; }
    public Dictionary<AttributeType, float> BaseCombatAttributes { get; } = [];
    public Dictionary<AttributeType, float> CombatAttributes { get; } = [];
    public List<AttributeModifierBase> TemporaryModifiers { get; set; } = [];
    public List<EssenceAbilityModifierDefinition> TemporaryAbilityModifiers { get; set; } = [];
    public BossStaggerDefinition? StaggerDefinition { get; set; }
    public int StaggerParticipantCount { get; set; } = 1;
    public int Level { get; set; }
    public bool IsSummoned = false;

    public CombatEntity(Entity entity)
    {
        OriginalId = entity.Id;
        Id = entity.Id.ToString();
        Name = entity.Name;
        ImagePath = entity.ImagePath;
        BaseAttributes = [.. entity.BaseAttributes];
        BaseCombatAttributes = new Dictionary<AttributeType, float>(entity.BaseCombatAttributes);
        CombatAttributes = new Dictionary<AttributeType, float>(entity.CombatAttributes);
        CurrentHealth = entity.CombatAttributes.GetValueOrDefault(
            AttributeType.MaxHealth,
            entity.BaseCombatAttributes.GetValueOrDefault(AttributeType.MaxHealth));
        Equipment = entity.EquipmentSlots
            .Where(es => es.EquipmentInstance != null)
            .Select(es => es.EquipmentInstance!)
            .DistinctBy(equipment => equipment.Id)
            .ToList();
        MainHandEquipment = entity.EquipmentSlots
            .FirstOrDefault(slot => slot.EquipmentSlotType == EquipmentSlotType.MainHand)
            ?.EquipmentInstance;
        OffHandEquipment = entity.EquipmentSlots
            .FirstOrDefault(slot => slot.EquipmentSlotType == EquipmentSlotType.OffHand)
            ?.EquipmentInstance;
        Level = entity.Level;
    }

    public void ModifyAttribute(AttributeModifierBase attributeModifier, bool remove = false)
    {
        if (remove)
            TemporaryModifiers.Remove(attributeModifier);
        else
            TemporaryModifiers.Add(attributeModifier);

        AttributeCalculator.CalculateCombatAttributeByType(this, attributeModifier.AttributeType);
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

        TemporaryModifiers.Clear();
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
        NextBasicAttackIn = entity.NextBasicAttackIn;
        Equipment = entity.Equipment.Select(e => e).ToList();
        MainHandEquipment = entity.MainHandEquipment;
        OffHandEquipment = entity.OffHandEquipment;
        TemporaryModifiers = entity.TemporaryModifiers.Select(tm => tm).ToList();
        TemporaryAbilityModifiers = entity.TemporaryAbilityModifiers.Select(tm => tm).ToList();
        StaggerDefinition = entity.StaggerDefinition;
        StaggerParticipantCount = entity.StaggerParticipantCount;
        BaseAttributes = [.. entity.BaseAttributes];
        BaseCombatAttributes = new Dictionary<AttributeType, float>(entity.BaseCombatAttributes);
        CombatAttributes = new Dictionary<AttributeType, float>(entity.CombatAttributes);
        CurrentHealth = entity.CurrentHealth;
        CurrentBarrier = entity.CurrentBarrier;
        SourceMonsterId = entity.SourceMonsterId;
        Tags = new HashSet<string>(entity.Tags, StringComparer.OrdinalIgnoreCase);
        EquippedEssences = [.. entity.EquippedEssences];
        NativeAbilityIds = [.. entity.NativeAbilityIds];
        HasEquippedEssenceSnapshot = entity.HasEquippedEssenceSnapshot;
        Level = entity.Level;
        IsSummoned = entity.IsSummoned;
    }
}
