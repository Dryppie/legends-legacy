using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class ModifyAttributeAction : IEffectAction
{
    public AttributeModifier AttributeModifier;
    public bool Stackable;
    public int Magnitude => 1;

    public ModifyAttributeAction(AttributeModifier attributeModifier, bool stackable)
    {
        AttributeModifier = attributeModifier;
        Stackable = stackable;
    }

    public void Execute(EffectContext context, ICombatContext combatContext)
    {
        var target = context.Target;
        
        var existingEffect = combatContext.EffectManager.FindEffectForEntity(target, context.Effect.Definition.SourceId);
        
        if (existingEffect != null && !Stackable) 
        {
            // If an effect exists, renew it, but only if it isn't stackable
            combatContext.EffectManager.RenewEffect(existingEffect);
            return;
        }

        // If an effect doesn't exist, always apply it
        target.ModifyAttribute(AttributeModifier);

        context.EventType = AttributeModifier.Amount > 0 ? EventType.Buff : EventType.Debuff;
        LogEvent(context, combatContext, target);
    }

    public void OnExpireExecute(EffectContext context, ICombatContext combatContext)
    {
        var target = context.Target;

        context.Target.ModifyAttribute(AttributeModifier, remove: true);

        // At the moment it isn't necessary to log the expiration of an effect. It simply just have to disappear
        // context.EventType = AttributeModifier.Amount > 0 ? EventType.BuffExpired : EventType.DebuffExpired;
        // LogEvent(context, combatContext, target);
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
            Mana = target.GetAttributeValue(AttributeType.Mana),
            Barrier = target.GetAttributeValue(AttributeType.Barrier)
        };

        combatContext.LogEffectExecution(context, simpleCombatEntity);
    }
}