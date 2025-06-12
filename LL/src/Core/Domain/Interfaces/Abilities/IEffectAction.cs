using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects;

namespace Domain.Interfaces.Abilities;
public interface IEffectAction
{
    void Execute(EffectContext effect, ICombatContext combatContext);
    void OnExpireExecute(EffectContext effect, ICombatContext combatContext);
}