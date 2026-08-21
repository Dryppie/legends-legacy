using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;

namespace Services.LL.Combat.Engine;

public sealed class AbilityCatalog
{
    public AbilityCatalog(
        IReadOnlyList<AbilitySpec> abilities,
        IReadOnlyList<StatusSpec> statuses,
        IReadOnlyList<SummonSpec> summons,
        IReadOnlyDictionary<string, string> owningEssenceByAbilityId)
    {
        Abilities = abilities;
        Statuses = statuses;
        Summons = summons;
        OwningEssenceByAbilityId = owningEssenceByAbilityId;
        AbilitiesById = abilities.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        StatusesById = statuses.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        SummonsById = summons.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        AbilityIdsByKind = abilities
            .GroupBy(x => x.Kind)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Select(a => a.Id).ToList());
        AbilityIdsByTag = BuildTagIndex(abilities.Select(x => (x.Id, Tags: (IEnumerable<string>)x.Tags)));
        StatusIdsByTag = BuildTagIndex(statuses.Select(x => (x.Id, Tags: (IEnumerable<string>)x.Tags)));
        SummonIdsByTag = BuildTagIndex(summons.Select(x => (x.Id, Tags: (IEnumerable<string>)x.Tags)));
        AbilityIdsByTrigger = BuildTriggerIndex(abilities);
        AbilityIdsByOwningEssence = owningEssenceByAbilityId
            .GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<string>)x.Select(item => item.Key).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AbilitySpec> Abilities { get; }
    public IReadOnlyList<StatusSpec> Statuses { get; }
    public IReadOnlyList<SummonSpec> Summons { get; }
    public IReadOnlyDictionary<string, AbilitySpec> AbilitiesById { get; }
    public IReadOnlyDictionary<string, StatusSpec> StatusesById { get; }
    public IReadOnlyDictionary<string, SummonSpec> SummonsById { get; }
    public IReadOnlyDictionary<AbilitySpecKind, IReadOnlyList<string>> AbilityIdsByKind { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AbilityIdsByTag { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> StatusIdsByTag { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SummonIdsByTag { get; }
    public IReadOnlyDictionary<AbilityTriggerEvent, IReadOnlyList<string>> AbilityIdsByTrigger { get; }
    public IReadOnlyDictionary<string, string> OwningEssenceByAbilityId { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> AbilityIdsByOwningEssence { get; }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildTagIndex(
        IEnumerable<(string Id, IEnumerable<string> Tags)> taggedItems)
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in taggedItems)
        {
            foreach (var tag in item.Tags.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!index.TryGetValue(tag, out var ids))
                {
                    ids = [];
                    index.Add(tag, ids);
                }

                ids.Add(item.Id);
            }
        }

        return index.ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<AbilityTriggerEvent, IReadOnlyList<string>> BuildTriggerIndex(
        IEnumerable<AbilitySpec> abilities)
    {
        var index = new Dictionary<AbilityTriggerEvent, List<string>>();

        foreach (var ability in abilities)
        {
            foreach (var triggerEvent in ability.Triggers.Select(x => x.Event).Distinct())
            {
                if (!index.TryGetValue(triggerEvent, out var ids))
                {
                    ids = [];
                    index.Add(triggerEvent, ids);
                }

                ids.Add(ability.Id);
            }
        }

        return index.ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Value);
    }
}

