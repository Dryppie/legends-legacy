using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Abilities.Statuses;
using Domain.Models.Combat;

namespace Domain.Interfaces.Combat;
public interface ICombatEffectManager
{
    void Tick();
    void AddEffect(EffectInstance instance);
    void AddStatus(StatusInstance status);
    void RemoveEffect(EffectInstance instance);

    ///// <summary>
    ///// Adds an effect to a target entity.
    ///// </summary>
    //void AddEffect(CombatEntity actor, CombatEntity target, Effect effect);

    ///// <summary>
    ///// Update all effects for all entities.
    ///// This should be called every tick of the combat simulation.
    ///// </summary>
    //void UpdateEffectsForEntity(CombatEntity entity);

    ///// <summary>
    ///// Trigger effects that respond to a given event, such as damage taken or healing received.
    ///// </summary>
    //void TriggerEffects(TriggerEvent triggerEvent, CombatEntity target, CombatEntity? actor = null, int magnitude = -1);
    //Effect? FindEffectForEntity(CombatEntity target, string sourceId);
    //void RenewEffect(Effect existingEffect);
}