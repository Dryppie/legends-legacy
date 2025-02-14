using Domain.Interfaces.Combat;
using Domain.Models.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Duration;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Combat;

namespace Services.LL.Combat;
public class CombatEffectManager : ICombatEffectManager
{
    private readonly ICombatEntityManager _entityManager;
    private readonly ICombatContext _combatContext;
    private readonly Dictionary<CombatEntity, List<Effect>> _entityEffects = [];

    public CombatEffectManager(ICombatEntityManager entityManager, ICombatContext combatContext, List<CombatEvent> eventLog)
    {
        _entityManager = entityManager;
        _combatContext = combatContext;
        _entityEffects = [];
    }

    public void AddEffect(CombatEntity actor, CombatEntity target, Effect effect)
    {
        // If the effect should trigger immediately (no trigger event needed)
        if (effect.Definition.Duration is NoDuration)
        {
            var context = CreateEffectContext(effect, target, actor);
            effect.ExecuteAction(context, _combatContext);
            return;
        }

        if (effect.Definition.Trigger.Equals(TriggerEvent.None))
        {
            var context = CreateEffectContext(effect, target, actor);
            effect.ExecuteAction(context, _combatContext);
        }

        if (!_entityEffects.TryGetValue(target, out var effects))
        {
            effects = [];
            _entityEffects[target] = effects;
        }

        effects.Add(effect);
    }

    public void UpdateEffectsForEntity(CombatEntity entity)
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
                if (effect.ShouldTrigger())
                {
                    var intervalContext = CreateEffectContext(effect, entity);
                    effect.ExecuteAction(intervalContext, _combatContext);
                }

                // Duration check
                if (!effect.IsActive())
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

    public void TriggerEffects(TriggerEvent triggerEvent, CombatEntity effectTriggeredOn, CombatEntity? causeOfTrigger = null, int magnitude = -1)
    {
        if (!_entityEffects.TryGetValue(effectTriggeredOn, out var effects))
            return;

        // Find the effects on the target with the correct trigger
        foreach (var effect in effects.Where(e => e.IsTrigger(triggerEvent)).ToList())
        {
            
            var targets = new List<CombatEntity>();

            var triggerTarget = effect.Definition.TriggerTarget;

            if (triggerTarget.Equals(Targeting.CauseOfTrigger))
            {
                targets.Add(causeOfTrigger!);
            }
            else
            {
                var ownTeam = _entityManager.GetOwnTeam(effectTriggeredOn);
                var enemyTeam = _entityManager.GetOpposingTeam(effectTriggeredOn);
                var targeting = effect.Definition.TriggerTarget.Equals(Targeting.None) ? effect.Definition.Targeting : effect.Definition.TriggerTarget;
                targets = TargetingManager.SelectTargets(triggerTarget.Equals(Targeting.None) ? effect.Definition.Targeting : triggerTarget, effectTriggeredOn, enemyTeam, ownTeam);
            }

            foreach ( var target in targets )
            {
                var effectDefinition = new EffectDefinition(action: effect.Definition.Action,
                    duration: effect.Definition.Duration.Clone(),
                    condition: effect.Definition.Condition.Clone(),
                    interval: effect.Definition.Interval.Clone(),
                    usage: effect.Definition.Usage.Clone(),
                    targeting: effect.Definition.Targeting,
                    trigger: effect.Definition.Trigger,
                    triggerTarget: effect.Definition.TriggerTarget,
                    isFlatAmount: effect.Definition.IsFlatAmount,
                    chance: effect.Definition.Chance,
                    effectTags: effect.Definition.EffectTags,
                    attackType: effect.Definition.AttackType,
                    damageType: effect.Definition.DamageType)
                {
                    Log = effect.Definition.Log
                };

                var triggeredEffect = new Effect()
                {
                    Definition = effectDefinition,
                    Caster = effectTriggeredOn,
                    Target = target,
                };

                var context = CreateEffectContext(effect, target, effectTriggeredOn, magnitude);
                effect.ExecuteAction(context, _combatContext);
            }
        }
    }

    public void RenewEffect(Effect effect)
    {
        effect.Definition.Duration.RenewDuration();
    }

    public Effect? FindEffectForEntity(CombatEntity target, string sourceId)
    {
        return _entityEffects.TryGetValue(target, out var effects) ? effects.FirstOrDefault(e => e.Definition.SourceId.Equals(sourceId)) : null;
    }

    /// <summary>
    /// Creates a fully populated EffectContext to be passed to the effect when executing.
    /// You can adjust this to provide OwnTeam, EnemyTeam, CurrentTime, etc.
    /// </summary>
    private EffectContext CreateEffectContext(Effect effect, CombatEntity target, CombatEntity? actor = null, int magnitude = -1)
    {
        // Gather any info from the combat context or entity manager:
        var ownTeam = _entityManager.GetOwnTeam(target);
        var enemyTeam = _entityManager.GetOpposingTeam(target);

        var effectContext = new EffectContext(effect: effect,
                                              ownTeam: ownTeam,
                                              enemyTeam: enemyTeam,
                                              actor: actor ?? effect.Caster, // Fallback to the caster of the effect, in case there's no actor
                                              target: target,
                                              magnitude: magnitude,
                                              details: effect.Definition.Log
                                              );

        return effectContext;
    }
}