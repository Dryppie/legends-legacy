using Domain.Interfaces.Combat;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.Effects;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class ModifyStatusEffectOperationHandler : ICombatEffectOperationHandler
{
    public string Operation => CombatEffectOperation.ModifyStatusEffect;

    public void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        CombatEffectActionHelpers.ModifyStatusEffect(action, effect, action.Magnitude);
        effect.EventType = EventType.StatusEffect;
        effect.Magnitude = action.Magnitude;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Status}", action.StatusId ?? string.Empty)
            .Replace("{Amount}", action.Magnitude.ToString());

        combatContext.LogEffectExecution(effect, CombatEffectActionHelpers.CreateSimpleCombatEntity(effect.Target));
    }

    public void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext) =>
        CombatEffectActionHelpers.ModifyStatusEffect(action, effect, -action.Magnitude);
}
