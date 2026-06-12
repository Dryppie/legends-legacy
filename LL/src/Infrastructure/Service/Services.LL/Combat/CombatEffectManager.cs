using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Trigger;
using Domain.Models.Combat.Abilities.Statuses;

namespace Services.LL.Combat;
public class CombatEffectManager : ICombatEffectManager
{
    private readonly ICombatContext _combatContext;
    private readonly List<EffectInstance> _activeEffects = new();
    private readonly List<StatusInstance> _activeStatuses = new();

    public CombatEffectManager(ICombatContext combatContext)
    {
        _combatContext = combatContext;
    }
    public List<EffectInstance> GetAllActiveEffects() => _activeEffects;
    public List<StatusInstance> GetAllActiveStatuses() => _activeStatuses;

    public void AddEffect(EffectInstance instance)
    {
        if (!instance.Target.IsAlive) return;
        // Execute any effect without a an interval.
        // If an effect has a duration, it should be executed so the effect can be applied immediately.
        // It will then last until expired
        // TODO: What happens if an effect has a duration, but also an interval? Should it not be applied immediately then?
        if (instance.ShouldExecuteImmediately)
        {
            instance.Apply(_combatContext);
        }
        if (instance.IsActive())
        {
            // Add all effects to the list, except instantaneous effects, as those will otherwise expire
            _activeEffects.Add(instance);
        }
    }

    public void AddStatus(StatusInstance status)
    {
        var owner = status.Owner;

        // Handle stacking logic
        if (!status.Definition.IsStackable)
        {
            var existing = _activeStatuses.FirstOrDefault(s =>
                s.Definition.Id == status.Definition.Id && s.Owner == owner);

            if (existing != null)
            {
                _activeStatuses.Remove(existing);
                owner.Statuses.Remove(existing); // remove from entity too
            }
        }

        // Fire immediate "OnStatusAppliedIfThis" if needed
        foreach (var trigger in status.Definition.Triggers)
        {
            if (trigger.Event == TriggerEvent.OnStatusAppliedIfThis)
            {
                foreach (var effect in trigger.Actions)
                {
                    var instance = new EffectInstance(effect, status.Source, status.Owner);
                    AddEffect(instance);
                }
            }
        }

        if (!status.IsExpired)
        {
            _activeStatuses.Add(status);
            owner.Statuses.Add(status); // add to entity
        }
    }

    public void Tick()
    {
        TickEffects();
        TickStatuses();
    }

    private void TickEffects()
    {
        foreach (var effect in _activeEffects.ToList())
        {
            effect.Update();

            if (!effect.Target.IsAlive) // If the target of the effect isn't alive, no reason to activate it.
            {
                _activeEffects.Remove(effect);
                continue;
            }

            if (effect.ShouldTrigger())
            {
                effect.ExecuteAction(_combatContext);
            }

            if (!effect.IsActive())
            {
                RemoveEffect(effect);
                //_combatContext.LogEffectExecution(new EffectContext(effect.Source, effect.Target, effect.Definition.AttackType, [], $"{effect.Definition.Log} expired {effect.Target.Name}"));
                _combatContext.EventBus.Publish(new CombatEvent
                {
                    Type = TriggerEvent.OnEffectExpired,
                    Source = effect.Source,
                    Target = effect.Target,
                    CurrentTime = _combatContext.CurrentTime,
                });
            }
        }
    }

    private void TickStatuses()
    {
        foreach (var status in _activeStatuses.ToList())
        {
            status.Tick();

            if (status.IsExpired)
            {
                // Fire expire effects
                foreach (var trigger in status.Definition.Triggers)
                {
                    if (trigger.Event == TriggerEvent.OnEffectExpired)
                    {
                        foreach (var effect in trigger.Actions)
                        {
                            var instance = new EffectInstance(effect, status.Source, status.Owner);
                            AddEffect(instance);
                        }
                    }
                    else
                    {
                        foreach (var effect in trigger.Actions)
                        {
                            var instance = new EffectInstance(effect, status.Source, status.Owner);
                            instance.ExecuteOnExpireAction(_combatContext);
                        }
                    }
                }

                _activeStatuses.Remove(status);
                status.Owner.Statuses.Remove(status);

                _combatContext.EventBus.Publish(new CombatEvent
                {
                    Type = TriggerEvent.OnEffectExpired,
                    Source = status.Source,
                    Target = status.Owner,
                    StatusId = status.Definition.Id,
                    CurrentTime = _combatContext.CurrentTime
                });
            }
        }
    }

