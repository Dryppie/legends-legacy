using Domain.Helpers;
using Domain.Helpers.Constants;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.EffectModifications;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;
using Domain.Models.Items.Equipments;
using static System.Net.Mime.MediaTypeNames;

namespace Services.LL.Combat;
public class CombatInteractionManager : ICombatInteractionManager
{
    private readonly ICombatEffectManager _effectManager;

    public CombatInteractionManager(ICombatEffectManager effectManager)
    {
        _effectManager = effectManager;
    }

    public AttackOutcome CalculateAttackOutcomeForDamage(CombatEntity actor, CombatEntity target, List<EffectModification> effectModifications)
    {
        return CombatFormulaCalculator.CalculateAttackOutcome(actor, target, effectModifications, isDamage: true);
    }

    public AttackOutcome CalculateAttackOutcomeForHealing(CombatEntity actor, CombatEntity target, List<EffectModification> effectModifications)
    {
        return CombatFormulaCalculator.CalculateAttackOutcome(actor, target, effectModifications, isDamage: false);
    }

    public int CalculateDamageToDeal(CombatEntity actor, CombatEntity target, float magnitude, AttackOutcome attackOutcome, AttributeType scalingAttribute, float scalingMultiplier)
    {
        var finalDamage = magnitude + (actor.CombatAttributes[scalingAttribute] * scalingMultiplier);
        if (attackOutcome.Equals(AttackOutcome.Crit))
            finalDamage = finalDamage * (1 + (actor.CombatAttributes[AttributeType.CritDamage] / 100));

        finalDamage = CombatConstants.GetRandomValue(finalDamage);

        return (int)finalDamage;
    }

    public int CalculateHealingToDeal(CombatEntity actor, CombatEntity target, float magnitude, AttackOutcome attackOutcome, AttributeType scalingAttribute, float scalingMultiplier)
    {
        var finalHealing = magnitude + (actor.CombatAttributes[scalingAttribute] * scalingMultiplier);

        if (attackOutcome.Equals(AttackOutcome.Crit))
            finalHealing = finalHealing * (1 + (actor.CombatAttributes[AttributeType.CritDamage] / 100));

        finalHealing = CombatConstants.GetRandomValue(finalHealing);

        return (int)finalHealing;
    }

    public int CalculateDamageReceived(CombatEntity target, float magnitude, AttackOutcome attackOutcome)
    {
        magnitude -= target.CombatAttributes[AttributeType.FlatDamageReduction];

        magnitude = GoThroughBarrier(target, magnitude);

        if (attackOutcome == AttackOutcome.Crit)
        {
            float critReductionMultiplier = 1 - (target.CombatAttributes[AttributeType.CritDamageReduction] / 100f);
            critReductionMultiplier = Math.Clamp(critReductionMultiplier, 0, 1); // Ensure it doesn't go below 0
            return (int)(magnitude * critReductionMultiplier);
        }

        return (int)Math.Max(magnitude, 0);
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
        var target = context.Target;
        var damage = context.Magnitude;

        target.CombatAttributes[AttributeType.Health] -= damage;

        // TODO: Make sure something like "Retaliate" is only triggered based on a specific TriggerEvent. And this effect should not return that specific TriggerEvent
        var attackTypeTrigger = TriggerEvent.None;
        attackTypeTrigger = context.Effect.Definition.AttackType switch
        {
            AttackType.Melee            => TriggerEvent.OnMeleeAttacked,
            AttackType.Ranged           => TriggerEvent.OnRangedAttacked,
            AttackType.DamageOverTime   => TriggerEvent.OnDamaged,
            _                           => TriggerEvent.OnDamaged
        };

        _effectManager.TriggerEffects(attackTypeTrigger, target, context.Actor);
        _effectManager.TriggerEffects(TriggerEvent.OnAttacked, target, context.Actor);

        if (damage > 0) _effectManager.TriggerEffects(TriggerEvent.OnHealthChanged, target, context.Actor);

        // If target is dead
        if (!target.IsAlive)
        {
            _effectManager.TriggerEffects(TriggerEvent.OnDeath, target, context.Actor);
        }
    }

    private float GoThroughBarrier(CombatEntity target, float damage)
    {
        if (target.CombatAttributes.TryGetValue(AttributeType.Barrier, out var barrier) && barrier > 0)
        {
            if (damage >= barrier)
            {
                damage -= barrier;
                target.CombatAttributes[AttributeType.Barrier] = 0;
            }
            else
            {
                target.CombatAttributes[AttributeType.Barrier] -= damage;
                damage = 0;
            }
        }
        return damage;
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