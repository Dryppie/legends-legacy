using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects;
public class Effect
{
    public EffectDefinition Definition { get; set; }
    /// <summary>
    /// The entity that used the ability with this effect
    /// </summary>
    public CombatEntity Caster { get; set; }
    /// <summary>
    /// The entity this effect is attached to (stored on)
    /// </summary>
    public CombatEntity Owner { get; set; }
    /// <summary>
    /// The entity this effect affects when Executed
    /// If null, the target is simply going to be the Owner of the effect
    /// </summary>
    public CombatEntity? Target { get; set; }

    public void Update()
    {
        Definition.Duration.DecrementDuration();
        Definition.Interval.Update();
        Definition.Usage.Recharge();
    }

    public void ExecuteAction(EffectContext context, ICombatContext combatContext)
    {
        if (!Definition.Usage.CanUse()) return;
        if (!Definition.Condition.IsSatisfied(context)) return;

        if (Definition.Chance == 100 || Random.Shared.Next(1, 101) <= Definition.Chance)
        {
            // TODO: Apply EffectModifications properly. This might have to be checked during DamageCalculation and HealCalculation, and not here
            Definition.Action?.Execute(context, combatContext);
            Definition.Usage.ConsumeUse();
        }
    }

    public void ExecuteOnExpireAction(EffectContext context, ICombatContext combatContext)
    {
        if (Definition.Chance == 100 || Random.Shared.Next(1, 101) <= Definition.Chance)
        {
            Definition.Action?.OnExpireExecute(context, combatContext);
        }
    }

    public bool IsTrigger(TriggerEvent triggerEvent)
    {
        return Definition.Trigger == triggerEvent;
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