public sealed record AbilityCatalogValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class AbilityCatalogValidator
{
    public static AbilityCatalogValidationResult Validate(
        IReadOnlyList<AbilitySpec> abilities,
        IReadOnlyList<StatusSpec> statuses,
        IReadOnlyDictionary<string, string>? owningEssenceByAbilityId = null,
        IReadOnlyList<SummonSpec>? summons = null)
    {
        var errors = new List<string>();
        var abilityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var statusIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var summonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var summonSpecs = summons ?? [];

        foreach (var ability in abilities)
        {
            var label = string.IsNullOrWhiteSpace(ability.Id) ? "<missing ability id>" : ability.Id;
            if (string.IsNullOrWhiteSpace(ability.Id))
                errors.Add("Ability id is required.");
            else if (!abilityIds.Add(ability.Id))
                errors.Add($"Duplicate ability id '{ability.Id}'.");

            if (string.IsNullOrWhiteSpace(ability.Name))
                errors.Add($"{label}: name is required.");

            if (ability.CooldownTicks < 0)
                errors.Add($"{label}: cooldown cannot be negative.");

            if (!float.IsFinite(ability.ThreatMultiplier) || ability.ThreatMultiplier < 0)
                errors.Add($"{label}: threatMultiplier must be finite and non-negative.");

            if (ability.Kind != AbilitySpecKind.Passive
                && ability.Effects.Any(effect => effect.MaintainWhileConditionsMet))
            {
                errors.Add($"{label}: maintained conditional modifiers require a Passive ability.");
            }

            ValidateCosts(label, ability.Costs, errors);
            ValidateEffects(label, ability.Effects, statusIds: null, errors);
            ValidateTriggers(label, ability.Triggers, ability.Effects, errors);
        }

        foreach (var status in statuses)
        {
            var label = string.IsNullOrWhiteSpace(status.Id) ? "<missing status id>" : status.Id;
            if (string.IsNullOrWhiteSpace(status.Id))
                errors.Add("Status id is required.");
            else if (!statusIds.Add(status.Id))
                errors.Add($"Duplicate status id '{status.Id}'.");

            if (status.MaxStacks <= 0)
                errors.Add($"{label}: max stacks must be greater than 0.");

            if (status.DurationTicks < 0)
                errors.Add($"{label}: duration cannot be negative.");

            if (!float.IsFinite(status.SourceDamageTakenPercentPerStack)
                || status.SourceDamageTakenPercentPerStack < 0)
            {
                errors.Add($"{label}: sourceDamageTakenPercentPerStack must be finite and non-negative.");
            }

            if (status.Effects.Any(effect => effect.MaintainWhileConditionsMet))
                errors.Add($"{label}: status effects cannot maintain conditional modifiers.");

            ValidateEffects(label, status.Effects, statusIds: null, errors);
            ValidateTriggers(label, status.Triggers, status.Effects, errors);
        }

        foreach (var summon in summonSpecs)
        {
            var label = string.IsNullOrWhiteSpace(summon.Id) ? "<missing summon id>" : summon.Id;
            if (string.IsNullOrWhiteSpace(summon.Id))
                errors.Add("Summon id is required.");
            else if (!summonIds.Add(summon.Id))
                errors.Add($"Duplicate summon id '{summon.Id}'.");

            if (string.IsNullOrWhiteSpace(summon.Name))
                errors.Add($"{label}: name is required.");

            if (summon.DurationTicks < 0)
                errors.Add($"{label}: duration cannot be negative.");

            if (summon.MaxActive < 0)
                errors.Add($"{label}: maxActive cannot be negative.");

            if (summon.ThreatMultiplier is { } threatMultiplier
                && (!float.IsFinite(threatMultiplier) || threatMultiplier < 0))
                errors.Add($"{label}: threatMultiplier must be finite and non-negative.");

            ValidateSummonAttributes(summon, errors);
        }

        var knownStatusIds = new HashSet<string>(statuses.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
        foreach (var ability in abilities)
            ValidateStatusReferences(ability.Id, ability.Effects, ability.Triggers.SelectMany(x => x.Conditions), knownStatusIds, errors);
        foreach (var status in statuses)
            ValidateStatusReferences(status.Id, status.Effects, status.Triggers.SelectMany(x => x.Conditions), knownStatusIds, errors);

        var knownSummonIds = new HashSet<string>(summonSpecs.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);
        foreach (var ability in abilities)
            ValidateSummonReferences(ability.Id, ability.Effects, knownSummonIds, errors);
        foreach (var status in statuses)
            ValidateSummonReferences(status.Id, status.Effects, knownSummonIds, errors);
        foreach (var summon in summonSpecs)
            ValidateSummonAbilityReferences(summon, abilityIds, errors);

        if (owningEssenceByAbilityId is not null)
        {
            foreach (var abilityId in owningEssenceByAbilityId.Keys)
            {
                if (!abilityIds.Contains(abilityId))
                    errors.Add($"Owning essence index references unknown ability '{abilityId}'.");
            }
        }

        return new AbilityCatalogValidationResult(errors);
    }

    public static AbilityCatalog CreateCatalog(
        IReadOnlyList<AbilitySpec> abilities,
        IReadOnlyList<StatusSpec> statuses,
        IReadOnlyDictionary<string, string>? owningEssenceByAbilityId = null,
        IReadOnlyList<SummonSpec>? summons = null)
    {
        var owners = owningEssenceByAbilityId ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var summonSpecs = summons ?? [];
        var validation = Validate(abilities, statuses, owners, summonSpecs);
        if (!validation.IsValid)
            throw new InvalidOperationException("Ability catalog validation failed: " + string.Join(" | ", validation.Errors));

        return new AbilityCatalog(abilities, statuses, summonSpecs, owners);
    }

    private static void ValidateEffects(
        string ownerId,
        IReadOnlyList<AbilityEffectSpec> effects,
        ISet<string>? statusIds,
        ICollection<string> errors)
    {
        var effectIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var effect in effects)
        {
            var label = string.IsNullOrWhiteSpace(effect.Id) ? $"{ownerId}/<missing effect id>" : $"{ownerId}/{effect.Id}";
            if (string.IsNullOrWhiteSpace(effect.Id))
                errors.Add($"{ownerId}: effect id is required.");
            else if (!effectIds.Add(effect.Id))
                errors.Add($"{ownerId}: duplicate effect id '{effect.Id}'.");

            if (effect.ChancePercent is < 0 or > 100)
                errors.Add($"{label}: chance must be between 0 and 100.");

            if (effect.ProcCoefficient is <= 0 or > 2)
                errors.Add($"{label}: procCoefficient must be greater than 0 and no more than 2.");

            if (effect.DurationTicks < 0 || effect.IntervalTicks < 0 || effect.Uses < 0)
                errors.Add($"{label}: duration, interval, and uses cannot be negative.");

            if (effect.MaintainWhileConditionsMet)
            {
                if (!IsMaintainableModifierOperation(effect.Operation))
                    errors.Add($"{label}: {effect.Operation} cannot be maintained while conditions are met.");
                if (effect.Target is not (AbilityTargetSelector.Self or AbilityTargetSelector.Source))
                    errors.Add($"{label}: maintained conditional modifiers must target Self or Source.");
                if (effect.Conditions.Count == 0)
                    errors.Add($"{label}: maintained conditional modifiers require at least one condition.");
                if (effect.DurationTicks != 0 || effect.IntervalTicks != 0 || effect.Uses != 0)
                    errors.Add($"{label}: maintained conditional modifiers cannot use durationTicks, intervalTicks, or uses.");
                if (effect.OncePerTarget || effect.ChancePercent != 100)
                    errors.Add($"{label}: maintained conditional modifiers cannot use oncePerTarget or chance.");
            }

            if (effect.GuaranteedConditionApplication
                && effect.Operation is not (AbilityEffectOperation.ApplyCondition
                    or AbilityEffectOperation.ApplyRandomCondition))
            {
                errors.Add($"{label}: guaranteedConditionApplication requires a condition application operation.");
            }
            if (effect.StaggerPower < 0)
                errors.Add($"{label}: staggerPower cannot be negative.");
            if (effect.StaggerPower > 0
                && (effect.Operation != AbilityEffectOperation.ApplyCondition
                    || effect.Condition is not (StandardConditionType.Stun or StandardConditionType.Freeze)))
            {
                errors.Add($"{label}: staggerPower requires an ApplyCondition Stun or Freeze effect.");
            }
            if (effect.Operation is (AbilityEffectOperation.Damage
                    or AbilityEffectOperation.Heal
                    or AbilityEffectOperation.GrantBarrier)
                && effect.BaseValue != 0)
            {
                errors.Add(
                    $"{label}: {effect.Operation} cannot use baseValue; author its magnitude with scaling attributes or event-based coefficients.");
            }

            if (effect.Operation is (AbilityEffectOperation.Damage
                    or AbilityEffectOperation.Heal
                    or AbilityEffectOperation.GrantBarrier)
                && !HasProgressionMagnitudeSource(effect))
            {
                errors.Add(
                    $"{label}: {effect.Operation} requires a positive attribute, event, condition, status, or owned-summon scaling source.");
            }

            if (effect.LivingNonSummonedAllyDamagePercent < 0)
                errors.Add($"{label}: livingNonSummonedAllyDamagePercent cannot be negative.");

            if (effect.SubsequentTargetDamagePercent is <= 0 or > 100)
                errors.Add($"{label}: subsequentTargetDamagePercent must be between 1 and 100.");

            if (effect.LifeStealTargetCondition is not null && effect.LifeStealPercentage <= 0)
                errors.Add($"{label}: lifeStealTargetCondition requires positive lifeStealPercentage.");

            if (effect.RepeatCount <= 0)
                errors.Add($"{label}: repeatCount must be greater than 0.");

            if (!float.IsFinite(effect.OwnedSummonScalingCoefficient)
                || effect.OwnedSummonScalingCoefficient < 0)
            {
                errors.Add($"{label}: ownedSummonScalingCoefficient must be finite and non-negative.");
            }

            if (effect.OwnedSummonScalingCoefficient > 0
                && string.IsNullOrWhiteSpace(effect.ScalingOwnedSummonId))
            {
                errors.Add($"{label}: ownedSummonScalingCoefficient requires scalingOwnedSummonId.");
            }

            if (!float.IsFinite(effect.HealingScalingCoefficient)
                || !float.IsFinite(effect.MaximumHealingScalingCoefficient)
                || effect.HealingScalingCoefficient < 0
                || effect.MaximumHealingScalingCoefficient < 0)
            {
                errors.Add($"{label}: healing scaling coefficients must be finite and non-negative.");
            }

            if (effect.HealingScalingCoefficient > 0 && effect.HealingScalingAttribute is null)
                errors.Add($"{label}: healingScalingCoefficient requires healingScalingAttribute.");

            if (effect.ScalingAttribute is { } scalingAttribute
                && !AttributeCatalog.IsContentFacing(scalingAttribute))
            {
                errors.Add($"{label}: scaling attribute '{scalingAttribute}' is runtime-only and cannot be authored.");
            }

            if (!AttributeCatalog.IsContentFacing(effect.StatusScalingAttribute))
            {
                errors.Add($"{label}: status scaling attribute '{effect.StatusScalingAttribute}' is runtime-only and cannot be authored.");
            }

            if (effect.Operation is AbilityEffectOperation.ModifyAttribute
                    or AbilityEffectOperation.ModifyAttributePercentOfInitial
                    or AbilityEffectOperation.TransferAttributePercent
                    or AbilityEffectOperation.SynchronizeAttributePerOwnedSummon
                    or AbilityEffectOperation.SynchronizeAttributePerStatusStack
                    or AbilityEffectOperation.SynchronizeAttributePerMissingHealthStep
                && effect.Attribute is null)
            {
                errors.Add($"{label}: {effect.Operation} requires attribute.");
            }

            if (effect.Operation is AbilityEffectOperation.ModifyAttributePercentOfInitial
                    or AbilityEffectOperation.TransferAttributePercent
                && effect.ScalingCoefficient is <= -1 or >= 1)
            {
                errors.Add($"{label}: {effect.Operation} scalingCoefficient must be greater than -1 and less than 1.");
            }

            if ((effect.Operation == AbilityEffectOperation.ApplyStatus
                 || effect.Operation == AbilityEffectOperation.ModifyStatusStacks
                 || effect.Operation == AbilityEffectOperation.RemoveStatus
                 || effect.Operation == AbilityEffectOperation.SynchronizeAttributePerStatusStack)
                && string.IsNullOrWhiteSpace(effect.StatusId))
            {
                errors.Add($"{label}: {effect.Operation} requires statusId.");
            }

            if (effect.Operation == AbilityEffectOperation.Summon && string.IsNullOrWhiteSpace(effect.SummonId))
                errors.Add($"{label}: Summon requires summonId.");

            if (effect.Operation == AbilityEffectOperation.GrantCover
                && (effect.BaseValue is <= 0 or > 100 || effect.DurationTicks <= 0))
            {
                errors.Add($"{label}: GrantCover requires baseValue between 1 and 100 and a positive durationTicks.");
            }

            if (effect.Target == AbilityTargetSelector.OwnedSummons
                && string.IsNullOrWhiteSpace(effect.SummonId))
            {
                errors.Add($"{label}: OwnedSummons requires summonId.");
            }

            if (effect.Operation == AbilityEffectOperation.SwapHealth
                && (effect.Target != AbilityTargetSelector.HighestCurrentHealthOwnedSummon
                    || string.IsNullOrWhiteSpace(effect.SummonId)))
            {
                errors.Add($"{label}: SwapHealth requires HighestCurrentHealthOwnedSummon and summonId.");
            }

            if (effect.Operation == AbilityEffectOperation.Summon
                && !string.IsNullOrWhiteSpace(effect.SummonGroupId)
                && effect.DurationTicks <= 0)
            {
                errors.Add($"{label}: grouped Summon effects require a positive durationTicks.");
            }

            if (effect.Operation == AbilityEffectOperation.SynchronizeAttributePerOwnedSummon)
            {
                if (string.IsNullOrWhiteSpace(effect.SummonId))
                    errors.Add($"{label}: SynchronizeAttributePerOwnedSummon requires summonId.");
                if (effect.BaseValue == 0)
                    errors.Add($"{label}: SynchronizeAttributePerOwnedSummon requires a non-zero baseValue.");
            }

            if (effect.Operation == AbilityEffectOperation.SynchronizeAttributePerStatusStack
                && effect.BaseValue == 0
                && Math.Abs(effect.ScalingCoefficient) <= float.Epsilon)
            {
                errors.Add($"{label}: SynchronizeAttributePerStatusStack requires a non-zero baseValue or scalingCoefficient.");
            }

            if (effect.Operation == AbilityEffectOperation.SynchronizeAttributePerMissingHealthStep)
            {
                if (effect.HealthStepPercent is <= 0 or > 100)
                {
                    errors.Add(
                        $"{label}: SynchronizeAttributePerMissingHealthStep requires healthStepPercent between 1 and 100.");
                }
                if (effect.BaseValue == 0)
                {
                    errors.Add(
                        $"{label}: SynchronizeAttributePerMissingHealthStep requires a non-zero baseValue.");
                }
            }

            if (effect.Operation == AbilityEffectOperation.ConsumeOwnedSummon)
            {
                if (string.IsNullOrWhiteSpace(effect.SummonId))
                    errors.Add($"{label}: ConsumeOwnedSummon requires summonId.");
                if (effect.BaseValue <= 0
                    && (effect.ScalingAttribute is null || effect.ScalingCoefficient <= 0))
                {
                    errors.Add($"{label}: ConsumeOwnedSummon requires positive healing.");
                }
            }

            if (effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is null)
                errors.Add($"{label}: ApplyCondition requires condition.");

            if (effect.Operation is AbilityEffectOperation.ConsumeConditionStacks
                    or AbilityEffectOperation.RemoveCondition
                && effect.Condition is null)
            {
                errors.Add($"{label}: {effect.Operation} requires condition.");
            }

            if (effect.Operation == AbilityEffectOperation.ConsumeConditionStacks)
            {
                if (effect.BaseValue <= 0)
                    errors.Add($"{label}: ConsumeConditionStacks requires a positive baseValue.");
                if (effect.ScalingAttribute is null || effect.ScalingCoefficient <= 0)
                    errors.Add($"{label}: ConsumeConditionStacks requires positive damage scaling.");
                if (effect.HealingScalingCoefficient < 0 || effect.MaximumHealingScalingCoefficient < 0)
                    errors.Add($"{label}: ConsumeConditionStacks healing coefficients cannot be negative.");
                if (effect.HealingScalingCoefficient > 0 && effect.HealingScalingAttribute is null)
                    errors.Add($"{label}: ConsumeConditionStacks healing requires healingScalingAttribute.");
                if (effect.MaximumHealingScalingCoefficient > 0
                    && effect.MaximumHealingScalingCoefficient < effect.HealingScalingCoefficient)
                {
                    errors.Add($"{label}: ConsumeConditionStacks maximum healing must be at least its per-stack healing.");
                }
            }

            if (effect.Operation == AbilityEffectOperation.ApplyRandomCondition
                && (effect.Condition is null || effect.AlternativeCondition is null))
            {
                errors.Add($"{label}: ApplyRandomCondition requires condition and alternativeCondition.");
            }

            if (effect.Operation == AbilityEffectOperation.ModifyDamageTakenFromCondition
                && effect.Condition is null)
            {
                errors.Add($"{label}: ModifyDamageTakenFromCondition requires condition.");
            }

            if (effect.Operation == AbilityEffectOperation.ApplyCondition && effect.Condition is { } condition)
            {
                if (condition == StandardConditionType.Thorns && effect.DurationTicks <= 0)
                    errors.Add($"{label}: Thorns requires a positive durationTicks.");

                if (condition == StandardConditionType.Thorns && effect.IntervalTicks > 0)
                    errors.Add($"{label}: Thorns cannot use intervalTicks because durationTicks is its condition duration.");

                if (condition != StandardConditionType.Thorns
                    && effect.DurationTicks > 0
                    && effect.IntervalTicks <= 0)
                {
                    errors.Add($"{label}: durationTicks requires intervalTicks; condition duration comes from canonical X or its fixed rule.");
                }

                if (RequiresPositiveConditionValue(condition)
                    && effect.BaseValue <= 0
                    && effect.ScalingCoefficient <= 0)
                {
                    errors.Add($"{label}: {condition} requires a positive condition value.");
                }
            }

            if (statusIds is not null
                && !string.IsNullOrWhiteSpace(effect.StatusId)
                && !statusIds.Contains(effect.StatusId))
            {
                errors.Add($"{label}: references unknown status '{effect.StatusId}'.");
            }

            ValidateConditions(label, effect.Conditions, errors);
        }
    }

