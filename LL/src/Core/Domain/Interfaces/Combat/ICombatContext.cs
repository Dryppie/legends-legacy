using Domain.Models.Abilities.Effects;

namespace Domain.Interfaces.Combat;
public interface ICombatContext
{
    public ICombatEntityManager EntityManager { get; set; }
    public ICombatEffectManager EffectManager { get; set; }
    public ICombatInteractionManager InteractionManager { get; set; }
    void LogEffectExecution(EffectContext context);
}