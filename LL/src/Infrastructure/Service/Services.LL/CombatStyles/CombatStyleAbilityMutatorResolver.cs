using Application.Interfaces.Services.LL.CombatStyles;
using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;

namespace Services.LL.CombatStyles;

public sealed class CombatStyleAbilityMutatorResolver
{
    private readonly ICombatStyleDefinitionProvider _definitions;

    public CombatStyleAbilityMutatorResolver(ICombatStyleDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public AbilitySpec ApplyMutators(AbilitySpec spec, CombatStyleSnapshot? snapshot)
    {
        if (snapshot?.NodeRanks is not { Count: > 0 } nodeRanks)
            return spec;

        var definition = _definitions.GetById(snapshot.StyleId);
        if (definition is null)
            return spec;

        AbilitySpec? clone = null;
        var usedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in SelectActiveMutatorNodes(definition, nodeRanks))
        {
            var mutator = node.Mutator;
            if (mutator is null)
                continue;

            if (mutator.Groups.Any(group => usedGroups.Contains(group)))
                continue;

            if (!CanApplyMutator(clone ?? spec, mutator))
                continue;

            clone ??= CloneAbilitySpec(spec);
            if (ApplyMutator(clone, mutator))
            {
                foreach (var group in mutator.Groups.Where(group => !string.IsNullOrWhiteSpace(group)))
                    usedGroups.Add(group);
            }
        }

        return clone ?? spec;
    }

    private static IEnumerable<CombatStyleTreeNodeDefinition> SelectActiveMutatorNodes(
        CombatStyleDefinition definition,
        IReadOnlyDictionary<string, int> nodeRanks) =>
        definition.SkillTreeNodes
            .Where(node => node.Mutator is not null)
            .Where(node => nodeRanks.GetValueOrDefault(node.Id) > 0)
            .OrderBy(node => node.Row)
            .ThenBy(node => node.X);

    private static bool CanApplyMutator(AbilitySpec spec, CombatStyleAbilityMutatorDefinition mutator)
    {
        var conditions = mutator.Conditions;
        if (conditions.ActiveAbilityOnly && spec.Kind != AbilitySpecKind.Active)
            return false;

        if (conditions.PassiveAbilityOnly && spec.Kind != AbilitySpecKind.Passive)
            return false;

        if (!FlagMatches(conditions.AllowDamageTypeConversionRequired, spec.ConversionFlags.AllowDamageTypeConversion)
            || !FlagMatches(conditions.AllowScalingConversionRequired, spec.ConversionFlags.AllowScalingConversion)
            || !FlagMatches(conditions.AllowDeliveryConversionRequired, spec.ConversionFlags.AllowDeliveryConversion)
            || !FlagMatches(conditions.AllowTargetingConversionRequired, spec.ConversionFlags.AllowTargetingConversion)
            || !FlagMatches(conditions.AllowSummonProxyRequired, spec.ConversionFlags.AllowSummonProxy)
            || !FlagMatches(conditions.AllowEquipmentOverrideRequired, spec.ConversionFlags.AllowEquipmentOverride))
        {
            return false;
        }

        if (spec.IsHardCrowdControl && conditions.ExcludeHardCrowdControl)
            return false;

        var abilityTags = SelectAbilityTags(spec).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!ContainsAll(abilityTags, conditions.RequiredAbilityTags)
            || !ContainsAnyIfSpecified(abilityTags, conditions.AnyAbilityTags)
            || !ContainsAll(spec.DeliveryTags, conditions.RequiredDeliveryTags)
            || !ContainsAnyIfSpecified(spec.DeliveryTags, conditions.AnyDeliveryTags)
            || !ContainsAll(spec.EffectTags, conditions.RequiredEffectTags)
            || !ContainsAnyIfSpecified(spec.EffectTags, conditions.AnyEffectTags))
        {
            return false;
        }