    private static bool IsMaintainableModifierOperation(AbilityEffectOperation operation) =>
        operation is AbilityEffectOperation.ModifyAttribute
            or AbilityEffectOperation.ModifyRegenerationRate
            or AbilityEffectOperation.ModifyRegenerationInterval
            or AbilityEffectOperation.ModifyHealingReceived
            or AbilityEffectOperation.ModifyDamageDealt
            or AbilityEffectOperation.ModifyDamageTaken
            or AbilityEffectOperation.ModifyDamageTakenFromCondition;

    private static bool HasProgressionMagnitudeSource(AbilityEffectSpec effect) =>
        (effect.ScalingAttribute is not null && effect.ScalingCoefficient > 0)
        || effect.EventMagnitudeCoefficient > 0
        || (effect.ScalingCondition is not null && effect.ConditionScalingCoefficient > 0)
        || (!string.IsNullOrWhiteSpace(effect.ScalingStatusId) && effect.StatusScalingCoefficient > 0)
        || (!string.IsNullOrWhiteSpace(effect.ScalingOwnedSummonId) && effect.OwnedSummonScalingCoefficient > 0);

    private static void ValidateCosts(
        string ownerId,
        IEnumerable<AbilityCostSpec> costs,
        ICollection<string> errors)
    {
        foreach (var cost in costs)
        {
            if (cost.BaseValue < 0)
                errors.Add($"{ownerId}: cost {cost.Resource} base value cannot be negative.");

            if (cost.ScalingCoefficient < 0)
                errors.Add($"{ownerId}: cost {cost.Resource} scaling coefficient cannot be negative.");
        }
    }

