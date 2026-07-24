using Domain.Models.Combat.Abilities;

namespace Services.LL.Combat.Engine;

public static class AbilityCompiler
{
    public static CompiledAbility CompileAbility(AbilitySpec spec)
    {
        var effectsById = spec.Effects.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var triggers = spec.Triggers.Count == 0
            ? [new AbilityTriggerSpec { Event = spec.Kind == AbilitySpecKind.Active ? AbilityTriggerEvent.OnAbilityUsed : AbilityTriggerEvent.OnCombatStart }]
            : spec.Triggers;

        var compiledTriggers = triggers
            .Select(trigger => CompileTrigger(trigger, effectsById.Values, effectsById, spec.Name, spec.Kind, spec.Tags))
            .GroupBy(x => x.Event)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<CompiledTrigger>)x.ToList());

        return new CompiledAbility
        {
            Id = spec.Id,
            Name = spec.Name,
            Kind = spec.Kind,
            CooldownTicks = spec.CooldownTicks,
            Costs = [.. spec.Costs.Select(CompileCost)],
            Tags = new HashSet<string>(spec.Tags, StringComparer.OrdinalIgnoreCase),
            TriggersByEvent = compiledTriggers
        };
    }

    public static CompiledStatus CompileStatus(StatusSpec spec)
    {
        var effectsById = spec.Effects.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var compiledTriggers = spec.Triggers
            .Select(trigger => CompileTrigger(trigger, spec.Effects, effectsById, spec.Name, AbilitySpecKind.Passive, spec.Tags))
            .GroupBy(x => x.Event)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<CompiledTrigger>)x.ToList());

        return new CompiledStatus
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

    public static IReadOnlyDictionary<string, CompiledAbility> CompileAbilities(IEnumerable<AbilitySpec> specs) =>
        specs.Select(CompileAbility).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, CompiledStatus> CompileStatuses(IEnumerable<StatusSpec> specs) =>
        specs.Select(CompileStatus).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, CompiledSummon> CompileSummons(IEnumerable<SummonSpec> specs) =>
        specs.Select(CompileSummon).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

    public static CompiledSummon CompileSummon(SummonSpec spec) =>
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

    private static CompiledTrigger CompileTrigger(
        AbilityTriggerSpec trigger,
        IEnumerable<AbilityEffectSpec> defaultEffects,
        IReadOnlyDictionary<string, AbilityEffectSpec> effectsById,
        string statsSource,
        AbilitySpecKind abilityKind,
        IReadOnlyList<string> abilityTags)
    {
        var selectedEffects = trigger.EffectIds.Count == 0
            ? defaultEffects
            : trigger.EffectIds.Select(effectId => effectsById[effectId]);

        return new CompiledTrigger
        {
            Event = trigger.Event,
            InternalCooldownTicks = trigger.InternalCooldownTicks,
            Conditions = [.. trigger.Conditions.Select(CompileCondition)],
            Effects = [.. selectedEffects.Select(effect => CompileEffect(effect, statsSource, abilityKind, abilityTags))]
        };
    }

    private static CompiledEffect CompileEffect(
        AbilityEffectSpec effect,
        string statsSource,
        AbilitySpecKind abilityKind,
        IReadOnlyList<string> abilityTags) =>
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
            SummonPowerMultiplier = effect.SummonPowerMultiplier <= 0 ? 1d : effect.SummonPowerMultiplier,
            SummonHealthMultiplier = effect.SummonHealthMultiplier <= 0 ? 1d : effect.SummonHealthMultiplier,
            Resource = effect.Resource,
            DurationTicks = effect.DurationTicks,
            IntervalTicks = effect.IntervalTicks,
            Uses = effect.Uses,
            ChancePercent = effect.ChancePercent,
            AttackType = effect.AttackType,
            DamageType = effect.DamageType,
            CritEligibility = effect.CritEligibility,
            LifeStealPercentage = effect.LifeStealPercentage,
            ProcCoefficient = effect.ProcCoefficient <= 0 ? 1m : effect.ProcCoefficient,
            AbilityKind = abilityKind,
            AbilityTags = new HashSet<string>(abilityTags, StringComparer.OrdinalIgnoreCase),
            Tags = new HashSet<string>(effect.Tags, StringComparer.OrdinalIgnoreCase),
            Conditions = [.. effect.Conditions.Select(CompileCondition)]
        };

    private static CompiledCost CompileCost(AbilityCostSpec cost) =>
        new()
        {
            Resource = cost.Resource,
            BaseValue = cost.BaseValue,
            ScalingAttribute = cost.ScalingAttribute,
            ScalingCoefficient = cost.ScalingCoefficient
        };

    private static CompiledCondition CompileCondition(AbilityConditionSpec condition) =>
        new()
        {
            Type = condition.Type,
            Subject = condition.Subject,
            StatusId = condition.StatusId,
            Tag = condition.Tag,
            Value = condition.Value
        };

    private static CompiledSummonAttribute CompileSummonAttribute(SummonAttributeSpec attribute) =>
        new()
        {
            Attribute = attribute.Attribute,
            BaseValue = attribute.BaseValue,
            ScalingAttribute = attribute.ScalingAttribute,
            ScalingCoefficient = attribute.ScalingCoefficient,
            MinimumValue = attribute.MinimumValue
        };
}
