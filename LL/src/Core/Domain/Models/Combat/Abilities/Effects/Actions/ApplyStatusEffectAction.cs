using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Actions;
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

    public void Execute(EffectContext effect, ICombatContext combatContext)
    {

        effect.Target.ModifyStatusEffects(_status, Magnitude);

        effect.EventType = EventType.StatusEffect;
        effect.Details = effect.Details
           .Replace("{Actor}", effect.Source.Name)
           .Replace("{Target}", effect.Target.Name)
           .Replace("{Amount}", effect.Magnitude.ToString());

        combatContext.LogEffectExecution(effect);
    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {
        effect.Target.ModifyStatusEffects(_status, -Magnitude);
    }
}