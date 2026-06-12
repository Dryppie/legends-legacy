using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Combat.Abilities.Effects.StatusEffects;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public class RemoveStatusAction : IEffectAction
{
    private readonly string _statusId;
    public int Magnitude => 1;

    public RemoveStatusAction(string statusId)
    {
        _statusId = statusId;
    }

    public void Execute(EffectContext effect, ICombatContext combatContext)
    {
        if (Enum.TryParse<StatusEffectType>(_statusId, ignoreCase: true, out var statusEffect))
            effect.Target.ModifyStatusEffects(statusEffect, -Magnitude);

        var matchingStatuses = effect.Target.Statuses
            .Where(x => x.Definition.Id.Equals(_statusId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var status in matchingStatuses)
            effect.Target.RemoveStatus(status);

        effect.EventType = EventType.StatusEffectExpired;
        effect.Magnitude = matchingStatuses.Count;
        effect.Details = effect.Details
            .Replace("{Actor}", effect.Source.Name)
            .Replace("{Target}", effect.Target.Name)
            .Replace("{Status}", _statusId)
            .Replace("{Amount}", effect.Magnitude.ToString());

        combatContext.LogEffectExecution(effect, CreateSimpleCombatEntity(effect.Target));
    }

    public void OnExpireExecute(EffectContext effect, ICombatContext combatContext)
    {
    }

    private static SimpleCombatEntity CreateSimpleCombatEntity(CombatEntity target) =>
        new()
        {
            Id = target.Id,
            MaxHealth = target.GetAttributeValue(AttributeType.MaxHealth),
            Health = target.GetCurrentHealthValue(),
            Barrier = target.GetAttributeValue(AttributeType.BlockEffectiveness)
        };
}
