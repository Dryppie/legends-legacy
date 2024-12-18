using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects;

namespace Domain.Interfaces;
public interface IEffectAction
{
    int Magnitude { get; }
    void Execute(EffectContext context, ICombatContext combatContext);
    void OnExpireExecute(EffectContext context, ICombatContext combatContext);
}