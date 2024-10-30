using Domain.Interfaces;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class SelfDestructAction : IEffectAction
{
    private readonly ICombatContext _combatContext;
    public int Magnitude => 1;

    public SelfDestructAction(ICombatContext combatContext)
    {
        _combatContext = combatContext;
    }

    public void Execute(EffectContext context, Action<EffectContext> action)
    {
        
    }

    public void OnExpireExecute(EffectContext context, Action<EffectContext> action)
    {
        _combatContext.RemoveEntityFromTeam(context.Target);

        context.EffectType = EventType.SummonExpired;
        context.Details = $"{context.Target.Name} vanished. Summon effect expired.";

        action.Invoke(context);
    }
}
