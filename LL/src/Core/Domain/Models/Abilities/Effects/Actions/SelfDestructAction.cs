using Domain.Interfaces.Abilities;
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

    public void Execute(EffectContext effect, ICombatContext combatContext)
    {
        
    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {
        combatContext.EntityManager.RemoveEntity(effect.Target);

        effect.EventType = EventType.SummonExpired;
        effect.Details = $"{effect.Target.Name} vanished. Summon effect expired.";

        combatContext.LogEffectExecution(effect);
    }
}
