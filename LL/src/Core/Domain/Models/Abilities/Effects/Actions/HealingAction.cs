using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class HealingAction : IEffectAction
{
    private readonly int _healAmount;
    public int Magnitude => _healAmount;
    public AttributeType? HealScalingAttribute { get; set; }
    public float HealScalingMultiplier { get; set; }

    public HealingAction(int healAmount, AttributeType? healScalingAttribute, float healScalingMultiplier)
    {
        _healAmount = healAmount;
        HealScalingAttribute = healScalingAttribute;
        HealScalingMultiplier = healScalingMultiplier;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        // If it's a flat amount, take the value of the effect itself (_damageAmount),
        // else take the calculated value from the context.Magnitude
        var healingReceived = combatContext.InteractionManager.CalculateHealingReceived(context.Owner, context.Target, context.IsFlatAmount ? Magnitude : context.Magnitude);

        context.Magnitude = healingReceived;
        context.EventType = EventType.Heal;
        context.Details = context.Details
            .Replace("{Actor}", context.Owner.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", healingReceived.ToString());

        combatContext.LogEffectExecution(context);

        combatContext.InteractionManager.ApplyHealing(context.Owner, context.Target, healingReceived);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        // Do nothing
    }
}