using Domain.Interfaces.Combat.Abilities;
using Domain.Interfaces.Combat;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Domain.Models.Combat.Abilities.Effects.Actions;

public class CleanseAction : IEffectAction
{
    public int Magnitude => 1;

    public void Execute(EffectContext effect, ICombatContext combatContext)
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
