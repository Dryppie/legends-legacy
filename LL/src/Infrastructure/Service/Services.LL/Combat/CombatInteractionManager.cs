using Domain.Helpers;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;
using Domain.Models.Entities;
using Domain.Models.Items.Equipments;

namespace Services.LL.Combat;
public class CombatInteractionManager : ICombatInteractionManager
{
    private readonly ICombatEffectManager _effectManager;

    public CombatInteractionManager(ICombatEffectManager effectManager)
    {
        _effectManager = effectManager;
    }

    public int CalculateDamageToDeal(Entity attacker, Entity defender, float magnitude)
    {
        CombatFormulaCalculator.CalculateAttackOutcome(attacker, defender);
        int finalDamage = (int)magnitude + (int)attacker.CombatAttributes[AttributeType.Strength];
        // More logic if needed: defense reduction, armor, etc.
        return finalDamage;
    }

    public int CalculateDamageReceived(Entity defender, float magnitude, AttackOutcome attackOutcome)
    {
        if (attackOutcome.Equals(AttackOutcome.Block)) return (int)(magnitude * 0.6f);
        if (attackOutcome.Equals(AttackOutcome.Crit)) return (int)(magnitude * defender.CombatAttributes[AttributeType.CritDamageReduction]);
        return (int)magnitude;
    }

    public int CalculateBasicAttackDamage(Entity attacker, float baseDamage)
    {
        // Check equipment
        var weapon = attacker.Equipment.FirstOrDefault(e => e.EquipmentType == EquipmentType.Weapon)
                     ?? new Weapon { DamageType = DamageType.Physical };

        // Example calculation:
        int finalDamage = (int)baseDamage + (int)attacker.CombatAttributes[AttributeType.Strength];
        // Potential for more complex calculations, crit chance, etc.
        return finalDamage;
    }

    public void ApplyDamage(Entity attacker, Entity target, float damage)
    {
        target.CombatAttributes[AttributeType.Health] -= damage;

        
        // TODO: Make sure something like "Retaliate" is only triggered based on a specific TriggerEvent. And this effect should not return that specific TriggerEvent
        //if (attackType == "MeleeAttack") { ... }
        _effectManager.TriggerEffects(TriggerEvent.OnAttacked, target, attacker);
        _effectManager.TriggerEffects(TriggerEvent.OnHealthChanged, target, attacker);

        // If target is dead
        if (!target.IsAlive)
        {
            _effectManager.TriggerEffects(TriggerEvent.OnDeath, target, attacker);
        }
    }

    public int CalculateHealingToDo(Entity healer, Entity target, float baseHealing)
    {
        // Possibly scale with healer's attributes
        return (int)baseHealing;
    }

    public int CalculateHealingReceived(Entity healer, Entity target, float baseHealing)
    {
        return (int)baseHealing;
    }

    public void ApplyHealing(Entity healer, Entity target, float healing)
    {
        float maxHealth = target.CombatAttributes[AttributeType.MaxHealth];
        float currentHealth = target.CombatAttributes[AttributeType.Health];

        // If overhealing occurs
        if (currentHealth + healing > maxHealth)
        {
            int extraHealing = (int)(currentHealth + healing - maxHealth);
            int actualHealing = (int)(maxHealth - currentHealth);

            target.CombatAttributes[AttributeType.Health] = maxHealth;

            // Trigger effects for overhealing
            _effectManager.TriggerEffects(TriggerEvent.OnOverhealed, target, healer, extraHealing);
        }
        else
        {
            target.CombatAttributes[AttributeType.Health] += healing;

            // Trigger normal healing effects
            _effectManager.TriggerEffects(TriggerEvent.OnHealed, target, healer, (int)healing);
        }
    }
}