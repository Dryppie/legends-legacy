using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Statuses;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Effects.Actions;
public class ApplyStatusAction : IEffectAction
{
    public string StatusId { get; set; } = string.Empty;

    public ApplyStatusAction(string statusId)
    {
        StatusId = statusId;
    }

    public void Execute(EffectContext effect, ICombatContext combatContext)
    {
        // Lookup status definition
        if (!combatContext.StatusDefinitionService.TryGetById(StatusId, out var statusDef))
        {
            effect.Details = $"Status '{StatusId}' not found.";
            combatContext.LogEffectExecution(effect); // Optional logging
            return;
        }

        // Optional log message
        if (!string.IsNullOrEmpty(effect.Details))
        {
            effect.EventType = EventType.AbilityUse;
            effect.Details = effect.Details
                .Replace("{Actor}", effect.Source.Name)
                .Replace("{Target}", effect.Target.Name)
                .Replace("{Status}", statusDef.Name);

            combatContext.LogEffectExecution(effect);
        }

        // Create runtime instance
        var statusInstance = new StatusInstance(statusDef.Clone(), effect.Source, effect.Target);

        // Add to target
        //effect.Target.Statuses.Add(statusInstance);
        combatContext.EffectManager.AddStatus(statusInstance);

    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {

    }
}
