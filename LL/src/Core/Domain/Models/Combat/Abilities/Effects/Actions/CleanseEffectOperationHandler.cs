using Domain.Interfaces.Combat;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.Effects;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class CleanseEffectOperationHandler : ICombatEffectOperationHandler
{
    public string Operation => CombatEffectOperation.Cleanse;

    public void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        var removed = effect.Target.Statuses.Count + effect.Target.StatusEffects.Count;
        effect.Target.Statuses.Clear();
        effect.Target.StatusEffects.Clear();
        effect.EventType = EventType.StatusEffectExpired;
        effect.Magnitude = removed;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Amount}", removed.ToString());

        combatContext.LogEffectExecution(effect, CombatEffectActionHelpers.CreateSimpleCombatEntity(effect.Target));
    }

    public void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
    }
}
