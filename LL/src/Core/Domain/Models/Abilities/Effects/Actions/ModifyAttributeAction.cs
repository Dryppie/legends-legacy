using Domain.Interfaces;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class ModifyAttributeAction : IEffectAction
{
    public AttributeModifier AttributeModifier;
    public bool IsStackable;
    public int Magnitude => 1;

    public ModifyAttributeAction(AttributeModifier attributeModifier, bool isStackable)
    {
        AttributeModifier = attributeModifier;
        IsStackable = isStackable;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        var target = context.Target;

        target.ModifyAttribute(AttributeModifier);

        context.EventType = AttributeModifier.Amount > 0 ? EventType.Buff : EventType.Debuff;

        LogEvent(context, combatContext, target);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        var target = context.Target;

        context.Target.ModifyAttribute(AttributeModifier, remove: true);

        context.EventType = AttributeModifier.Amount > 0 ? EventType.BuffExpired : EventType.DebuffExpired;

        LogEvent(context, combatContext, target);
    }

    private void LogEvent(EffectContext context, ICombatContext combatContext, CombatEntity target)
    {
        // The details will simply display the value that the buff/debuff changes, so it's easily understandable
        // ie. 'HP increasd by 25%', rather than 'HP increased by 171'.
        // Magnitude will tell exactly how much it changed
        context.Magnitude = context.Magnitude;
        context.Attribute = AttributeModifier.AttributeType;
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
}