    public void RemoveEffect(EffectInstance instance)
    {
        instance.ExecuteOnExpireAction(_combatContext);
        _activeEffects.Remove(instance);
    }

    //private readonly ICombatEntityManager _entityManager;
    //private readonly ICombatContext _combatContext;
    //private readonly Dictionary<CombatEntity, List<Effect>> _entityEffects = [];

    //public CombatEffectManager(ICombatEntityManager entityManager, ICombatContext combatContext, List<CombatEvent> eventLog)
    //{
    //    _entityManager = entityManager;
    //    _combatContext = combatContext;
    //    _entityEffects = [];
    //}

    //public void AddEffect(CombatEntity actor, CombatEntity target, Effect effect)
    //{
    //    // If the effect should trigger immediately (no trigger event needed)
    //    if (effect.Definition.Duration is NoDuration)
    //    {
    //        var context = CreateEffectContext(effect, target, actor);
    //        effect.ExecuteAction(context, _combatContext);
    //        return;
    //    }

    //    if (effect.Definition.Trigger.Equals(TriggerEvent.None))
    //    {
    //        var context = CreateEffectContext(effect, target, actor);
    //        effect.ExecuteAction(context, _combatContext);
    //    }

    //    if (!_entityEffects.TryGetValue(target, out var effects))
    //    {
    //        effects = [];
    //        _entityEffects[target] = effects;
    //    }

    //    effects.Add(effect);
    //}

    //public void UpdateEffectsForEntity(CombatEntity entity)
    //{
    //    if (_entityEffects.TryGetValue(entity, out var effects))
    //    {
    //        // If entity is dead, you might decide to skip updating or remove effects
    //        if (!entity.IsAlive)
    //            return;

    //        // Process each effect
    //        foreach (var effect in effects.ToList())
    //        {
    //            // Interval checks
    //            if (effect.ShouldTrigger())
    //            {
    //                var targets = new List<CombatEntity>();

    //                var triggerTarget = effect.Definition.TriggerTarget;

    //                var ownTeam = _entityManager.GetOwnTeam(entity);
    //                var enemyTeam = _entityManager.GetOpposingTeam(entity);
    //                var targeting = triggerTarget.Equals(CombatTargeting.None) ? effect.Definition.Targeting : effect.Definition.TriggerTarget;
    //                targets = TargetingManager.SelectTargets(targeting, entity, enemyTeam, ownTeam);


    //                foreach (var target in targets)
    //                {
    //                    var effectDefinition = new EffectDefinition(action: effect.Definition.Action,
    //                        duration: effect.Definition.Duration.Clone(),
    //                        condition: effect.Definition.Condition.Clone(),
    //                        interval: effect.Definition.Interval.Clone(),
    //                        usage: effect.Definition.Usage.Clone(),
    //                        targeting: effect.Definition.Targeting,
    //                        chance: effect.Definition.Chance,
    //                        effectModifications: effect.Definition.EffectModifications,
    //                        effectTags: effect.Definition.EffectTags,
    //                        attackType: effect.Definition.AttackType,
    //                        damageType: effect.Definition.DamageType)
    //                    {
    //                        Log = effect.Definition.Log
    //                    };

    //                    var triggeredEffect = new Effect()
    //                    {
    //                        Definition = effectDefinition,
    //                        Caster = entity,
    //                        Target = target,
    //                    };

    //                    var context = CreateEffectContext(triggeredEffect, target, entity, effect.Definition.Action.Magnitude);
    //                    effect.ExecuteAction(context, _combatContext);
    //                }
    //            }

    //            // Duration check
    //            if (!effect.IsActive())
    //            {
    //                var expireContext = CreateEffectContext(effect, entity);
    //                effect.ExecuteOnExpireAction(expireContext, _combatContext);

