using Domain.Models.Combat.Abilities;

namespace Services.LL.Combat.Engine;

public sealed partial class FastCombatEngine
{
    private sealed class AbilityDamageCastContext
    {
        public double Multiplier { get; set; } = 1d;
        public Dictionary<CompiledTrigger, int> Choices { get; } = [];
    }

    private AbilityDamageCastContext? PrepareDamageCast(
        RuntimeCombatant actor,
        RuntimeAbility ability,
        RuntimeCombatant? target,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (!ability.Definition.TriggersByEvent.TryGetValue(AbilityTriggerEvent.OnAbilityUsed, out var triggers))
            return null;
        var observesDamageCasts = HasPotentialListener(AbilityTriggerEvent.OnBeforeDamageAbility);
        if (!observesDamageCasts && !triggers.Any(x => x.ChooseOneEffect))
            return null;

        var context = new AbilityDamageCastContext();

        var castEvent = new CombatEvent(AbilityTriggerEvent.OnBeforeDamageAbility, actor, target,
            ability.Definition.Id, DamageCast: context);
        var dealsDamage = false;
        foreach (var trigger in triggers)
        {
            var choice = trigger.ChooseOneEffect ? _random.Next(trigger.Effects.Count) : -1;
            if (choice >= 0)
                context.Choices[trigger] = choice;
            for (var index = 0; index < trigger.Effects.Count; index++)
            {
                if (choice >= 0 && index != choice)
                    continue;
                var effect = trigger.Effects[index];
                if (observesDamageCasts && !dealsDamage
                    && IsDamagingCastEffect(effect, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                    dealsDamage = true;
            }
        }

        // Resolve consumption before dispatching any active effects, independently of loadout order.
        if (dealsDamage)
            Publish(castEvent, combatants);
        return context;
    }

    private bool IsDamagingCastEffect(CompiledEffect effect, HashSet<string> visitedStatuses)
    {
        if (effect.Operation is AbilityEffectOperation.Damage or AbilityEffectOperation.PerformBasicAttack
            or AbilityEffectOperation.ConsumeConditionStacks)
            return true;
        if (effect.Operation is AbilityEffectOperation.ApplyCondition or AbilityEffectOperation.ApplyRandomCondition)
            return IsDamageCondition(effect.Condition) || IsDamageCondition(effect.AlternativeCondition);
        return effect.Operation == AbilityEffectOperation.ApplyStatus
            && effect.StatusId is { } statusId && visitedStatuses.Add(statusId)
            && _statusesById.TryGetValue(statusId, out var status)
            && status.TriggersByEvent.Values.SelectMany(x => x).SelectMany(x => x.Effects)
                .Any(x => IsDamagingCastEffect(x, visitedStatuses));
    }

    private static bool IsDamageCondition(StandardConditionType? condition) =>
        condition is StandardConditionType.Burn or StandardConditionType.Poison
            or StandardConditionType.Bleed or StandardConditionType.Doom;

    private int CleanseNegativeEffects(
        RuntimeCombatant source,
        RuntimeCombatant target,
        int maximum,
        IReadOnlyList<RuntimeCombatant> combatants)
    {
        var removed = 0;
        // Oldest standard condition first. Shared conditions remove as one effect;
        // independently timed conditions remove one instance.
        foreach (var condition in target.Conditions.Where(x => IsHarmfulCondition(x.Type))
                     .OrderBy(x => x.ApplicationOrder).ToArray())
        {
            if (removed >= maximum)
                return removed;
            if (!target.Conditions.Contains(condition))
                continue;
            RemoveCondition(source, target, condition, ConditionRemovalReason.Cleansed, combatants);
            removed++;
        }

        foreach (var status in target.Statuses.Where(x => IsNegativeStatus(x.Definition)).ToArray())
        {
            if (removed >= maximum)
                break;
            if (status.IsRemovalLocked || !target.Statuses.Contains(status))
                continue;
            RemoveStatusInstance(source, target, status, ConditionRemovalReason.Cleansed, combatants);
            removed++;
        }
        return removed;
    }

    private static bool IsNegativeStatus(CompiledStatus status) =>
        status.Tags.Any(tag => tag.Equals("Status.Debuff", StringComparison.OrdinalIgnoreCase)
            || tag.StartsWith("Debuff", StringComparison.OrdinalIgnoreCase)
            || tag.StartsWith("Affliction", StringComparison.OrdinalIgnoreCase)
            || tag.StartsWith("Control.", StringComparison.OrdinalIgnoreCase)
            || tag is "Status.Curse" or "Status.Poison");
}
