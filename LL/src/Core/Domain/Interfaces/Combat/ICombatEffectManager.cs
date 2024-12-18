using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Entities;

namespace Domain.Interfaces.Combat;
public interface ICombatEffectManager
{
    /// <summary>
    /// Adds an effect to a target entity.
    /// </summary>
    void AddEffect(Entity target, Effect effect);

    /// <summary>
    /// Update all effects for all entities.
    /// This should be called every tick of the combat simulation.
    /// </summary>
    void UpdateEffectsForEntity(Entity entity);

    /// <summary>
    /// Trigger effects that respond to a given event, such as damage taken or healing received.
    /// </summary>
    void TriggerEffects(TriggerEvent triggerEvent, Entity target, Entity? opponent = null, int magnitude = 0);
}