        return spec.Effects.Any(effect => EffectMatches(effect, conditions));
    }

    private static bool ApplyMutator(AbilitySpec spec, CombatStyleAbilityMutatorDefinition mutator)
    {
        var transform = mutator.Transform;
        var tradeoff = mutator.Tradeoff;
        var applied = false;

        AddTags(spec.Tags, transform.AddAbilityTags);
        AddTags(spec.DeliveryTags, transform.AddDeliveryTags);
        AddTags(spec.EffectTags, transform.AddEffectTags);

        if (transform.CooldownMultiplier is not null || tradeoff.CooldownMultiplier is not null)
        {
            spec.CooldownTicks = ScaleValue(
                spec.CooldownTicks,
                CombineMultipliers(transform.CooldownMultiplier, tradeoff.CooldownMultiplier));
            applied = true;
        }

        if (transform.ResourceCostMultiplier is not null || tradeoff.ResourceCostMultiplier is not null)
        {
            var multiplier = CombineMultipliers(transform.ResourceCostMultiplier, tradeoff.ResourceCostMultiplier);
            foreach (var cost in spec.Costs)
            {
                cost.BaseValue = ScaleValue(cost.BaseValue, multiplier);
                cost.ScalingCoefficient *= (float)multiplier;
            }

            applied = true;
        }

        foreach (var effect in spec.Effects.Where(effect => EffectMatches(effect, mutator.Conditions)))
        {
            ApplyEffectTransform(effect, transform, tradeoff);
            applied = true;
        }

        return applied
            || transform.AddAbilityTags.Count > 0
            || transform.AddDeliveryTags.Count > 0
            || transform.AddEffectTags.Count > 0;
    }

    private static void ApplyEffectTransform(
        AbilityEffectSpec effect,
        CombatStyleMutatorTransformDefinition transform,
        CombatStyleMutatorTradeoffDefinition tradeoff)
    {
        if (transform.DamageType is not null)
            effect.DamageType = transform.DamageType.Value;

        if (transform.TargetingType is not null)
            effect.Target = transform.TargetingType.Value;

        if (transform.ScalingAttribute is not null)
            effect.ScalingAttribute = transform.ScalingAttribute.Value;

        if (transform.ScalingCoefficientOverride is not null)
            effect.ScalingCoefficient = transform.ScalingCoefficientOverride.Value;

        if (transform.ScalingCoefficientMultiplier is not null)
            effect.ScalingCoefficient *= (float)transform.ScalingCoefficientMultiplier.Value;

        var potencyMultiplier = CombineMultipliers(transform.EffectPotencyMultiplier, tradeoff.EffectPotencyMultiplier);
        if (potencyMultiplier != 1m)
        {
            effect.BaseValue = ScaleValue(effect.BaseValue, potencyMultiplier);
            effect.ScalingCoefficient *= (float)potencyMultiplier;
        }

        if (tradeoff.ProcCoefficientMultiplier is not null)
            effect.ProcCoefficient *= tradeoff.ProcCoefficientMultiplier.Value;

        AddTags(effect.Tags, transform.AddEffectTagsToMatchingEffects);
    }

    private static bool EffectMatches(
        AbilityEffectSpec effect,
        CombatStyleMutatorConditionDefinition conditions)
    {
        if (conditions.ExcludeTrueDamage
            && effect.Tags.Contains("TrueDamage", StringComparer.OrdinalIgnoreCase)
            && !conditions.AllowedDamageTypes.Contains(effect.DamageType))
        {
            return false;
        }

        if (conditions.EffectOperations.Count > 0 && !conditions.EffectOperations.Contains(effect.Operation))
            return false;

        if (conditions.AllowedDamageTypes.Count > 0 && !conditions.AllowedDamageTypes.Contains(effect.DamageType))
            return false;

        if (conditions.TargetSelectors.Count > 0 && !conditions.TargetSelectors.Contains(effect.Target))
            return false;

        return true;
    }

    private static IEnumerable<string> SelectAbilityTags(AbilitySpec spec) =>
        spec.Tags
            .Concat(spec.DeliveryTags)
            .Concat(spec.EffectTags)
            .Concat(spec.Effects.SelectMany(effect => effect.Tags));

    private static bool FlagMatches(bool? required, bool actual) =>
        required is null || required.Value == actual;

    private static bool ContainsAll(IEnumerable<string> available, IEnumerable<string> required)
    {
        var set = available.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return required.All(set.Contains);
    }

    private static bool ContainsAnyIfSpecified(IEnumerable<string> available, IReadOnlyList<string> required)
    {
        if (required.Count == 0)
            return true;

        var set = available.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return required.Any(set.Contains);
    }

    private static void AddTags(List<string> target, IEnumerable<string> tags)
    {
        foreach (var tag in tags.Where(tag => !string.IsNullOrWhiteSpace(tag)))
        {
            if (!target.Contains(tag, StringComparer.OrdinalIgnoreCase))
                target.Add(tag);
        }
    }

    private static decimal CombineMultipliers(decimal? first, decimal? second) =>
        (first ?? 1m) * (second ?? 1m);

    private static int ScaleValue(int value, decimal multiplier) =>
        (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero);

    private static AbilitySpec CloneAbilitySpec(AbilitySpec spec) =>
        new()
        {
            Id = spec.Id,
            Kind = spec.Kind,
            Name = spec.Name,
            Description = spec.Description,
            OwningEssenceId = spec.OwningEssenceId,
            CooldownTicks = spec.CooldownTicks,
            Tags = [.. spec.Tags],
            DeliveryTags = [.. spec.DeliveryTags],
            EffectTags = [.. spec.EffectTags],
            TargetingType = spec.TargetingType,
            Scaling = new Dictionary<AttributeType, float>(spec.Scaling),
            ConversionFlags = CloneConversionFlags(spec.ConversionFlags),
            IsHardCrowdControl = spec.IsHardCrowdControl,
            CanEcho = spec.CanEcho,
            CanRepeat = spec.CanRepeat,
            CanTriggerWeaponEffects = spec.CanTriggerWeaponEffects,
            Costs = [.. spec.Costs.Select(CloneCost)],
            Triggers = [.. spec.Triggers.Select(CloneTrigger)],
            Effects = [.. spec.Effects.Select(CloneEffect)]
        };

    private static AbilityConversionFlags CloneConversionFlags(AbilityConversionFlags flags) =>
        new()
        {
            AllowDamageTypeConversion = flags.AllowDamageTypeConversion,
            AllowScalingConversion = flags.AllowScalingConversion,
            AllowDeliveryConversion = flags.AllowDeliveryConversion,
            AllowTargetingConversion = flags.AllowTargetingConversion,
            AllowSummonProxy = flags.AllowSummonProxy,
            AllowEquipmentOverride = flags.AllowEquipmentOverride,
            AllowTrueDamageConversion = flags.AllowTrueDamageConversion
        };

    private static AbilityCostSpec CloneCost(AbilityCostSpec cost) =>
        new()
        {
            Resource = cost.Resource,
            BaseValue = cost.BaseValue,
            ScalingAttribute = cost.ScalingAttribute,
            ScalingCoefficient = cost.ScalingCoefficient
        };

    private static AbilityTriggerSpec CloneTrigger(AbilityTriggerSpec trigger) =>
        new()
        {
            Event = trigger.Event,
            InternalCooldownTicks = trigger.InternalCooldownTicks,
            Conditions = [.. trigger.Conditions.Select(CloneCondition)],
            EffectIds = [.. trigger.EffectIds]
        };

    private static AbilityEffectSpec CloneEffect(AbilityEffectSpec effect) =>
        new()
        {
            Id = effect.Id,
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
            ProcCoefficient = effect.ProcCoefficient,
            Tags = [.. effect.Tags],
            Conditions = [.. effect.Conditions.Select(CloneCondition)]
        };

    private static AbilityConditionSpec CloneCondition(AbilityConditionSpec condition) =>
        new()
        {
            Type = condition.Type,
            Subject = condition.Subject,
            StatusId = condition.StatusId,
            Tag = condition.Tag,
            Value = condition.Value
        };
}
