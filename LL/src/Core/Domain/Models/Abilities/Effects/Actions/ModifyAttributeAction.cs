using Domain.Interfaces;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class ModifyAttributeAction : IEffectAction
{
    public AttributeModifier AttributeModifier;
    public int Magnitude => 1;

    public ModifyAttributeAction(AttributeModifier attributeModifier)
    {
        AttributeModifier = attributeModifier;
    }

    public void Execute(EffectContext context, Action<EffectContext> action)
    {
        context.EventType = AttributeModifier.Amount > 0 ? EventType.Buff : EventType.Debuff;
        context.Details = context.Details
            .Replace("{Actor}", context.Owner.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", AttributeModifier.Amount.ToString());

        action.Invoke(context);

        context.Target.ModifyAttribute(AttributeModifier);
    }

    public void OnExpireExecute(EffectContext context, Action<EffectContext> action)
    {
        context.EventType = AttributeModifier.Amount > 0 ? EventType.BuffExpired : EventType.DebuffExpired;
        context.Details = context.Details
            .Replace("{Actor}", context.Owner.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", AttributeModifier.Amount.ToString());

        action.Invoke(context);

        context.Target.ModifyAttribute(AttributeModifier, remove: true);
    }
}