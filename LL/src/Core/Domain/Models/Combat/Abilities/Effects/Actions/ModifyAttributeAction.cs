using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Actions;
public class ModifyAttributeAction : IEffectAction
{
    public AbilityAttributeModifier AttributeModifier;
    public bool Stackable;
    public int Magnitude => 1;

    public ModifyAttributeAction(AbilityAttributeModifier attributeModifier, bool stackable)
    {
        AttributeModifier = attributeModifier;
        Stackable = stackable;
    }

    public void Execute(EffectContext effect, ICombatContext combatContext)
    {
        var target = effect.Target;

        //var existingEffect = combatContext.EffectManager.FindEffectForEntity(target, effect.Effect.Definition.SourceId);

        //if (existingEffect != null && !Stackable)
        //{
        //    // If an effect exists, renew it, but only if it isn't stackable
        //    combatContext.EffectManager.RenewEffect(existingEffect);
        //    return;
        //}

        // If an effect doesn't exist, always apply it
        target.ModifyAttribute(AttributeModifier);

        effect.EventType = AttributeModifier.Amount > 0 ? EventType.Buff : EventType.Debuff;
        LogEvent(effect, combatContext, target);
    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {
        var target = effect.Target;

        effect.Target.ModifyAttribute(AttributeModifier, remove: true);

        //At the moment it isn't necessary to log the expiration of an effect. It simply just have to disappear
        effect.EventType = AttributeModifier.Amount > 0 ? EventType.BuffExpired : EventType.DebuffExpired;
        //LogEvent(effect, combatContext, target);
    }

    private void LogEvent(EffectContext context, ICombatContext combatContext, CombatEntity target)
    {
        // The details will simply display the value that the buff/debuff changes, so it's easily understandable
        // ie. 'HP increasd by 25%', rather than 'HP increased by 171'.
        // Magnitude will tell exactly how much it changed
        context.Magnitude = context.Magnitude;
        //context.Attribute = AttributeModifier.AttributeType;
        context.Details = context.Details
            .Replace("{Actor}", context.Source.Name)
            .Replace("{Target}", target.Name)
            .Replace("{Amount}", AttributeModifier.Amount.ToString());

        var simpleCombatEntity = new SimpleCombatEntity()
        {
            Id = target.Id,
            MaxHealth = target.GetAttributeValue(AttributeType.MaxHealth),
            Health = target.GetCurrentHealthValue(),
            Barrier = target.GetAttributeValue(AttributeType.BlockEffectiveness)
        };

        combatContext.LogEffectExecution(context, simpleCombatEntity);
    }
}