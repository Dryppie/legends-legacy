using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class NestedEffectAction : IEffectAction
{
    public int Magnitude => 1;
    public List<EffectDefinition> Effects { get; set; } = [];

    public NestedEffectAction(List<EffectDefinition> effects)
    {
        Effects = effects;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        var effectsToApply = new List<(CombatEntity target, Effect effectInstance)>();

        // Apply each effect of the ability
        foreach (var effectTemplate in Effects)
        {
            var effectDefinition = new EffectDefinition(
                action: effectTemplate.Action,
                duration: effectTemplate.Duration.Clone(),
                condition: effectTemplate.Condition.Clone(),
                interval: effectTemplate.Interval.Clone(),
                usage: effectTemplate.Usage.Clone(),
                targeting: effectTemplate.Targeting,
                trigger: effectTemplate.Trigger,
                triggerTarget: effectTemplate.TriggerTarget,
                isFlatAmount: effectTemplate.IsFlatAmount,
                chance: effectTemplate.Chance,
                effectTags: effectTemplate.EffectTags,
                attackType: effectTemplate.AttackType,
                damageType: effectTemplate.DamageType
                );
            effectDefinition.Log = effectTemplate.Log;

            var effectInstance = new Effect()
            {
                Definition = effectDefinition,
                Caster = context.Actor!,
                Owner = context.Target
            };


            // Defer effect application until after logging
            effectsToApply.Add((context.Target, effectInstance));
        }

        context.Details = context.Details
            .Replace("{Actor}", context.Actor!.Name)
            .Replace("{Target}", context.Target.Name);

        combatContext.LogEffectExecution(context);

        foreach (var (target, effectInstance) in effectsToApply)
        {
            combatContext.EffectManager.AddEffect(context.Actor, target, effectInstance);
        }
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        throw new NotImplementedException();
    }

    private List<CombatEntity> SelectTargets(Targeting target, CombatEntity caster, List<CombatEntity> enemyTeam, List<CombatEntity> allies)
    {
        List<CombatEntity> targets = [];

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

    private CombatEntity? SelectTarget(List<CombatEntity> potentialTargets)
    {
        // Select a random alive target
        var aliveTargets = potentialTargets.Where(c => c.IsAlive).ToList();
        if (aliveTargets.Count == 0) return null;

        var random = new Random();
        int index = random.Next(aliveTargets.Count);
        return aliveTargets[index];
    }
}