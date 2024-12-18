using Domain.Interfaces;
using Domain.Interfaces.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class ApplyStatusEffectAction : IEffectAction
{
    private readonly string _status;
    public int Magnitude => 1;

    public ApplyStatusEffectAction(string status)
    {
        _status = status;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        context.Target.ModifyStatuses(_status);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        context.Target.ModifyStatuses(_status, remove: true);
    }
}