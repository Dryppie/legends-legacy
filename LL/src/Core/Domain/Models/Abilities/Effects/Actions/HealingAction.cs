using Domain.Interfaces;
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

    public void Execute(EffectContext context, Action<EffectContext> action)
    {
        // If it's a flat amount, take the value of the effect itself (_damageAmount),
        // else take the calculated value from the context.Magnitude
        var healingReceived = context.Target.CalculateReceiveHealing(context.IsFlatAmount ? Magnitude : context.Magnitude);

        context.Magnitude = healingReceived;
        context.EventType = EventType.Heal;
        context.Details = context.Details
            .Replace("{Actor}", context.Owner.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", healingReceived.ToString());

        action.Invoke(context);

        context.Target.PerformReceiveHealing(healingReceived);

    }

    public void OnExpireExecute(EffectContext context, Action<EffectContext> action)
    {
        // Do nothing
    }
}