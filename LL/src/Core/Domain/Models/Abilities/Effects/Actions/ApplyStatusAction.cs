using Domain.Interfaces.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Abilities.Statuses;
using Domain.Models.Attributes;
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
            var simpleCombatEntity = CreateSimpleCombatEntity(effect.Source);

            combatContext.LogEffectExecution(effect, simpleCombatEntity);
        }

        // Create runtime instance
        var statusInstance = new StatusInstance(statusDef.Clone(), effect.Source, effect.Target);

        // Add to target
        //effect.Target.Statuses.Add(statusInstance);
        combatContext.EffectManager.AddStatus(statusInstance);

    }

    private SimpleCombatEntity CreateSimpleCombatEntity(CombatEntity target)
    {
        return new SimpleCombatEntity()
        {
            Id = target.Id,
            MaxHealth = target.GetAttributeValue(AttributeType.MaxHealth),
            Health = target.GetAttributeValue(AttributeType.Health),
            MaxMana = target.GetAttributeValue(AttributeType.MaxMana),
            Mana = target.GetAttributeValue(AttributeType.Mana),
            Barrier = target.GetAttributeValue(AttributeType.Barrier)
        };
    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {

    }
}