    private static void ValidateTriggers(
        string ownerId,
        IReadOnlyList<AbilityTriggerSpec> triggers,
        IReadOnlyList<AbilityEffectSpec> effects,
        ICollection<string> errors)
    {
        var effectIds = effects.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var trigger in triggers)
        {
            if (trigger.InternalCooldownTicks < 0)
                errors.Add($"{ownerId}: trigger {trigger.Event} internal cooldown cannot be negative.");
            if (trigger.InitialDelayTicks < 0)
                errors.Add($"{ownerId}: trigger {trigger.Event} initial delay cannot be negative.");
            if (trigger.EveryNthOccurrence <= 0)
                errors.Add($"{ownerId}: trigger {trigger.Event} everyNthOccurrence must be positive.");

            foreach (var effectId in trigger.EffectIds)
            {
                if (!effectIds.Contains(effectId))
                    errors.Add($"{ownerId}: trigger {trigger.Event} references unknown effect '{effectId}'.");
            }

            ValidateConditions($"{ownerId}/{trigger.Event}", trigger.Conditions, errors);
        }
    }

    private static void ValidateConditions(
        string ownerId,
        IEnumerable<AbilityConditionSpec> conditions,
        ICollection<string> errors)
    {
        foreach (var condition in conditions)
        {
            if ((condition.Type == AbilityConditionType.HasStatus
                 || condition.Type == AbilityConditionType.StatusStacksAtLeast)
                && string.IsNullOrWhiteSpace(condition.StatusId))
            {
                errors.Add($"{ownerId}: condition {condition.Type} requires statusId.");
            }

            if (condition.Type == AbilityConditionType.HasTag && string.IsNullOrWhiteSpace(condition.Tag))
                errors.Add($"{ownerId}: condition HasTag requires tag.");

            if ((condition.Type == AbilityConditionType.HasCondition
                 || condition.Type == AbilityConditionType.ConditionStacksAtLeast
                 || condition.Type == AbilityConditionType.AnyEnemyHasCondition
                 || condition.Type == AbilityConditionType.NoEnemyHasCondition)
                && condition.Condition is null)
            {
                errors.Add($"{ownerId}: condition {condition.Type} requires condition.");
            }

            if (condition.Type == AbilityConditionType.ConditionStacksAtLeast && condition.Value <= 0)
                errors.Add($"{ownerId}: condition ConditionStacksAtLeast requires a positive value.");

            if (condition.Type == AbilityConditionType.ChancePercent && condition.Value is < 0 or > 100)
                errors.Add($"{ownerId}: condition ChancePercent requires value between 0 and 100.");

            if (condition.Type is AbilityConditionType.AnyEnemyHealthBelowPercent
                    or AbilityConditionType.NoEnemyHealthBelowPercent
                && condition.Value is < 0 or > 100)
            {
                errors.Add($"{ownerId}: condition {condition.Type} requires value between 0 and 100.");
            }

            if (condition.Type == AbilityConditionType.EventIdIs
                && string.IsNullOrWhiteSpace(condition.StatusId))
            {
                errors.Add($"{ownerId}: condition EventIdIs requires statusId as its event id.");
            }
        }
    }

    private static void ValidateStatusReferences(
        string ownerId,
        IEnumerable<AbilityEffectSpec> effects,
        IEnumerable<AbilityConditionSpec> triggerConditions,
        ISet<string> knownStatusIds,
        ICollection<string> errors)
    {
        foreach (var effect in effects)
        {
            if (!string.IsNullOrWhiteSpace(effect.StatusId) && !knownStatusIds.Contains(effect.StatusId))
                errors.Add($"{ownerId}/{effect.Id}: references unknown status '{effect.StatusId}'.");

            foreach (var condition in effect.Conditions)
                ValidateStatusConditionReference($"{ownerId}/{effect.Id}", condition, knownStatusIds, errors);
        }

        foreach (var condition in triggerConditions)
            ValidateStatusConditionReference(ownerId, condition, knownStatusIds, errors);
    }

    private static void ValidateStatusConditionReference(
        string ownerId,
        AbilityConditionSpec condition,
        ISet<string> knownStatusIds,
        ICollection<string> errors)
    {
        if ((condition.Type == AbilityConditionType.HasStatus
             || condition.Type == AbilityConditionType.StatusStacksAtLeast)
            && !string.IsNullOrWhiteSpace(condition.StatusId)
            && !knownStatusIds.Contains(condition.StatusId))
        {
            errors.Add($"{ownerId}: condition references unknown status '{condition.StatusId}'.");
        }
    }

    private static void ValidateSummonReferences(
        string ownerId,
        IEnumerable<AbilityEffectSpec> effects,
        ISet<string> knownSummonIds,
        ICollection<string> errors)
    {
        foreach (var effect in effects)
        {
            var referencedSummonIds = new List<string>();
            if (effect.Operation is AbilityEffectOperation.Summon
                    or AbilityEffectOperation.SynchronizeAttributePerOwnedSummon
                    or AbilityEffectOperation.ConsumeOwnedSummon
                    or AbilityEffectOperation.SwapHealth
                && !string.IsNullOrWhiteSpace(effect.SummonId))
            {
                referencedSummonIds.Add(effect.SummonId);
            }

            if (!string.IsNullOrWhiteSpace(effect.RepeatPerOwnedSummonId))
                referencedSummonIds.Add(effect.RepeatPerOwnedSummonId);
            if (!string.IsNullOrWhiteSpace(effect.ScalingOwnedSummonId))
                referencedSummonIds.Add(effect.ScalingOwnedSummonId);

            foreach (var summonId in referencedSummonIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!knownSummonIds.Contains(summonId))
                    errors.Add($"{ownerId}/{effect.Id}: references unknown summon '{summonId}'.");
            }
        }
    }

    private static void ValidateSummonAbilityReferences(
        SummonSpec summon,
        ISet<string> knownAbilityIds,
        ICollection<string> errors)
    {
        foreach (var abilityId in summon.AbilityIds)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                errors.Add($"{summon.Id}: summon ability id is required.");
                continue;
            }

            if (!knownAbilityIds.Contains(abilityId))
                errors.Add($"{summon.Id}: references unknown ability '{abilityId}'.");
        }
    }

    private static void ValidateSummonAttributes(
        SummonSpec summon,
        ICollection<string> errors)
    {
        var summonId = summon.Id;
        var attributeTypes = new HashSet<AttributeType>();

        foreach (var attribute in summon.Attributes)
        {
            if (!attributeTypes.Add(attribute.Attribute))
                errors.Add($"{summonId}: duplicate summon attribute '{attribute.Attribute}'.");

            if (!float.IsFinite(attribute.ScalingCoefficient) || attribute.ScalingCoefficient < 0)
                errors.Add($"{summonId}/{attribute.Attribute}: scaling coefficient must be finite and non-negative.");

            if (attribute.ScalingCoefficient > 0 && attribute.ScalingAttribute is null)
                errors.Add($"{summonId}/{attribute.Attribute}: positive scaling requires scalingAttribute.");

            if (attribute.ScalingAttribute is { } scalingAttribute
                && !AttributeCatalog.IsContentFacing(scalingAttribute))
            {
                errors.Add(
                    $"{summonId}/{attribute.Attribute}: scaling attribute '{scalingAttribute}' is runtime-only and cannot be authored.");
            }
        }

        if (!attributeTypes.Contains(AttributeType.MaxHealth))
            errors.Add($"{summonId}: summon attributes must include MaxHealth.");

        var health = summon.Attributes.FirstOrDefault(x => x.Attribute == AttributeType.MaxHealth);
        if (health is not null
            && (health.ScalingAttribute != AttributeType.MaxHealth || health.ScalingCoefficient <= 0))
        {
            errors.Add(
                $"{summonId}/MaxHealth: summon durability must scale positively from owner MaxHealth.");
        }

        if (summon.CanBasicAttack)
        {
            var power = summon.Attributes.FirstOrDefault(x => x.Attribute == AttributeType.Power);
            if (power is null
                || power.ScalingAttribute != AttributeType.Power
                || power.ScalingCoefficient <= 0)
            {
                errors.Add(
                    $"{summonId}/Power: basic-attacking summons must scale positively from owner Power.");
            }
        }
    }

    private static bool RequiresPositiveConditionValue(StandardConditionType condition) =>
        condition is not StandardConditionType.Empower
            and not StandardConditionType.Weaken
            and not StandardConditionType.Haste
            and not StandardConditionType.Slow;
}
