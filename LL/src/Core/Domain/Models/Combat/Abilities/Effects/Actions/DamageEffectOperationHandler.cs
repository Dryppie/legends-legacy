using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities.Effects.Trigger;
using Domain.Models.Damages;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class DamageEffectOperationHandler : ICombatEffectOperationHandler
{
    public string Operation => CombatEffectOperation.Damage;

    public void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        var attackOutcome = AttackOutcome.Miss;
        var damageAmount = action.Magnitude;
        var eventType = effect.AttackType == AttackType.DamageOverTime
            ? EventType.DamageOverTime
            : EventType.Damage;

        if (eventType != EventType.DamageOverTime)
        {
            attackOutcome = combatContext.InteractionManager.CalculateAttackOutcomeForDamage(effect.Source, effect.Target, effect.EffectModifications);

            if (attackOutcome == AttackOutcome.Miss)
            {
                effect.EventType = EventType.Miss;
                effect.Details = $"{effect.Source.Name} missed the target.";
                combatContext.LogEffectExecution(effect);
                combatContext.EventBus.Publish(new CombatEvent
                {
                    Type = TriggerEvent.OnDodge,
                    Source = effect.Target,
                    Target = effect.Source,
                    CurrentTime = combatContext.CurrentTime
                });
                return;
            }

            damageAmount = combatContext.InteractionManager.CalculateDamageToDeal(
                effect.Source,
                effect.Target,
                action.Magnitude,
                attackOutcome,
                action.ScalingAttribute,
                action.ScalingMultiplier);
        }

        var damageResult = combatContext.InteractionManager.CalculateDamageBreakdown(effect.Target, damageAmount, attackOutcome, effect.DamageType);

        effect.AttackOutcome = attackOutcome;
        effect.Magnitude = damageResult.HealthDamage;
        effect.EventType = eventType;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Amount}", CombatEffectActionHelpers.FormatDamageAmount(damageResult, attackOutcome));

        var simpleTarget = CombatEffectActionHelpers.CreateSimpleCombatEntity(effect.Target);
        simpleTarget.Health = Math.Max(0, simpleTarget.Health - damageResult.HealthDamage);
        combatContext.LogEffectExecution(effect, simpleTarget);

        combatContext.InteractionManager.ApplyDamage(effect.Source, effect.Target, damageResult.HealthDamage, effect.AttackType);
        ApplyLifeSteal(action, effect, combatContext, damageResult.HealthDamage);
    }

    public void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
    }

    private static void ApplyLifeSteal(CombatEffectAction action, EffectContext effect, ICombatContext combatContext, int finalDamageDealt)
    {
        if (action.LifeStealPercentage <= 0 || finalDamageDealt <= 0) return;

        var lifeStolen = (int)(finalDamageDealt * (action.LifeStealPercentage / 100));
        if (lifeStolen <= 0) return;

        effect.Magnitude = lifeStolen;
        effect.Target = effect.Source;
        effect.EventType = EventType.Heal;
        effect.Details = $"{effect.Source.Name} restored {lifeStolen} health through lifesteal.";
        combatContext.InteractionManager.ApplyHealing(effect);
        combatContext.LogEffectExecution(effect, CombatEffectActionHelpers.CreateSimpleCombatEntity(effect.Source));
        combatContext.EventBus.Publish(new CombatEvent { Type = TriggerEvent.OnLifestealHeal, Source = effect.Source });
    }
}
