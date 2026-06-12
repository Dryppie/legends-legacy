using Domain.Interfaces.Combat;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.Effects;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class ModifyAttributeEffectOperationHandler : ICombatEffectOperationHandler
{
    public string Operation => CombatEffectOperation.ModifyAttribute;

    public void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        if (action.Attribute is null)
            throw new InvalidOperationException("ModifyAttribute requires an attribute.");

        effect.Target.ModifyAttribute(CombatEffectActionHelpers.CreateAttributeModifier(action));
        effect.EventType = action.Magnitude > 0 ? EventType.Buff : EventType.Debuff;
        effect.Magnitude = action.Magnitude;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Amount}", action.Magnitude.ToString());

        combatContext.LogEffectExecution(effect, CombatEffectActionHelpers.CreateSimpleCombatEntity(effect.Target));
    }

    public void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        if (action.Attribute is null) return;

        effect.Target.ModifyAttribute(CombatEffectActionHelpers.CreateAttributeModifier(action), remove: true);
        effect.EventType = action.Magnitude > 0 ? EventType.BuffExpired : EventType.DebuffExpired;
    }
}
