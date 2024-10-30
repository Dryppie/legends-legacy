using Domain.Helpers;
using Domain.Interfaces;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class DamageAction : IEffectAction
{
    private readonly int _damageAmount;
    public int Magnitude => _damageAmount;

    public DamageAction(int damageAmount)
    {
        _damageAmount = damageAmount;
    }

    public void Execute(EffectContext context, Action<EffectContext> action)
    {
        var calculatedResult = new CalculatedResult();
        if (context.IsFlatAmount)
        {
            // If it's a flat amount, take the value of the effect itself (_damageAmount)
            calculatedResult.CalculatedDamageDealt = Magnitude;
            calculatedResult.CalculatedDamageReceived = context.Target.CalculateReceiveDamage(Magnitude);
        }
        else
        {
            calculatedResult = CombatFormulaCalculator.CalculateCombatInteraction(context.Owner, context.Target, context.Magnitude);
        }

        context.Magnitude = calculatedResult.CalculatedDamageReceived;
        context.EffectType = EventType.Damage;
        context.AttackOutcome = calculatedResult.AttackOutcome;
        context.Details = context.Details
            .Replace("{Actor}", context.Owner.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", calculatedResult.CalculatedDamageReceived.ToString());
        action.Invoke(context);

        context.Target.PerformReceiveDamage(context.Magnitude, context.Owner);
    }

    public void OnExpireExecute(EffectContext context, Action<EffectContext> action)
    {
        // Do nothing
    }
}