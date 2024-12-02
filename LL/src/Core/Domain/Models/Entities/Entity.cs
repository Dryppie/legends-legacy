using Domain.Components.Attributes;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Damages;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities;
public abstract class Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<EntityAttribute> BaseAttributes { get; set; } = [];
    public ICollection<Essence> EquippedEssences { get; set; } = [];
    [NotMapped]
    public List<string> AbilityIds { get; set; } = [];
    [NotMapped]
    public List<Ability> Abilities { get; set; } = [];
    [NotMapped]
    public int NextBasicAttackIn = 30; // Set this to be equal to BasicAttackSpeed at combat start
    public bool IsAlive => CombatAttributes.FirstOrDefault(cm => cm.Key.Equals(AttributeType.Health)).Value > 0;
    [NotMapped]
    public List<Equipment> Equipment { get; set; } = [];
    [NotMapped]
    public Dictionary<AttributeType, float> BaseCombatAttributes { get; } = [];
    [NotMapped]
    public Dictionary<AttributeType, float> CombatAttributes { get; } = [];
    [NotMapped]
    public List<AttributeModifier> TemporaryModifiers { get; set; } = [];
    [NotMapped]
    public List<Effect> ActiveEffects { get; } = [];
    [NotMapped]
    public HashSet<string> Statuses { get; } = [];
    [NotMapped]
    public int Level { get; set; } = 1;
    [NotMapped]
    public bool IsSummoned { get; set; } = false;

    public void AddEffect(Effect effect)
    {
        if (effect.Trigger == TriggerEvent.None)
        {
            // Create an EffectContext for immediate execution
            var context = CreateEffectContextFromEffect(effect);

            effect.ExecuteAction(context);
        }
        ActiveEffects.Add(effect);
    }

    public void IncrementStep()
    {
        UpdateAbilities();
        UpdateEffects();
    }

    private void UpdateAbilities()
    {
        foreach (var ability in Abilities)
        {
            ability.RemainingTimeUntilUse--;
        }
    }

    private void UpdateEffects()
    {
        foreach (var effect in ActiveEffects.ToList())
        {
            if (effect.Interval.ShouldTrigger())
            {
                var intervalContext = CreateEffectContextFromEffect(effect);

                effect.ExecuteAction(intervalContext);
            }

            if (!effect.Duration.IsActive())
            {
                var expireContext = CreateEffectContextFromEffect(effect);

                effect.ExecuteOnExpireAction(expireContext);

                ActiveEffects.Remove(effect);
            }

            effect.Update();
        }
    }

    public void TriggerEffects(TriggerEvent triggerEvent, Entity? opponent = null, int magnitude = 0)
    {
        foreach (var effect in ActiveEffects)
        {
            if (!effect.IsTrigger(triggerEvent)) continue;

            var context = CreateEffectContextFromEffect(effect);

            effect.ExecuteAction(context);
        }
    }

    public int BasicAttack(int damage)
    {
        var weapon = Equipment.FirstOrDefault(e => e.EquipmentType.Equals(EquipmentType.Weapon), new Weapon { DamageType = DamageType.Physical});

        damage += (int)CombatAttributes[AttributeType.Strength];
        return damage;
    }

    public int CalculateDamage(int damage)
    {
        damage += (int)CombatAttributes[AttributeType.Strength];
        return damage;
    }

    public int CalculateHealing(int healing)
    {
        return healing;
    }

    public int CalculateReceiveDamage(int damage)
    {
        // Add logic to calculate damage
        return damage;
    }

    public void PerformReceiveDamage(int damage, Entity attacker)
    {
        // Create proper formula for taking damage
        CombatAttributes[AttributeType.Health] -= damage;
        if ("MeleeAttack" == "Tag")
        {
            TriggerEffects(TriggerEvent.OnAttacked, attacker, damage);

        }
        TriggerEffects(TriggerEvent.OnHealthChanged, attacker, damage);

        if (!IsAlive)
        {
            TriggerEffects(TriggerEvent.OnDeath, attacker, damage);
        }
    }

    public int CalculateReceiveHealing(int healing)
    {
        // Add logic to calculate healing
        return healing;
    }

    public int PerformReceiveHealing(int healing)
    {
        if (CombatAttributes[AttributeType.Health] + healing > CombatAttributes[AttributeType.MaxHealth])
        {
            var extraHealing = (int)CombatAttributes[AttributeType.Health] + healing - (int)CombatAttributes[AttributeType.MaxHealth];
            healing = (int)CombatAttributes[AttributeType.MaxHealth] - (int)CombatAttributes[AttributeType.Health];

            TriggerEffects(TriggerEvent.OnOverhealed, magnitude: extraHealing);
            CombatAttributes[AttributeType.Health] = CombatAttributes[AttributeType.MaxHealth];
        }
        else
        {
            TriggerEffects(TriggerEvent.OnHealed, magnitude: healing);
            CombatAttributes[AttributeType.Health] += healing;
        }
        return healing;
    }

    public void ModifyAttribute(AttributeModifier attributeModifier, bool remove = false)
    {
        if (remove)
            TemporaryModifiers.Remove(attributeModifier);
        else
            TemporaryModifiers.Add(attributeModifier);

        AttributeCalculator.CalculateCombatAttributeByType(this, attributeModifier.AttributeType);
    }

    public void ModifyStatuses(string status, bool remove = false)
    {
        if (remove)
            Statuses.Remove(status);
        else
            Statuses.Add(status);
    }

    public bool CanAct()
    {
        return !Statuses.Contains("Stun");
    }

    public void Reset()
    {
        NextBasicAttackIn = 30;
        CombatAttributes.Clear();

        foreach (var kvp in BaseCombatAttributes)
        {
            CombatAttributes.Add(kvp.Key, kvp.Value);
        }

        foreach (var ability in Abilities)
        {
            ability.RemainingTimeUntilUse = ability.Cooldown;
        }

        TemporaryModifiers = [];
        ActiveEffects.Clear();
        Statuses.Clear();
    }

    public EffectContext CreateEffectContextFromEffect(Effect effect)
    {
        return new EffectContext(effect.Caster ?? this, this, effect.Trigger, effect.Action.Magnitude, effect.IsFlatAmount, effect.Log, effect.EffectModifications);
    }

    //public Actor DeepClone()
    //{
    //    // Create a new Actor instance
    //    var clone = new Actor
    //    {
    //        Level = this.Level,
    //        RawAttributes = new List<EntityAttribute>(this.RawAttributes.Select(attr => attr.Clone())), // Assuming EntityAttribute has a DeepClone method
    //        HP = HP,
    //        // Do not copy the AttackTimer directly as it is tied to specific event handlers, create a new one instead
    //        _modifiersHaveChanged = _modifiersHaveChanged,
    //        // _attributes are recalculated, so we don't clone them directly but let them be lazy-initialized
    //    };

    //    // Copy the AttackTimer's Elapsed event handlers, if necessary
    //    // Note: This could introduce side effects as the handlers are shared between the original and the clone
    //    // foreach (ElapsedEventHandler handler in this.AttackTimer.Elapsed.GetInvocationList())
    //    // {
    //    //     clone.AttackTimer.Elapsed += handler;
    //    // }

    //    return clone;
    //}
}