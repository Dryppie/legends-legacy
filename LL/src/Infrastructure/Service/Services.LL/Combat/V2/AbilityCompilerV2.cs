using Domain.Models.Combat.Abilities.V2;

namespace Services.LL.Combat.V2;

public static class AbilityCompilerV2
{
    public static CompiledAbilityV2 CompileAbility(AbilitySpec spec)
    {
        var effectsById = spec.Effects.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var triggers = spec.Triggers.Count == 0
            ? [new AbilityTriggerSpec { Event = spec.Kind == AbilitySpecKind.Active ? AbilityTriggerEvent.OnAbilityUsed : AbilityTriggerEvent.OnCombatStart }]
            : spec.Triggers;

        var compiledTriggers = triggers
            .Select(trigger => CompileTrigger(trigger, effectsById.Values, effectsById, spec.Name))
            .GroupBy(x => x.Event)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<CompiledTriggerV2>)x.ToList());

        return new CompiledAbilityV2
        {
            Id = spec.Id,
            Name = spec.Name,
            Kind = spec.Kind,
            CooldownTicks = spec.CooldownTicks,
            Tags = new HashSet<string>(spec.Tags, StringComparer.OrdinalIgnoreCase),
            TriggersByEvent = compiledTriggers
        };
    }

    public static CompiledStatusV2 CompileStatus(StatusSpec spec)
    {
        var effectsById = spec.Effects.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var compiledTriggers = spec.Triggers
            .Select(trigger => CompileTrigger(trigger, spec.Effects, effectsById, spec.Name))
            .GroupBy(x => x.Event)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<CompiledTriggerV2>)x.ToList());

        return new CompiledStatusV2
        {
            Id = spec.Id,
            Name = spec.Name,
            Tags = new HashSet<string>(spec.Tags, StringComparer.OrdinalIgnoreCase),
            StackingPolicy = spec.StackingPolicy,
            MaxStacks = spec.MaxStacks,
            DurationTicks = spec.DurationTicks,
            TriggersByEvent = compiledTriggers
        };
    }

    public static IReadOnlyDictionary<string, CompiledAbilityV2> CompileAbilities(IEnumerable<AbilitySpec> specs) =>
        specs.Select(CompileAbility).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, CompiledStatusV2> CompileStatuses(IEnumerable<StatusSpec> specs) =>
        specs.Select(CompileStatus).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, CompiledSummonV2> CompileSummons(IEnumerable<SummonSpec> specs) =>
        specs.Select(CompileSummon).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

    public static CompiledSummonV2 CompileSummon(SummonSpec spec) =>
        new()
        {
            Id = spec.Id,
            Name = spec.Name,
            ImagePath = spec.ImagePath,
            DurationTicks = spec.DurationTicks,
            MaxActive = spec.MaxActive,
            Tags = new HashSet<string>(spec.Tags, StringComparer.OrdinalIgnoreCase),
            AbilityIds = spec.AbilityIds,
            Attributes = [.. spec.Attributes.Select(CompileSummonAttribute)]
        };

    private static CompiledTriggerV2 CompileTrigger(
        AbilityTriggerSpec trigger,
        IEnumerable<AbilityEffectSpec> defaultEffects,
        IReadOnlyDictionary<string, AbilityEffectSpec> effectsById,
        string statsSource)
    {
        var selectedEffects = trigger.EffectIds.Count == 0
            ? defaultEffects
            : trigger.EffectIds.Select(effectId => effectsById[effectId]);

        return new CompiledTriggerV2
        {
            Event = trigger.Event,
            InternalCooldownTicks = trigger.InternalCooldownTicks,
            Conditions = [.. trigger.Conditions.Select(CompileCondition)],
            Effects = [.. selectedEffects.Select(effect => CompileEffect(effect, statsSource))]
        };
    }

    private static CompiledEffectV2 CompileEffect(AbilityEffectSpec effect, string statsSource) =>
        new()
        {
            Id = effect.Id,
            StatsSource = statsSource,
            Operation = effect.Operation,
            Target = effect.Target,
            BaseValue = effect.BaseValue,
            ScalingAttribute = effect.ScalingAttribute,
            ScalingCoefficient = effect.ScalingCoefficient,
            Attribute = effect.Attribute,
            StatusId = effect.StatusId,
            SummonId = effect.SummonId,
            Resource = effect.Resource,
            DurationTicks = effect.DurationTicks,
            IntervalTicks = effect.IntervalTicks,
            Uses = effect.Uses,
            ChancePercent = effect.ChancePercent,
            AttackType = effect.AttackType,
            DamageType = effect.DamageType,
            LifeStealPercentage = effect.LifeStealPercentage,
            Conditions = [.. effect.Conditions.Select(CompileCondition)]
        };

    private static CompiledConditionV2 CompileCondition(AbilityConditionSpec condition) =>
        new()
        {
            Type = condition.Type,
            Subject = condition.Subject,
            StatusId = condition.StatusId,
            Tag = condition.Tag,
            Value = condition.Value
        };

    private static CompiledSummonAttributeV2 CompileSummonAttribute(SummonAttributeSpec attribute) =>
        new()
        {
            Attribute = attribute.Attribute,
            BaseValue = attribute.BaseValue,
            ScalingAttribute = attribute.ScalingAttribute,
            ScalingCoefficient = attribute.ScalingCoefficient,
            MinimumValue = attribute.MinimumValue
        };
}
