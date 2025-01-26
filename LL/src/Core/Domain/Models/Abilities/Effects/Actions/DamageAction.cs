using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class DamageAction : IEffectAction
{
    private readonly int _damageAmount;
    public int Magnitude => _damageAmount;
    public AttributeType? ScalingAttribute { get; set; }
    public float ScalingMultiplier { get; set; }

    public DamageAction(int damageAmount, AttributeType? scalingAttribute, float scalingMultiplier)
    {
        _damageAmount = damageAmount;
        this.ScalingAttribute = scalingAttribute;
        ScalingMultiplier = scalingMultiplier;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {   
        // Attack outcome
        var attackOutcome = combatContext.InteractionManager.CalculateAttackOutcomeForDamage(context.Actor, context.Target);
        if (attackOutcome == AttackOutcome.Miss)
        {
            context.EventType = EventType.Miss;
            context.Details = $"{context.Actor?.Name!} missed the target.";
            // Log
            combatContext.LogEffectExecution(context);
            return;
        }

        // Potential damage to deal
        var isFlatAmount = context.Effect.Definition.IsFlatAmount;
        var damageAmount = isFlatAmount ? Magnitude : combatContext.InteractionManager.CalculateDamageToDeal(context.Actor, context.Target, Magnitude);

        // Damage opponent will receive
        var damageReceived = combatContext.InteractionManager.CalculateDamageReceived(context.Target, damageAmount, attackOutcome);

        context.AttackOutcome = attackOutcome;
        context.Magnitude = damageReceived;
        context.EventType = EventType.Damage;
        context.Details = context.Details
            .Replace("{Actor}", context.Actor.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", context.Magnitude.ToString());

        combatContext.LogEffectExecution(context);

        combatContext.InteractionManager.ApplyDamage(context);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        // Do nothing
    }
}