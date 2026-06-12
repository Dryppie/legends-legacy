using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public class TriggerSecondaryEffectAction : IEffectAction
{
    private readonly string _secondaryEffectId;
    public int Magnitude => 1;

    public TriggerSecondaryEffectAction(string secondaryEffectId)
    {
        _secondaryEffectId = secondaryEffectId;
    }

    public void Execute(EffectContext effect, ICombatContext combatContext)
    {
        effect.EventType = EventType.AbilityUse;
        effect.Magnitude = Magnitude;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Status}", _secondaryEffectId)
            .Replace("{Amount}", Magnitude.ToString());

        combatContext.LogEffectExecution(effect);
    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {
    }
}