    //                effects.Remove(effect);
    //            }

    //            effect.Update();
    //        }

    //        // If no effects left on this entity, remove the key
    //        if (effects.Count == 0)
    //        {
    //            _entityEffects.Remove(entity);
    //        }
    //    }
    //}

    //public void TriggerEffects(TriggerEvent triggerEvent, CombatEntity effectTriggeredOn, CombatEntity? causeOfTrigger = null, int magnitude = -1)
    //{
    //    if (!_entityEffects.TryGetValue(effectTriggeredOn, out var effects))
    //        return;

    //    // Find the effects on the target with the correct trigger
    //    foreach (var effect in effects.Where(e => e.IsTrigger(triggerEvent)).ToList())
    //    {

    //        var targets = new List<CombatEntity>();

    //        var triggerTarget = effect.Definition.TriggerTarget;

    //        if (triggerTarget.Equals(CombatTargeting.CauseOfTrigger))
    //        {
    //            targets.Add(causeOfTrigger!);
    //        }
    //        else if (triggerTarget.Equals(CombatTargeting.AttackedEnemy))
    //        {
    //            targets.Add(causeOfTrigger!);
    //        }
    //        else
    //        {
    //            var ownTeam = _entityManager.GetOwnTeam(effectTriggeredOn);
    //            var enemyTeam = _entityManager.GetOpposingTeam(effectTriggeredOn);
    //            var targeting = triggerTarget.Equals(CombatTargeting.None) ? effect.Definition.Targeting : effect.Definition.TriggerTarget;
    //            targets = TargetingManager.SelectTargets(targeting, effectTriggeredOn, enemyTeam, ownTeam);
    //        }

    //        foreach ( var target in targets )
    //        {
    //            var effectDefinition = new EffectDefinition(action: effect.Definition.Action,
    //                duration: effect.Definition.Duration.Clone(),
    //                condition: effect.Definition.Condition.Clone(),
    //                interval: effect.Definition.Interval.Clone(),
    //                usage: effect.Definition.Usage.Clone(),
    //                targeting: effect.Definition.Targeting,
    //                chance: effect.Definition.Chance,
    //                effectModifications: effect.Definition.EffectModifications,
    //                effectTags: effect.Definition.EffectTags,
    //                attackType: effect.Definition.AttackType,
    //                damageType: effect.Definition.DamageType)
    //            {
    //                Log = effect.Definition.Log
    //            };

    //            var triggeredEffect = new Effect()
    //            {
    //                Definition = effectDefinition,
    //                Caster = effectTriggeredOn,
    //                Target = target,
    //            };

    //            var context = CreateEffectContext(triggeredEffect, target, effectTriggeredOn, magnitude);
    //            effect.ExecuteAction(context, _combatContext);
    //        }
    //    }
    //}

    //public void RenewEffect(Effect effect)
    //{
    //    effect.Definition.Duration.RenewDuration();
    //}

    //public Effect? FindEffectForEntity(CombatEntity target, string sourceId)
    //{
    //    return _entityEffects.TryGetValue(target, out var effects) ? effects.FirstOrDefault(e => e.Definition.SourceId.Equals(sourceId)) : null;
    //}

    ///// <summary>
    ///// Creates a fully populated EffectContext to be passed to the effect when executing.
    ///// You can adjust this to provide OwnTeam, EnemyTeam, CurrentTime, etc.
    ///// </summary>
    //private EffectContext CreateEffectContext(Effect effect, CombatEntity target, CombatEntity? actor = null, int magnitude = -1)
    //{
    //    // Gather any info from the combat context or entity manager:
    //    var ownTeam = _entityManager.GetOwnTeam(target);
    //    var enemyTeam = _entityManager.GetOpposingTeam(target);

    //    var effectContext = new EffectContext(effect: effect,
    //                                          ownTeam: ownTeam,
    //                                          enemyTeam: enemyTeam,
    //                                          actor: actor ?? effect.Caster, // Fallback to the caster of the effect, in case there's no actor
    //                                          target: target,
    //                                          magnitude: magnitude,
    //                                          details: effect.Definition.Log
    //                                          );

    //    return effectContext;
    //}
}