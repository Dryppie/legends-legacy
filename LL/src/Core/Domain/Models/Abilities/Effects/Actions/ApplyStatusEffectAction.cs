using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.StatusEffects;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class ApplyStatusEffectAction : IEffectAction
{
    private readonly StatusEffectType _status;
    private readonly int _amount;
    public int Magnitude => _amount;

    public ApplyStatusEffectAction(StatusEffectType status, int amount)
    {
        _status = status;
        _amount = amount;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        context.Target.ModifyStatuses(_status, Magnitude);

        context.EventType = EventType.StatusEffect;
        context.Details = context.Details
           .Replace("{Actor}", context.Actor.Name)
           .Replace("{Target}", context.Target.Name)
           .Replace("{Amount}", context.Magnitude.ToString());

        combatContext.LogEffectExecution(context);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        context.Target.ModifyStatuses(_status, -Magnitude);
    }
}