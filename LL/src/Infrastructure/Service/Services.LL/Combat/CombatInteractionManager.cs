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

    public int CalculateHealingToDeal(CombatEntity actor, CombatEntity target, float magnitude, AttackOutcome attackOutcome, AttributeType? scalingAttribute, float scalingMultiplier)
    {
        var finalHealing = magnitude;
        if (scalingAttribute != null)
            finalHealing += actor.CombatAttributes[scalingAttribute.Value] * scalingMultiplier;

        if (attackOutcome.Equals(AttackOutcome.Crit))
            finalHealing = finalHealing * (1 + (actor.CombatAttributes[AttributeType.CritDamage] / 100));

        finalHealing = CombatConstants.GetRandomValue(finalHealing);

        return (int)finalHealing;
    }

    public DamageResult CalculateDamageBreakdown(CombatEntity target, float baseDamage, AttackOutcome outcome, DamageType damageType)
    {
        // Subtract flat damage reduction
        baseDamage -= target.CombatAttributes[AttributeType.FlatDamageReduction];

        // Subtract damage if it's blocked or parried
        if (outcome == AttackOutcome.Block)
            baseDamage = baseDamage * (1 - CombatConstants.BLOCK_DAMAGE_DECREASE);
        if (outcome == AttackOutcome.Parry)
            baseDamage = baseDamage * (1 - CombatConstants.PARRY_DAMAGE_DECREASE);

        baseDamage *= 1 - (target.CombatAttributes[AttributeType.DamageReduction] / 100f);

        // TODO: Add damage reduction based on bleed, burn, and poison as well. (Bleed and burn physical, poison magical)
        var defenseValue = damageType switch
        {
            DamageType.Physical => target.GetAttributeValue(AttributeType.PhysicalDefense),
            DamageType.Magical => target.GetAttributeValue(AttributeType.MagicalDefense),
            _ => 0
        };

        var damageReductionFromArmor = ArmorDamageReductionConstants.CalculateEffectiveDefense(defenseValue, 0);
        baseDamage = (int)Math.Round(baseDamage * (1 - damageReductionFromArmor), 0);

        // Store total so you can display it later (in case it's partially blocked by barrier)
        //    Make sure it isn't negative here.
        int totalSoFar = (int)Math.Max(baseDamage, 0);

        // Let the barrier do its work, but keep track of how much barrier absorbed
        float damageAfterBarrier = GoThroughBarrier(target, baseDamage, out float barrierAbsorbed);

        // If crit, apply crit damage reduction
        bool isCrit = (outcome == AttackOutcome.Crit);
        if (isCrit)
        {
            float critReductionMultiplier = 1 - (target.CombatAttributes[AttributeType.CritDamageReduction] / 100f);
            critReductionMultiplier = Math.Clamp(critReductionMultiplier, 0, 1);
            damageAfterBarrier *= critReductionMultiplier;
        }

        // Final health damage cannot go below zero
        int healthDamage = (int)Math.Max(damageAfterBarrier, 0);

        return new DamageResult
        {
            TotalDamage = totalSoFar,
            BarrierAbsorbed = (int)barrierAbsorbed,
            HealthDamage = healthDamage,
            IsCrit = isCrit
        };
    }

    private float GoThroughBarrier(CombatEntity target, float incomingDamage, out float absorbedByBarrier)
    {
        absorbedByBarrier = 0;

        // If no barrier, just return
        if (!target.CombatAttributes.TryGetValue(AttributeType.Barrier, out float barrier) || barrier <= 0)
            return incomingDamage;

        if (incomingDamage >= barrier)
        {
            // Barrier fully consumed
            absorbedByBarrier = barrier;
            target.CombatAttributes[AttributeType.Barrier] = 0;
            return incomingDamage - barrier;
        }
        else
        {
            // Barrier partially absorbs damage, but still remains
            absorbedByBarrier = incomingDamage;
            target.CombatAttributes[AttributeType.Barrier] = barrier - incomingDamage;
            return 0;
        }
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