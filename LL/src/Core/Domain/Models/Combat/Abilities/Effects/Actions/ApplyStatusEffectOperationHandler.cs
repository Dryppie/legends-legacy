using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities.Effects.Duration;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public sealed class ApplyStatusEffectOperationHandler : ICombatEffectOperationHandler
{
    public string Operation => CombatEffectOperation.ApplyStatus;

    public void Execute(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
        if (string.IsNullOrWhiteSpace(action.StatusId))
            throw new InvalidOperationException("ApplyStatus requires a status id.");

        if (!combatContext.StatusDefinitionService.TryGetById(action.StatusId, out var statusDefinition))
        {
            effect.Details = $"Status '{action.StatusId}' not found.";
            combatContext.LogEffectExecution(effect);
            return;
        }

        statusDefinition = statusDefinition.Clone();
        if (action.StatusDuration > 0)
            statusDefinition.Duration = new TimedDuration(action.StatusDuration);

        CombatEffectActionHelpers.SetSourceNameForStatusEffects(statusDefinition, effect.SourceName);
        effect.EventType = EventType.AbilityUse;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Status}", statusDefinition.Name);

        combatContext.LogEffectExecution(effect, CombatEffectActionHelpers.CreateSimpleCombatEntity(effect.Source));
        combatContext.EffectManager.AddStatus(new(statusDefinition, effect.Source, effect.Target));
    }

    public void OnExpire(CombatEffectAction action, EffectContext effect, ICombatContext combatContext)
    {
    }
}
