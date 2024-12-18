using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Services.LL.Combat;
public class CombatEffectManager : ICombatEffectManager
{
    private readonly ICombatEntityManager _entityManager;
    private readonly ICombatContext _combatContext;
    private readonly List<Effect> _activeEffects = new();
    private readonly List<CombatEvent> _eventLog; // Optional, if you want direct access to event logging
    private readonly Dictionary<Entity, List<Effect>> _entityEffects = new();

    public CombatEffectManager(ICombatEntityManager entityManager, ICombatContext combatContext, List<CombatEvent> eventLog)
    {
        _entityManager = entityManager;
        _combatContext = combatContext;
        _eventLog = eventLog;
    }

    public void AddEffect(Entity target, Effect effect)
    {
        if (!_entityEffects.TryGetValue(target, out var effects))
        {
            effects = new List<Effect>();
            _entityEffects[target] = effects;
        }

        // If the effect should trigger immediately (no trigger event needed)
        if (effect.Trigger == TriggerEvent.None)
        {
            var context = CreateEffectContext(effect, target);
            effect.ExecuteAction(context, _combatContext);
            return;
        }

        effects.Add(effect);
    }

    public void UpdateEffectsForEntity(Entity entity)
    {
        if (_entityEffects.TryGetValue(entity, out var effects))
        {
            // If entity is dead, you might decide to skip updating or remove effects
            if (!entity.IsAlive)
                return;

            // Process each effect
            foreach (var effect in effects.ToList())
            {
                // Interval checks
                if (effect.Interval.ShouldTrigger())
                {
                    var intervalContext = CreateEffectContext(effect, entity);
                    effect.ExecuteAction(intervalContext, _combatContext);
                }

                // Duration check
                if (!effect.Duration.IsActive())
                {
                    var expireContext = CreateEffectContext(effect, entity);
                    effect.ExecuteOnExpireAction(expireContext, _combatContext);

                    effects.Remove(effect);
                }

                effect.Update();
            }

            // If no effects left on this entity, remove the key
            if (effects.Count == 0)
            {
                _entityEffects.Remove(entity);
            }
        }
    }

    public void TriggerEffects(TriggerEvent triggerEvent, Entity effectTriggeredOn, Entity? causeOfTrigger = null, int magnitude = 0)
    {
        if (!_entityEffects.TryGetValue(effectTriggeredOn, out var effects))
            return;

        foreach (var effect in effects.Where(e => e.IsTrigger(triggerEvent)).ToList())
        {
            var context = CreateEffectContext(effect, effectTriggeredOn, causeOfTrigger, magnitude);
            effect.ExecuteAction(context, _combatContext);
        }
    }

    /// <summary>
    /// Creates a fully populated EffectContext to be passed to the effect when executing.
    /// You can adjust this to provide OwnTeam, EnemyTeam, CurrentTime, etc.
    /// </summary>
    private EffectContext CreateEffectContext(Effect effect, Entity target, Entity? opponent = null, int magnitude = 0)
    {
        // Gather any info from the combat context or entity manager:
        var ownTeam = _entityManager.GetOwnTeam(target);
        var enemyTeam = _entityManager.GetOpposingTeam(target);

        return new EffectContext(ownTeam,
                                 enemyTeam,
                                 effect.Caster ?? target,
                                 opponent ?? target,
                                 effect.Trigger,
                                 magnitude,
                                 effect.IsFlatAmount,
                                 effect.Log,
                                 effect.EffectModifications,
                                 effect.Action);
    }
}