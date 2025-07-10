using Domain.Helpers;
using Domain.Helpers.Constants;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.EffectModifications;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Damages;

namespace Services.LL.Combat;
public class CombatInteractionManager : ICombatInteractionManager
{
    private readonly ICombatContext _combatContext;

    public CombatInteractionManager(ICombatContext combatContext)
    {
        _combatContext = combatContext;
    }

    public AttackOutcome CalculateAttackOutcomeForDamage(CombatEntity actor, CombatEntity target, List<EffectModification> effectModifications)
    {
        return CombatFormulaCalculator.CalculateAttackOutcome(actor, target, effectModifications, isDamage: true);
    }

    public AttackOutcome CalculateAttackOutcomeForHealing(CombatEntity actor, CombatEntity target, List<EffectModification> effectModifications)
    {
        return CombatFormulaCalculator.CalculateAttackOutcome(actor, target, effectModifications, isDamage: false);
    }

    public int CalculateDamageToDeal(CombatEntity actor, CombatEntity target, float magnitude, AttackOutcome attackOutcome, AttributeType? scalingAttribute, float scalingMultiplier)
    {
        var finalDamage = magnitude + (scalingAttribute.HasValue ? (actor.CombatAttributes[scalingAttribute.Value] * scalingMultiplier) : 0);
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
        //var weapon = actor.Equipment.FirstOrDefault(e => e.EquipmentBase.EquipmentType == EquipmentType.MainHand)
        //             ?? new Weapon { DamageType = DamageType.Physical };

        var finalDamage = baseDamage + (actor.CombatAttributes[AttributeType.AttackPower] / 6); //TODO: Need proper calculation. Likely scaling of weapon
        finalDamage = CombatConstants.GetRandomValue(finalDamage);

        // Potential for more complex calculations, crit chance, etc.
        return (int)finalDamage;
    }

    public void ApplyDamage(CombatEntity source, CombatEntity target, int damage, AttackType attackType)
    {

        target.CombatAttributes[AttributeType.Health] -= damage;

        // TODO: Make sure something like "Retaliate" is only triggered based on a specific TriggerEvent. And this effect should not return that specific TriggerEvent

        var attackTypeTrigger = attackType switch
        {
            AttackType.Melee => TriggerEvent.OnMeleeAttack,
            AttackType.Ranged => TriggerEvent.OnRangedAttack,
            _ => TriggerEvent.None
        };

        var attackedTypeTrigger = attackType switch
        {
            AttackType.Melee => TriggerEvent.OnMeleeAttacked,
            AttackType.Ranged => TriggerEvent.OnRangedAttacked,
            AttackType.DamageOverTime => TriggerEvent.OnDamaged,
            _ => TriggerEvent.OnDamaged
        };
        if (!attackTypeTrigger.Equals(TriggerEvent.None))
        {
            _combatContext.EventBus.Publish(new CombatEvent
            {
                Type = attackTypeTrigger,
                Source = source,
                Target = target,
            });
        }

        _combatContext.EventBus.Publish(new CombatEvent
        {
            Type = attackedTypeTrigger,
            Source = target,
            Target = source,
        });

        // Target can only be attacked, if the actor is different from the target, and it is either Melee or Ranged
        if (source.Id != target.Id && (attackType.Equals(AttackType.Melee) || attackedTypeTrigger.Equals(AttackType.Ranged)))
            _combatContext.EventBus.Publish(new CombatEvent
            {
                Type = TriggerEvent.OnAttacked,
                Source = target,
                Target = source,
            });

        if (damage > 0) _combatContext.EventBus.Publish(new CombatEvent
            {
                Type = TriggerEvent.OnHealthChanged,
                Source = target,
                Target = source,
            });

        // If target is dead
        if (!target.IsAlive)
        {
            // TODO: Fix setting the ability name to the effect context
            _combatContext.LogEffectExecution(new EffectContext("", source, target, attackType, [], $"{target.Name} was killed by {source.Name}") { EventType = EventType.Death });

            _combatContext.EventBus.Publish(new CombatEvent
            {
                Type = TriggerEvent.OnKill,
                Source = source,
                Target = target,
            });

            _combatContext.EventBus.Publish(new CombatEvent
            {
                Type = TriggerEvent.OnDeath,
                Source = target,
                Target = source,
            });
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
            _combatContext.EventBus.Publish(new CombatEvent
            {
                Type = TriggerEvent.OnOverhealed,
                Source = context.Source,
                Target = context.Target,
            });
        }
        else
        {
            context.Target.CombatAttributes[AttributeType.Health] += healing;

            // Trigger normal healing effects
            _combatContext.EventBus.Publish(new CombatEvent
            {
                Type = TriggerEvent.OnHealed,
                Source = context.Source,
                Target = context.Target,
            });
        }
    }
}