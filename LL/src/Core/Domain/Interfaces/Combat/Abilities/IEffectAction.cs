using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities.Effects;

namespace Domain.Interfaces.Combat.Abilities;
public interface IEffectAction
{
    void Execute(EffectContext effect, ICombatContext combatContext);
    void OnExpireExecute(EffectContext effect, ICombatContext combatContext);
}