using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
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

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        var target = context.Target;

        target.ModifyAttribute(AttributeModifier);

        // The details will simply display the value that the buff/debuff changes, so it's easily understandable
        // ie. 'HP increasd by 25%', rather than 'HP increased by 171'.
        // Magnitude will tell exactly how much it changed
        context.Magnitude = context.Magnitude;
        context.Attribute = AttributeModifier.AttributeType;
        context.EventType = AttributeModifier.Amount > 0 ? EventType.Buff : EventType.Debuff;
        context.Details = context.Details
            .Replace("{Actor}", context.Actor.Name)
            .Replace("{Target}", target.Name)
            .Replace("{Amount}", AttributeModifier.Amount.ToString());

        var simpleCombatEntity = new SimpleCombatEntity()
        {
            Id = target.Id,
            MaxHealth = target.GetAttributeValue(AttributeType.MaxHealth),
            Health = target.GetAttributeValue(AttributeType.Health),
            MaxMana = target.GetAttributeValue(AttributeType.MaxMana),
            Mana = target.GetAttributeValue(AttributeType.Mana)
        };

        combatContext.LogEffectExecution(context, simpleCombatEntity);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        context.EventType = AttributeModifier.Amount > 0 ? EventType.BuffExpired : EventType.DebuffExpired;
        context.Details = context.Details
            .Replace("{Actor}", context.Actor.Name)
            .Replace("{Target}", context.Target.Name)
            .Replace("{Amount}", AttributeModifier.Amount.ToString());

        combatContext.LogEffectExecution(context);

        context.Target.ModifyAttribute(AttributeModifier, remove: true);
    }
}