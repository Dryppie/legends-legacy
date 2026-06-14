using Domain.Models.AbilityDefinitions;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;

namespace Services.LL.Essences;

internal static class EssenceEvolutionModifierApplier
{
    public static AbilityDefinition Apply(
        AbilityDefinition ability,
        IReadOnlyCollection<AbilityModifierDefinition> modifiers,
        PlayerEssence essence)
    {
        if (!essence.IsEvolved || modifiers.Count == 0) return ability;

        var copy = CloneAbility(ability);

        foreach (var modifier in modifiers)
        {
            if (modifier.Operation.Equals("AddEffect", StringComparison.OrdinalIgnoreCase) && modifier.Effect is not null)
            {
                copy.Effects.Add(CloneEffect(modifier.Effect));
                continue;
            }

            var effect = copy.Effects.FirstOrDefault(x => x.Id.Equals(modifier.Target, StringComparison.OrdinalIgnoreCase));
            if (effect is null) continue;

            if (modifier.Operation.Equals("AddMultiplier", StringComparison.OrdinalIgnoreCase))
            {
                var multiplier = 1 + modifier.Value;
                effect.Scaling.BaseValue *= multiplier;
            }
            else if (modifier.Operation.Equals("AddFlat", StringComparison.OrdinalIgnoreCase))
            {
                effect.Scaling.BaseValue += modifier.Value;
            }
        }

        return copy;
    }

    private static AbilityDefinition CloneAbility(AbilityDefinition ability) =>
        new()
        {
            Id = ability.Id,
            Name = ability.Name,
            Description = ability.Description,
            CooldownSeconds = ability.CooldownSeconds,
            Kind = ability.Kind,
            Targeting = ability.Targeting,
            Tags = [.. ability.Tags],
            Triggers = [.. ability.Triggers.Select(x => new AbilityTriggerDefinition { Type = x.Type, InternalCooldownSeconds = x.InternalCooldownSeconds })],
            Conditions = [.. ability.Conditions.Select(CloneCondition)],
            Effects = [.. ability.Effects.Select(CloneEffect)]
        };

    private static AbilityEffectDefinition CloneEffect(AbilityEffectDefinition effect) =>
        new()
        {
            Id = effect.Id,
            Type = effect.Type,
            Target = effect.Target,
            Attribute = effect.Attribute,
            Status = effect.Status,
            Resource = effect.Resource,
            DurationSeconds = effect.DurationSeconds,
            IntervalSeconds = effect.IntervalSeconds,
            Uses = effect.Uses,
            AttackType = effect.AttackType,
            DamageType = effect.DamageType,
            EffectTags = [.. effect.EffectTags],
            Log = effect.Log,
            LifeStealPercentage = effect.LifeStealPercentage,
            Conditions = [.. effect.Conditions.Select(CloneCondition)],
            Scaling = new AbilityScalingFormula
            {
                BaseValue = effect.Scaling.BaseValue,
                AttributeScaling =
                [
                    .. effect.Scaling.AttributeScaling.Select(x => new AbilityAttributeScalingDefinition
                    {
                        Attribute = x.Attribute,
                        Coefficient = x.Coefficient
                    })
                ]
            }
        };

    private static AbilityConditionDefinition CloneCondition(AbilityConditionDefinition condition) =>
        new()
        {
            Type = condition.Type,
            Tag = condition.Tag,
            Status = condition.Status,
            Value = condition.Value
        };
}
