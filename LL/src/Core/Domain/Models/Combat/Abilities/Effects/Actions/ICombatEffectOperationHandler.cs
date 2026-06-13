using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities.Effects;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public interface ICombatEffectOperationHandler
{
    string Operation { get; }
    void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext);
    void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext);
}
