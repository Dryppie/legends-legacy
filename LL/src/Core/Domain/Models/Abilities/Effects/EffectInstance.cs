using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.Intervals;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects;
public class EffectInstance
{
    public EffectDefinition Definition { get; set; }
    /// <summary>
    /// The entity that used the ability with this effect
    /// </summary>
    public CombatEntity Source { get; }
    public CombatEntity Target { get; }
    public bool HasTriggered { get; private set; }

    public EffectInstance(EffectDefinition definition, CombatEntity source, CombatEntity target)
    {
        Definition = definition;
        Source = source;
        Target = target;
    }
    public bool ShouldExecuteImmediately => Definition.Interval is NoInterval;
    public void Update()
    {
        Definition.Duration.DecrementDuration();
        Definition.Interval.Update();
        Definition.Usage.Recharge();
    }

    public void Apply(ICombatContext combatContext) => ExecuteAction(combatContext);

    public void ExecuteAction(ICombatContext combatContext)
    {
        if (!Definition.Usage.CanUse())
            return;

        if (!Definition.Condition.IsSatisfied(Source, Target, combatContext))
            return;

        // If the chance fails, no need to log anything. Lack of log should be enough to display something didn't trigger
        if (Definition.Chance == 100 || Random.Shared.Next(1, 101) <= Definition.Chance)
        {
            // TODO: Apply EffectModifications properly. This might have to be checked during DamageCalculation and HealCalculation, and not here
            var effectContext = new EffectContext(Source, Target, Definition.AttackType, Definition.Log);
            Definition.Action?.Execute(effectContext, combatContext);
            HasTriggered = true;
        }
        Definition.Usage.ConsumeUse();
    }

    public void ExecuteOnExpireAction(ICombatContext combatContext)
    {
        // Chance effect ExecuteOnExpire causes an issue. If an effect has a chance to stun the opponent,
        // then this chance also applies to when it tries to unstun the opponent again.
        //if (Definition.Chance == 100 || Random.Shared.Next(1, 101) <= Definition.Chance)
        //{
        //}
        var effectContext = new EffectContext(Source, Target, Definition.AttackType, Definition.Log);
        Definition.Action?.OnExpireExecute(effectContext, combatContext);
    }

    public bool IsActive()
    {
        return Definition.Duration.IsActive();
    }

    public bool ShouldTrigger()
    {
        return Definition.Interval.ShouldTrigger();
    }
}