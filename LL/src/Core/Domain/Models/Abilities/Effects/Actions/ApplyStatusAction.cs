using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Abilities.Statuses;

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

        // Create runtime instance
        var statusInstance = new StatusInstance(statusDef.Clone(), effect.Source, effect.Target);

        // Add to target
        //effect.Target.Statuses.Add(statusInstance);
        combatContext.EffectManager.AddStatus(statusInstance);

        // Optional log message
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Status}", statusDef.Name);

        //combatContext.LogEffectExecution(effect);

        // TODO: Determine if it's even needed to publish this? Not sure what the benefits are. This is more like a nested action effect
        // Publish global status applied event
        //combatContext.EventBus.Publish(new CombatEvent
        //{
        //    Type = TriggerEvent.OnStatusApplied,
        //    Source = effect.Source,
        //    Target = effect.Target,
        //    StatusId = statusDef.Id,
        //    CurrentTime = combatContext.CurrentTime
        //});

        // If we support firing OnStatusAppliedIfThis directly (not waiting for engine sweep)
        //if (statusDef.Triggers.Any(t => t.Event == TriggerEvent.OnStatusAppliedIfThis))
        //{
        //    combatContext.EventBus.Publish(new CombatEvent
        //    {
        //        Type = TriggerEvent.OnStatusAppliedIfThis,
        //        Source = effect.Source,
        //        Target = effect.Target,
        //        StatusId = statusDef.Id,
        //        CurrentTime = combatContext.CurrentTime
        //    });
        //}
    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {

    }
}
