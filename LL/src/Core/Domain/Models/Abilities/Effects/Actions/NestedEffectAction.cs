using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Domain.Models.Abilities.Effects.Actions;
public class NestedEffectAction : IEffectAction
{
    public int Magnitude => 1;
    public List<Effect> Effects { get; set; } = [];

    public NestedEffectAction(List<Effect> effects)
    {
        Effects = effects;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        var effectsToApply = new List<(Entity target, Effect effectInstance)>();

        // Apply each effect of the ability
        foreach (var effectTemplate in Effects)
        {
            var effectInstance = new Effect(
                action: effectTemplate.Action,
                duration: effectTemplate.Duration.Clone(),
                condition: effectTemplate.Condition.Clone(),
                targeting: effectTemplate.Targeting,
                trigger: effectTemplate.Trigger,
                interval: effectTemplate.Interval.Clone(),
                caster: context.Actor,
                applyOnSelf: effectTemplate.ApplyOnSelf,
                isFlatAmount: effectTemplate.IsFlatAmount,
                chance: effectTemplate.Chance,
                effectTags: effectTemplate.EffectTags,
                attackType: effectTemplate.AttackType,
                damageType: effectTemplate.DamageType
                );

            effectInstance.Log = effectTemplate.Log;

            // Defer effect application until after logging
            effectsToApply.Add((context.Target, effectInstance));
        }

        context.Details = context.Details
            .Replace("{Actor}", context.Actor.Name)
            .Replace("{Target}", context.Target.Name);

        combatContext.LogEffectExecution(context);

        foreach (var (target, effectInstance) in effectsToApply)
        {
            combatContext.EffectManager.AddEffect(target, effectInstance);
        }
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        throw new NotImplementedException();
    }

    private List<Entity> SelectTargets(Targeting target, Entity caster, List<Entity> enemyTeam, List<Entity> allies)
    {
        List<Entity> targets = [];

        switch (target)
        {
            case Targeting.SingleEnemy:
                var enemyTarget = SelectTarget(enemyTeam);
                if (enemyTarget != null) targets.Add(enemyTarget);
                break;

            case Targeting.AllEnemies:
                targets = enemyTeam.Where(e => e.IsAlive).ToList();
                break;
            case Targeting.TwoEnemies:
                if (enemyTeam.Where(e => e.IsAlive).Count() >= 2)
                {
                    targets = enemyTeam.Where(e => e.IsAlive).Take(2).ToList();
                }
                else
                {
                    var enemyTargets = SelectTarget(enemyTeam);
                    if (enemyTargets != null) targets.Add(enemyTargets);
                }
                break;
            case Targeting.TwoAllies:
                targets = enemyTeam.Where(e => e.IsAlive).ToList();
                break;

            case Targeting.Self:
                targets.Add(caster);
                break;

            case Targeting.SingleAlly:
                var allyTarget = SelectTarget(allies);
                if (allyTarget != null) targets.Add(allyTarget);
                break;

            case Targeting.AllAllies:
                targets = allies.Where(a => a.IsAlive).ToList();
                break;

            default:
                throw new NotSupportedException($"Targeting type '{target}' is not supported.");
        }

        return targets;
    }

    private Entity? SelectTarget(List<Entity> potentialTargets)
    {
        // Select a random alive target
        var aliveTargets = potentialTargets.Where(c => c.IsAlive).ToList();
        if (aliveTargets.Count == 0) return null;

        var random = new Random();
        int index = random.Next(aliveTargets.Count);
        return aliveTargets[index];
    }
}