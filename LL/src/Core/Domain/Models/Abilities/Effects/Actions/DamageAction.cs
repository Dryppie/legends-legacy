using Domain.Helpers;
using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class DamageAction : IEffectAction
{
    private readonly int _damageAmount;
    public int Magnitude => _damageAmount;
    public AttributeType? DamageScalingAttribute { get; set; }
    public float DamageScalingMultiplier { get; set; }

    public DamageAction(int damageAmount, AttributeType? damageScalingAttribute, float damageScalingMultiplier)
    {
        _damageAmount = damageAmount;
        DamageScalingAttribute = damageScalingAttribute;
        DamageScalingMultiplier = damageScalingMultiplier;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        var calculatedResult = new CalculatedResult();
        if (context.IsFlatAmount)
        {
            // If it's a flat amount, take the value of the effect itself (_damageAmount)
            calculatedResult.CalculatedDamageToDeal = Magnitude;
            calculatedResult.CalculatedDamageReceived = combatContext.InteractionManager.CalculateDamageReceived(context.Target, Magnitude, AttackOutcome.Hit);
        }
        else
        {
            calculatedResult = CombatFormulaCalculator.CalculateCombatInteraction(context.Actor, context.Target, context.Magnitude);
        }

        context.Magnitude = calculatedResult.CalculatedDamageReceived;
        context.EventType = EventType.Damage;
        context.AttackOutcome = calculatedResult.AttackOutcome;
        context.Details = context.Details
            .Replace("{Actor}", context.Actor.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", calculatedResult.CalculatedDamageReceived.ToString());

        combatContext.LogEffectExecution(context);
        combatContext.InteractionManager.ApplyDamage(context);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        // Do nothing
    }
}