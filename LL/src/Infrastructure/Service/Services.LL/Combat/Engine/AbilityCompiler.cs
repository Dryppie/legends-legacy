using Domain.Models.Combat.Abilities;

namespace Services.LL.Combat.Engine;

public static class AbilityCompiler
{
    public static CompiledAbility CompileAbility(AbilitySpec spec) => CompileAbility(spec, threatTuning: null);

    public static CompiledAbility CompileAbility(AbilitySpec spec, AbilityThreatTuning? threatTuning)
    {
        var effectsById = spec.Effects.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var triggers = spec.Triggers.Count == 0
            ? [new AbilityTriggerSpec { Event = spec.Kind == AbilitySpecKind.Active ? AbilityTriggerEvent.OnAbilityUsed : AbilityTriggerEvent.OnCombatStart }]
            : spec.Triggers;

        var compiledTriggers = triggers
            .Select(trigger => CompileTrigger(
                trigger,
                effectsById.Values,
                effectsById,
                spec.Name,
                spec.Kind,
                spec.Tags,
                spec.Kind == AbilitySpecKind.Passive
                    ? AbilityThreatRules.GetThreatValue(spec, trigger, threatTuning)
                    : 0))
            .GroupBy(x => x.Event)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<CompiledTrigger>)x.ToList());

        return new CompiledAbility
        {
            Id = spec.Id,
            Name = spec.Name,
            Kind = spec.Kind,
            CooldownTicks = spec.CooldownTicks,
            ThreatValue = AbilityThreatRules.GetThreatValue(spec, threatTuning),
            ThreatMultiplier = Math.Max(0, spec.ThreatMultiplier),
            Costs = [.. spec.Costs.Select(CompileCost)],
            Tags = new HashSet<string>(spec.Tags, StringComparer.OrdinalIgnoreCase),
            TriggersByEvent = compiledTriggers
        };
    }

    public static CompiledStatus CompileStatus(StatusSpec spec)
    {
        var effectsById = spec.Effects.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var compiledTriggers = spec.Triggers
            .Select(trigger => CompileTrigger(
                trigger,
                spec.Effects,
                effectsById,
                spec.Name,
                AbilitySpecKind.Passive,
                spec.Tags,
                threatValue: 0))
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
            LockAtMaxStacks = spec.LockAtMaxStacks,
            SourceDamageTakenPercentPerStack = spec.SourceDamageTakenPercentPerStack,
            TriggersByEvent = compiledTriggers
        };
    }

    public static IReadOnlyDictionary<string, CompiledAbility> CompileAbilities(
        IEnumerable<AbilitySpec> specs,
        AbilityThreatTuning? threatTuning = null) =>
        specs.Select(spec => CompileAbility(spec, threatTuning))
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, CompiledStatus> CompileStatuses(IEnumerable<StatusSpec> specs) =>
        specs.Select(CompileStatus).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, CompiledSummon> CompileSummons(
        IEnumerable<SummonSpec> specs,
        AbilityThreatTuning? threatTuning = null) =>
        specs.Select(spec => CompileSummon(spec, threatTuning))
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

    public static CompiledSummon CompileSummon(SummonSpec spec) => CompileSummon(spec, threatTuning: null);

    public static CompiledSummon CompileSummon(SummonSpec spec, AbilityThreatTuning? threatTuning) =>
        new()
        {
            Id = spec.Id,
            Name = spec.Name,
            ImagePath = spec.ImagePath,
            DurationTicks = spec.DurationTicks,
            MaxActive = spec.MaxActive,
            CanBasicAttack = spec.CanBasicAttack,
            ThreatMultiplier = Math.Max(
                0,
                spec.ThreatMultiplier ?? threatTuning?.DefaultSummonThreatMultiplier
                    ?? AbilityThreatTuning.Default.DefaultSummonThreatMultiplier),
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
        IReadOnlyList<string> abilityTags,
        int threatValue)
    {
        var selectedEffects = trigger.EffectIds.Count == 0
            ? defaultEffects
            : trigger.EffectIds.Select(effectId => effectsById[effectId]);

        return new CompiledTrigger
        {
            Event = trigger.Event,
            ThreatValue = threatValue,
            ThreatInternalCooldownTicks = threatValue != 0
                && trigger.InternalCooldownTicks <= 0
                && IsReactiveThreatEvent(trigger.Event)
                    ? 40
                    : 0,
            InternalCooldownTicks = trigger.InternalCooldownTicks,
            InitialDelayTicks = trigger.InitialDelayTicks,
            EveryNthOccurrence = Math.Max(1, trigger.EveryNthOccurrence),
            Conditions = [.. trigger.Conditions.Select(CompileCondition)],
            Effects = [.. selectedEffects.Select(effect => CompileEffect(effect, statsSource, abilityKind, abilityTags))]
        };
    }

    private static bool IsReactiveThreatEvent(AbilityTriggerEvent triggerEvent) => triggerEvent is
        AbilityTriggerEvent.OnHit
        or AbilityTriggerEvent.OnDamaged
        or AbilityTriggerEvent.OnAttacked
        or AbilityTriggerEvent.OnMeleeAttacked
        or AbilityTriggerEvent.OnRangedAttacked
        or AbilityTriggerEvent.OnBarrierAbsorbed
        or AbilityTriggerEvent.OnBarrierBroken
        or AbilityTriggerEvent.OnBarrierContributionBroken;

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
            ScalingAttributeSubject = effect.ScalingAttributeSubject,
            ScalingCoefficient = effect.ScalingCoefficient,
            MaximumScalingCoefficient = effect.MaximumScalingCoefficient,
            EventMagnitudeCoefficient = effect.EventMagnitudeCoefficient,
            ScalingCondition = effect.ScalingCondition,
            ConditionScalingCoefficient = effect.ConditionScalingCoefficient,
            ScalingStatusId = effect.ScalingStatusId,
            ScalingStatusSubject = effect.ScalingStatusSubject,
            StatusScalingAttribute = effect.StatusScalingAttribute,
            StatusScalingCoefficient = effect.StatusScalingCoefficient,
            HealingScalingAttribute = effect.HealingScalingAttribute,
            HealingScalingCoefficient = effect.HealingScalingCoefficient,
            MaximumHealingScalingCoefficient = effect.MaximumHealingScalingCoefficient,
            Attribute = effect.Attribute,
            StatusId = effect.StatusId,
            Condition = effect.Condition,
            AlternativeCondition = effect.AlternativeCondition,
            SummonId = effect.SummonId,
            RepeatCount = Math.Max(1, effect.RepeatCount),
            HealthStepPercent = effect.HealthStepPercent,
            RepeatPerOwnedSummonId = effect.RepeatPerOwnedSummonId,
            ScalingOwnedSummonId = effect.ScalingOwnedSummonId,
            OwnedSummonScalingCoefficient = effect.OwnedSummonScalingCoefficient,
            SummonGroupId = effect.SummonGroupId,
            LinkedEffectId = effect.LinkedEffectId,
            SummonPowerMultiplier = effect.SummonPowerMultiplier <= 0 ? 1d : effect.SummonPowerMultiplier,
            SummonHealthMultiplier = effect.SummonHealthMultiplier <= 0 ? 1d : effect.SummonHealthMultiplier,
            Resource = effect.Resource,
            DurationTicks = effect.DurationTicks,
            IntervalTicks = effect.IntervalTicks,
            Uses = effect.Uses,
            OncePerTarget = effect.OncePerTarget,
            LivingNonSummonedAllyDamagePercent = effect.LivingNonSummonedAllyDamagePercent,
            SubsequentTargetDamagePercent = effect.SubsequentTargetDamagePercent <= 0
                ? 100
                : effect.SubsequentTargetDamagePercent,
            ChancePercent = effect.ChancePercent,
            AttackType = effect.AttackType,
            DamageType = effect.DamageType,
            CritEligibility = effect.CritEligibility,
            CritChanceBonus = effect.CritChanceBonus,
            ArmorPenetrationBonus = effect.ArmorPenetrationBonus,
            LifeStealPercentage = effect.LifeStealPercentage,
            LifeStealTargetCondition = effect.LifeStealTargetCondition,
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
            Condition = condition.Condition,
            DamageType = condition.DamageType,
            AttackType = condition.AttackType,
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
