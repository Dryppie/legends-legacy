using Domain.Interfaces.Combat;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.Effects;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class SelfDestructEffectOperationHandler : ICombatEffectOperationHandler
{
    public string Operation => CombatEffectOperation.SelfDestruct;

    public void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        combatContext.EntityManager.RemoveEntity(effect.Target);
        effect.EventType = EventType.SummonExpired;
        combatContext.LogEffectExecution(effect, CombatEffectActionHelpers.CreateSimpleCombatEntity(effect.Target));
    }

    public void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
    }
}
