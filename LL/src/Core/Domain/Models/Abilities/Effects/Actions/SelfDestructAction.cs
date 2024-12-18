using Domain.Interfaces;
using Domain.Interfaces.Combat;
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

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        combatContext.EntityManager.RemoveEntity(context.Target);

        context.EventType = EventType.SummonExpired;
        context.Details = $"{context.Target.Name} vanished. Summon effect expired.";

        combatContext.LogEffectExecution(context);
    }
}
