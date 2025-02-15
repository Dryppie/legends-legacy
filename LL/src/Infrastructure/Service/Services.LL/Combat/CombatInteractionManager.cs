using Domain.Helpers;
using Domain.Helpers.Constants;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;
using Domain.Models.Items.Equipments;

namespace Services.LL.Combat;
public class CombatInteractionManager : ICombatInteractionManager
{
    private readonly ICombatEffectManager _effectManager;

    public CombatInteractionManager(ICombatEffectManager effectManager)
    {
        _effectManager = effectManager;
    }

    public AttackOutcome CalculateAttackOutcomeForDamage(CombatEntity actor, CombatEntity target)
    {
        return CombatFormulaCalculator.CalculateAttackOutcome(actor, target, isDamage: true);
    }

    public AttackOutcome CalculateAttackOutcomeForHealing(CombatEntity actor, CombatEntity target)
    {
        return CombatFormulaCalculator.CalculateAttackOutcome(actor, target, isDamage: false);
    }

    public int CalculateDamageToDeal(CombatEntity actor, CombatEntity target, float magnitude, AttributeType scalingAttribute, float scalingMultiplier)
    {
        var finalDamage = magnitude + (actor.CombatAttributes[scalingAttribute] * scalingMultiplier);
        finalDamage = CombatConstants.GetRandomValue(finalDamage);

        return (int)finalDamage;
    }

    public int CalculateHealingToDeal(CombatEntity actor, CombatEntity target, float magnitude, AttributeType scalingAttribute, float scalingMultiplier)
    {
        var finalHealing = magnitude + (actor.CombatAttributes[scalingAttribute] * scalingMultiplier);
        finalHealing = CombatConstants.GetRandomValue(finalHealing);

        return (int)finalHealing;
    }

    public int CalculateDamageReceived(CombatEntity target, float magnitude, AttackOutcome attackOutcome)
    {
        if (attackOutcome.Equals(AttackOutcome.Crit)) return (int)(magnitude * target.CombatAttributes[AttributeType.CritDamageReduction]);
        return (int)magnitude;
    }

    public int CalculateHealingReceived(CombatEntity target, float magnitude, AttackOutcome attackOutcome)
    {
        // Increase / Decrease magnitude based on effects on the target, such as 20% increase healing received, and so on.

        return (int)magnitude;
    }

    public int CalculateBasicAttackDamage(CombatEntity actor, CombatEntity target, float baseDamage)
    {
        // Check equipment
        var weapon = actor.Equipment.FirstOrDefault(e => e.EquipmentType == EquipmentType.Weapon)
                     ?? new Weapon { DamageType = DamageType.Physical };

        var finalDamage = baseDamage + (actor.CombatAttributes[AttributeType.Strength] / 6); //TODO: Need proper calculation. Likely scaling of weapon
        finalDamage = CombatConstants.GetRandomValue(finalDamage);

        // Potential for more complex calculations, crit chance, etc.
        return (int)finalDamage;
    }

    public void ApplyDamage(EffectContext context)
    {
        context.Target.CombatAttributes[AttributeType.Health] -= context.Magnitude;

        // TODO: Make sure something like "Retaliate" is only triggered based on a specific TriggerEvent. And this effect should not return that specific TriggerEvent
        var attackTypeTrigger = TriggerEvent.None;
        attackTypeTrigger = context.Effect.Definition.AttackType switch
        {
            AttackType.Melee            => TriggerEvent.OnMeleeAttacked,
            AttackType.Ranged           => TriggerEvent.OnRangedAttacked,
            AttackType.DamageOverTime   => TriggerEvent.OnDamaged,
            _                           => TriggerEvent.OnDamaged
        };

        _effectManager.TriggerEffects(attackTypeTrigger, context.Target, context.Actor);

        _effectManager.TriggerEffects(TriggerEvent.OnHealthChanged, context.Target, context.Actor);

        // If target is dead
        if (!context.Target.IsAlive)
        {
            _effectManager.TriggerEffects(TriggerEvent.OnDeath, context.Target, context.Actor);
        }
    }

    public void ApplyHealing(EffectContext context)
    {
        var healing = context.Magnitude;

        float maxHealth = context.Target.CombatAttributes[AttributeType.MaxHealth];
        float currentHealth = context.Target.CombatAttributes[AttributeType.Health];

        // If overhealing occurs
        if (currentHealth + healing > maxHealth)
        {
            var extraHealing = (int)(currentHealth + healing - maxHealth);
            var actualHealing = (int)(maxHealth - currentHealth);
            // Either set Health = MaxHealth, or use 'actualHealing'
            // as that can also be used for a trigger effect (gain shield per x healing done)
            context.Target.CombatAttributes[AttributeType.Health] += actualHealing;

            // Trigger effects for overhealing
            _effectManager.TriggerEffects(TriggerEvent.OnOverhealed, context.Target, context.Actor, extraHealing);
        }
        else
        {
            context.Target.CombatAttributes[AttributeType.Health] += healing;

            // Trigger normal healing effects
            _effectManager.TriggerEffects(TriggerEvent.OnHealed, context.Target, context.Actor, healing);
        }
    }
}