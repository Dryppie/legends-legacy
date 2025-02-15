using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class HealingAction : IEffectAction
{
    private readonly int _healAmount;
    public int Magnitude => _healAmount;
    public AttributeType? ScalingAttribute { get; set; }
    public float ScalingMultiplier { get; set; }

    public HealingAction(int healAmount, AttributeType? scalingAttribute, float scalingMultiplier)
    {
        _healAmount = healAmount;
        ScalingAttribute = scalingAttribute;
        ScalingMultiplier = scalingMultiplier;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        // Attack outcome
        var attackOutcome = combatContext.InteractionManager.CalculateAttackOutcomeForHealing(context.Actor, context.Target);

        // Potential healing
        var isFlatAmount = context.Effect.Definition.IsFlatAmount;
        var healingAmount = isFlatAmount
                            ? Magnitude
                            : combatContext.InteractionManager.CalculateHealingToDeal(context.Actor, context.Target, Magnitude, ScalingAttribute!.Value, ScalingMultiplier);

        // Healing target will receive
        var healingReceived = combatContext.InteractionManager.CalculateHealingReceived(context.Target, healingAmount, attackOutcome);

        context.AttackOutcome = attackOutcome;
        context.Magnitude = healingReceived;
        context.EventType = EventType.Heal;
        context.Details = context.Details   
            .Replace("{Actor}", context.Actor.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", context.Magnitude.ToString());

        combatContext.LogEffectExecution(context);

        combatContext.InteractionManager.ApplyHealing(context);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        // Do nothing
    }
}