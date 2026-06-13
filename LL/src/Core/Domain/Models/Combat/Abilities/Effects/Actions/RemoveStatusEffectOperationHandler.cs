using Domain.Interfaces.Combat;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class RemoveStatusEffectOperationHandler : ICombatEffectOperationHandler
{
    public string Operation => CombatEffectOperation.RemoveStatus;

    public void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        if (string.IsNullOrWhiteSpace(action.StatusId))
            throw new InvalidOperationException("RemoveStatus requires a status id.");

        if (Enum.TryParse<StatusEffectType>(action.StatusId, ignoreCase: true, out var statusEffect))
            effect.Target.ModifyStatusEffects(statusEffect, -Math.Max(1, action.Magnitude));

        var matchingStatuses = effect.Target.Statuses
            .Where(x => x.Definition.Id.Equals(action.StatusId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var status in matchingStatuses)
            effect.Target.RemoveStatus(status);

        effect.EventType = EventType.StatusEffectExpired;
        effect.Magnitude = matchingStatuses.Count;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Status}", action.StatusId)
            .Replace("{Amount}", effect.Magnitude.ToString());

        combatContext.LogEffectExecution(effect, CombatEffectActionHelpers.CreateSimpleCombatEntity(effect.Target));
    }

    public void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
    }
}
