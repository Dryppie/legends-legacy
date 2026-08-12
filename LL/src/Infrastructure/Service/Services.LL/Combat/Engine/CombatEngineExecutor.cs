using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Domain.Models.Damages;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Engine;

public sealed class CombatEngineExecutor : ICombatEngineExecutor
{
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IEssenceDefinitionRepository? _essenceDefinitions;
    private readonly ICraftingDefinitionProvider? _craftingDefinitions;

    public CombatEngineExecutor(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository? essenceDefinitions = null,
        ICraftingDefinitionProvider? craftingDefinitions = null)
    {
        _catalogProvider = catalogProvider;
        _essenceDefinitions = essenceDefinitions;
        _craftingDefinitions = craftingDefinitions;
    }

    public async Task<CombatResult> ExecuteAsync(
        CombatEncounterRuntime runtime,
        CancellationToken cancellationToken)
    {
        var execution = await ExecuteCoreAsync(
            runtime,
            new CombatSimulationOptions(
                runtime.Plan.EncounterId.GetHashCode(),
                6000,
                StartActiveAbilitiesOnCooldown: true),
            cancellationToken);
        SyncCombatEntityState(runtime.FriendlyParticipants, execution.Friendly);
        SyncCombatEntityState(runtime.HostileParticipants, execution.Hostile);
        execution.Result.StartedAt = runtime.Plan.StartsAt;
        return execution.Result;
    }

    public async Task<CombatExecutionWithCheckpoints> ExecuteWithCheckpointsAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CancellationToken cancellationToken)
    {
        var checkpoints = new List<CombatCheckpoint>();
        var execution = await ExecuteCoreAsync(
            runtime,
            new CombatSimulationOptions(
                runtime.Plan.EncounterId.GetHashCode(),
                6000,
                StartActiveAbilitiesOnCooldown: true),
            cancellationToken,
            checkpoint => checkpoints.Add(checkpoint),
            checkpointIntervalTicks);
        SyncCombatEntityState(runtime.FriendlyParticipants, execution.Friendly);
        SyncCombatEntityState(runtime.HostileParticipants, execution.Hostile);
        execution.Result.StartedAt = runtime.Plan.StartsAt;
        return new CombatExecutionWithCheckpoints(execution.Result, checkpoints);
    }

    public async Task<CombatResult> ExecuteSimulationAsync(
        CombatEncounterRuntime runtime,
        CombatSimulationOptions options,
        CancellationToken cancellationToken)
    {
        var execution = await ExecuteCoreAsync(runtime, options, cancellationToken);
        PopulatePostCombatTeams(execution.Result, execution.Friendly, execution.Hostile);
        execution.Result.StartedAt = runtime.Plan.StartsAt;
        return execution.Result;
    }

    private static void PopulatePostCombatTeams(
        CombatResult result,
        IReadOnlyList<RuntimeCombatant> friendly,
        IReadOnlyList<RuntimeCombatant> hostile)
    {
        result.PlayerTeam = friendly.Select(CreateSimpleCombatEntity).ToList();
        result.EnemyTeam = hostile.Select(CreateSimpleCombatEntity).ToList();
    }

    private static SimpleCombatEntity CreateSimpleCombatEntity(RuntimeCombatant combatant) => new()
    {
        Id = combatant.Id,
        Name = combatant.Name,
        ImagePath = combatant.ImagePath,
        MaxHealth = (int)combatant.GetAttribute(AttributeType.MaxHealth),
        Health = (int)combatant.Health,
        Barrier = (int)combatant.Barrier
    };

    private Task<ExecutionResult> ExecuteCoreAsync(
        CombatEncounterRuntime runtime,
        CombatSimulationOptions options,
        CancellationToken cancellationToken,
        Action<CombatCheckpoint>? checkpointObserver = null,
        int checkpointIntervalTicks = 0)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var catalog = _catalogProvider.GetCatalog();
        var supplementalAbilities = (options.SupplementalAbilities ?? [])
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var abilityIds = runtime.FriendlyParticipants
            .Concat(runtime.HostileParticipants)
            .SelectMany(participant => GetCombatantAbilityIds(participant.Combatant, catalog))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var supplementalId in runtime.AllCombatants
                     .SelectMany(x => x.NativeAbilityIds)
                     .Where(supplementalAbilities.ContainsKey))
            abilityIds.Add(supplementalId);

        var abilitySpecs = SelectAbilitySpecsAndSummons(catalog, supplementalAbilities, abilityIds).ToList();
        var summonIds = SelectSummonIds(abilitySpecs).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var compiledAbilities = AbilityCompiler.CompileAbilities(abilitySpecs);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var compiledSummons = AbilityCompiler.CompileSummons(
            summonIds.Select(summonId => catalog.SummonsById[summonId]));
        var friendly = runtime.FriendlyParticipants
            .Select(participant => CreateRuntimeCombatant(participant.Combatant, CombatTeam.Friendly, catalog, compiledAbilities))
            .ToList();
        var hostile = runtime.HostileParticipants
            .Select(participant => CreateRuntimeCombatant(participant.Combatant, CombatTeam.Hostile, catalog, compiledAbilities))
            .ToList();
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(
                options.MaxTicks,
                BasicAttackIntervalTicks: options.BasicAttackIntervalTicks,
                RandomSeed: options.RandomSeed,
                StartActiveAbilitiesOnCooldown: options.StartActiveAbilitiesOnCooldown));
        var result = engine.Run(
            friendly,
            hostile,
            cancellationToken,
            checkpointObserver,
            checkpointIntervalTicks);
        return Task.FromResult(new ExecutionResult(result, friendly, hostile));
    }

    private static void SyncCombatEntityState(
        IReadOnlyList<CombatRuntimeParticipant> participants,
        IReadOnlyList<RuntimeCombatant> runtimeCombatants)
    {
        var combatantsById = runtimeCombatants.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var participant in participants)
        {
            if (!combatantsById.TryGetValue(participant.Combatant.Id, out var runtimeCombatant))
                continue;

            participant.Combatant.SetCurrentHealth(runtimeCombatant.Health);
            participant.Combatant.SetCurrentBarrier(runtimeCombatant.Barrier);
        }
    }

    private RuntimeCombatant CreateRuntimeCombatant(
        CombatEntity combatant,
        CombatTeam team,
        AbilityCatalog catalog,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities)
    {
        var abilities = CreateCombatantAbilities(combatant, catalog, compiledAbilities).ToList();
        var behavior = ResolveBasicAttackBehavior(combatant);

        return new RuntimeCombatant(
            combatant.Id,
            combatant.Name,
            team,
            CreateAttributeSnapshot(combatant),
            abilities,
            combatant.Tags,
            combatant.ImagePath,
            combatant.IsSummoned,
            basicAttackIntervalMultiplier: behavior.IntervalMultiplier,
            basicAttackDamageMultiplier: behavior.DamageMultiplier,
            basicAttackType: behavior.AttackType,
            basicAttackDamageType: behavior.DamageType);
    }

    private BasicAttackBehavior ResolveBasicAttackBehavior(CombatEntity combatant)
    {
        if (_craftingDefinitions is null || combatant.MainHandEquipment is null)
            return BasicAttackBehavior.Default;
        if (string.IsNullOrWhiteSpace(combatant.MainHandEquipment.BaseRecipeId))
            return BasicAttackBehavior.Default;

        var recipe = _craftingDefinitions.GetRecipe(combatant.MainHandEquipment.BaseRecipeId);
        var blueprint = string.IsNullOrWhiteSpace(combatant.MainHandEquipment.BlueprintId)
            ? null
            : _craftingDefinitions.GetBlueprint(combatant.MainHandEquipment.BlueprintId);
        if (recipe is null ||
            (!string.IsNullOrWhiteSpace(combatant.MainHandEquipment.BlueprintId) && blueprint is null))
            return BasicAttackBehavior.Default;
        var behavior = EquipmentCraftingDesignComposer.Compose(recipe, blueprint).Behavior;

        var attackType = behavior.RangeCategory.Equals("Ranged", StringComparison.OrdinalIgnoreCase)
            ? AttackType.Ranged
            : AttackType.Melee;
        var damageType = behavior.AttackCategory.Equals("Magical", StringComparison.OrdinalIgnoreCase)
            ? DamageType.Magical
            : DamageType.Physical;

        return new BasicAttackBehavior(
            behavior.BasicAttackIntervalMultiplier,
            behavior.BasicAttackDamageMultiplier,
            attackType,
            damageType);
    }

    private IEnumerable<CompiledAbility> CreateCombatantAbilities(
        CombatEntity combatant,
        AbilityCatalog catalog,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var essence in combatant.EquippedEssences)
        {
            if (string.IsNullOrWhiteSpace(essence.EssenceDefinitionId))
                continue;

            foreach (var abilityId in GetAbilityIdsForEssence(essence.EssenceDefinitionId, catalog))
            {
                if (!selected.Add(abilityId))
                    continue;

                var baseSpec = catalog.AbilitiesById[abilityId];
                var modifiedSpec = ApplyEvolutionModifiers(baseSpec, essence, catalog);
                modifiedSpec = EssenceAbilityProgressionScaler.Apply(modifiedSpec, essence.AscensionTier);
                modifiedSpec = ApplyTemporaryAbilityModifiers(modifiedSpec, combatant, catalog);
                yield return ReferenceEquals(baseSpec, modifiedSpec)
                    ? compiledAbilities[abilityId]
                    : AbilityCompiler.CompileAbility(modifiedSpec);
            }
        }

        foreach (var abilityId in combatant.NativeAbilityIds)
        {
            if (!selected.Add(abilityId))
                continue;

            if (!catalog.AbilitiesById.TryGetValue(abilityId, out var baseSpec))
            {
                if (compiledAbilities.TryGetValue(abilityId, out var supplemental))
                    yield return supplemental;
                continue;
            }

            var modifiedSpec = ApplyTemporaryAbilityModifiers(baseSpec, combatant, catalog);
            yield return ReferenceEquals(baseSpec, modifiedSpec)
                ? compiledAbilities[abilityId]
                : AbilityCompiler.CompileAbility(modifiedSpec);
        }

        foreach (var essenceId in GetTaggedEssenceIds(combatant.Tags))
        {
            foreach (var abilityId in GetAbilityIdsForEssence(essenceId, catalog))
            {
                if (!selected.Add(abilityId))
                    continue;

                var baseSpec = catalog.AbilitiesById[abilityId];
                var modifiedSpec = ApplyTemporaryAbilityModifiers(baseSpec, combatant, catalog);
                yield return ReferenceEquals(baseSpec, modifiedSpec)
                    ? compiledAbilities[abilityId]
                    : AbilityCompiler.CompileAbility(modifiedSpec);
            }
        }
    }

    private static IEnumerable<AbilitySpec> SelectAbilitySpecsAndSummons(
        AbilityCatalog catalog,
        IReadOnlyDictionary<string, AbilitySpec> supplementalAbilities,
        IEnumerable<string> initialAbilityIds)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(initialAbilityIds);

        while (queue.TryDequeue(out var abilityId))
        {
            if (!selected.Add(abilityId))
                continue;

            if (!catalog.AbilitiesById.TryGetValue(abilityId, out var ability)
                && !supplementalAbilities.TryGetValue(abilityId, out ability))
                continue;
            yield return ability;

            foreach (var summonId in SelectSummonIds([ability]))
            {
                if (!catalog.SummonsById.TryGetValue(summonId, out var summon))
                    continue;

                foreach (var summonAbilityId in summon.AbilityIds)
                    queue.Enqueue(summonAbilityId);
            }
        }
    }

    private AbilitySpec ApplyEvolutionModifiers(
        AbilitySpec spec,
        PlayerEssence essence,
        AbilityCatalog catalog)
    {
        if (!essence.IsEvolved || _essenceDefinitions is null)
            return spec;

        var definition = _essenceDefinitions.GetById(essence.EssenceDefinitionId);
        if (definition is null)
            return spec;

        var modifiers = SelectEvolutionModifiers(spec, definition);
        if (modifiers.Count == 0)
            return spec;

        AbilitySpec? clone = null;
        foreach (var modifier in modifiers)
        {
            clone ??= CloneAbilitySpec(spec);
            if (modifier.Operation.Equals("AddMultiplier", StringComparison.OrdinalIgnoreCase))
                ApplyMultiplierModifier(clone, modifier, catalog);
            else if (modifier.Operation.Equals("AddEffect", StringComparison.OrdinalIgnoreCase))
                ApplyAddEffectModifier(clone, modifier, catalog);
        }

        return clone ?? spec;
    }

    private AbilitySpec ApplyTemporaryAbilityModifiers(
        AbilitySpec spec,
        CombatEntity combatant,
        AbilityCatalog catalog)
    {
        if (combatant.TemporaryAbilityModifiers.Count == 0)
            return spec;

        AbilitySpec? clone = null;
        foreach (var modifier in combatant.TemporaryAbilityModifiers)
        {
            if (!CanApplyModifier(spec, modifier))
                continue;

            clone ??= CloneAbilitySpec(spec);
            if (modifier.Operation.Equals("AddMultiplier", StringComparison.OrdinalIgnoreCase))
                ApplyMultiplierModifier(clone, modifier, catalog);
            else if (modifier.Operation.Equals("AddEffect", StringComparison.OrdinalIgnoreCase))
                ApplyAddEffectModifier(clone, modifier, catalog);
        }

        return clone ?? spec;
    }

    private static bool CanApplyModifier(AbilitySpec spec, EssenceAbilityModifierDefinition modifier)
    {
        if (string.IsNullOrWhiteSpace(modifier.Target))
            return false;

        if (spec.Effects.Any(x => x.Id.Equals(modifier.Target, StringComparison.OrdinalIgnoreCase)))
            return true;

        return modifier.Effect is not null
            && spec.Triggers.Any(x => x.EffectIds.Contains(modifier.Target, StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<EssenceAbilityModifierDefinition> SelectEvolutionModifiers(
        AbilitySpec spec,
        EssenceDefinition definition)
    {
        if (spec.Id.Equals(definition.ActiveAbilityId, StringComparison.OrdinalIgnoreCase))
            return definition.Evolution.ActiveAbilityModifiers;

        if (spec.Id.Equals(definition.PassiveAbilityId, StringComparison.OrdinalIgnoreCase))
            return definition.Evolution.PassiveAbilityModifiers;

        return [];
    }

    private static void ApplyMultiplierModifier(
        AbilitySpec spec,
        EssenceAbilityModifierDefinition modifier,
        AbilityCatalog catalog)
    {
        var effect = spec.Effects.FirstOrDefault(x => x.Id.Equals(modifier.Target, StringComparison.OrdinalIgnoreCase));
        if (effect is null)
            return;

        if (TryCreateCondition(modifier.Condition, catalog, out var condition))
        {
            var bonusEffect = CloneEffect(effect);
            bonusEffect.Id = $"{effect.Id}.evolved_bonus";
            bonusEffect.BaseValue = ScaleValue(effect.BaseValue, modifier.Value);
            bonusEffect.ScalingCoefficient = effect.ScalingCoefficient * (float)modifier.Value;
            bonusEffect.Conditions.Add(condition);
            spec.Effects.Add(bonusEffect);

            foreach (var trigger in spec.Triggers.Where(x => x.EffectIds.Contains(effect.Id, StringComparer.OrdinalIgnoreCase)))
                InsertAfter(trigger.EffectIds, effect.Id, bonusEffect.Id);

            return;
        }

        effect.BaseValue = ScaleValue(effect.BaseValue, 1 + modifier.Value);
        effect.ScalingCoefficient *= (float)(1 + modifier.Value);
    }

    private static void ApplyAddEffectModifier(
        AbilitySpec spec,
        EssenceAbilityModifierDefinition modifier,
        AbilityCatalog catalog)
    {
        if (modifier.Effect is null
            || spec.Effects.Any(x => x.Id.Equals(modifier.Effect.Id, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var requiresStatus = modifier.Effect.Operation is AbilityEffectOperation.ApplyStatus
            or AbilityEffectOperation.ModifyStatusStacks
            or AbilityEffectOperation.RemoveStatus;
        if (requiresStatus
            && !string.IsNullOrWhiteSpace(modifier.Effect.StatusId)
            && !catalog.StatusesById.ContainsKey(modifier.Effect.StatusId))
        {
            throw new InvalidOperationException(
                $"Evolution modifier effect '{modifier.Effect.Id}' references unknown status '{modifier.Effect.StatusId}'.");
        }

        var addedEffect = CloneEffect(modifier.Effect);
        if (TryCreateCondition(modifier.Condition, catalog, out var condition))
            addedEffect.Conditions.Add(condition);

        spec.Effects.Add(addedEffect);

        foreach (var trigger in spec.Triggers.Where(x => x.EffectIds.Contains(modifier.Target, StringComparer.OrdinalIgnoreCase)))
            InsertAfter(trigger.EffectIds, modifier.Target, addedEffect.Id);
    }

    private static bool TryCreateCondition(
        string? condition,
        AbilityCatalog catalog,
        out AbilityConditionSpec conditionSpec)
    {
        conditionSpec = new AbilityConditionSpec();
        if (string.IsNullOrWhiteSpace(condition))
            return false;

        const string targetHasStatusPrefix = "TargetHasStatus.";
        if (!condition.StartsWith(targetHasStatusPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var statusId = NormalizeStatusId(condition[targetHasStatusPrefix.Length..]);
        if (!catalog.StatusesById.ContainsKey(statusId))
            return false;

        conditionSpec = new AbilityConditionSpec
        {
            Type = AbilityConditionType.HasStatus,
            Subject = AbilityConditionSubject.Target,
            StatusId = statusId
        };
        return true;
    }

    private static string NormalizeStatusId(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("status.", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"status.{ToSnakeCase(trimmed)}";
    }

    private static string ToSnakeCase(string value)
    {
        var result = new List<char>();
        for (var index = 0; index < value.Length; index++)
        {
            var c = value[index];
            if (char.IsWhiteSpace(c) || c is '-' or '.')
            {
                AddUnderscore(result);
                continue;
            }

            if (char.IsUpper(c) && index > 0 && result.Count > 0 && result[^1] != '_')
                result.Add('_');

            result.Add(char.ToLowerInvariant(c));
        }

        return new string(result.ToArray()).Trim('_');
    }

    private static void AddUnderscore(List<char> result)
    {
        if (result.Count > 0 && result[^1] != '_')
            result.Add('_');
    }

    private static void InsertAfter(List<string> effectIds, string existingEffectId, string newEffectId)
    {
        var index = effectIds.FindIndex(x => x.Equals(existingEffectId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            effectIds.Add(newEffectId);
            return;
        }

        effectIds.Insert(index + 1, newEffectId);
    }

    private static int ScaleValue(int value, double multiplier) =>
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
            InitialDelayTicks = trigger.InitialDelayTicks,
            EveryNthOccurrence = trigger.EveryNthOccurrence,
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
            MaximumScalingCoefficient = effect.MaximumScalingCoefficient,
            EventMagnitudeCoefficient = effect.EventMagnitudeCoefficient,
            ScalingCondition = effect.ScalingCondition,
            ConditionScalingCoefficient = effect.ConditionScalingCoefficient,
            ScalingStatusId = effect.ScalingStatusId,
            StatusScalingCoefficient = effect.StatusScalingCoefficient,
            Attribute = effect.Attribute,
            StatusId = effect.StatusId,
            Condition = effect.Condition,
            AlternativeCondition = effect.AlternativeCondition,
            SummonId = effect.SummonId,
            SummonGroupId = effect.SummonGroupId,
            LinkedEffectId = effect.LinkedEffectId,
            SummonPowerMultiplier = effect.SummonPowerMultiplier,
            SummonHealthMultiplier = effect.SummonHealthMultiplier,
            Resource = effect.Resource,
            DurationTicks = effect.DurationTicks,
            IntervalTicks = effect.IntervalTicks,
            Uses = effect.Uses,
            ChancePercent = effect.ChancePercent,
            AttackType = effect.AttackType,
            DamageType = effect.DamageType,
            CritEligibility = effect.CritEligibility,
            CritChanceBonus = effect.CritChanceBonus,
            ArmorPenetrationBonus = effect.ArmorPenetrationBonus,
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
            Condition = condition.Condition,
            DamageType = condition.DamageType,
            AttackType = condition.AttackType,
            Tag = condition.Tag,
            Value = condition.Value
        };

    private static IEnumerable<string> SelectSummonIds(IEnumerable<AbilitySpec> abilities) =>
        abilities
            .SelectMany(ability => ability.Effects)
            .Where(effect => effect.Operation == AbilityEffectOperation.Summon && !string.IsNullOrWhiteSpace(effect.SummonId))
            .Select(effect => effect.SummonId!);

    private IEnumerable<string> GetCombatantAbilityIds(CombatEntity combatant, AbilityCatalog catalog)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var abilityId in combatant.NativeAbilityIds)
        {
            if (selected.Add(abilityId))
                yield return abilityId;
        }

        foreach (var essenceId in GetEquippedEssenceIds(combatant))
        {
            foreach (var abilityId in GetAbilityIdsForEssence(essenceId, catalog))
            {
                if (selected.Add(abilityId))
                    yield return abilityId;
            }
        }
    }

    private IEnumerable<string> GetAbilityIdsForEssence(string essenceId, AbilityCatalog catalog)
    {
        if (_essenceDefinitions?.GetById(essenceId) is { } definition)
        {
            if (!string.IsNullOrWhiteSpace(definition.ActiveAbilityId))
                yield return definition.ActiveAbilityId;
            if (!string.IsNullOrWhiteSpace(definition.PassiveAbilityId))
                yield return definition.PassiveAbilityId;
            yield break;
        }

        if (!catalog.AbilityIdsByOwningEssence.TryGetValue(essenceId, out var abilityIds))
            yield break;

        foreach (var abilityId in abilityIds)
            yield return abilityId;
    }

    private static IEnumerable<string> GetEquippedEssenceIds(CombatEntity combatant)
    {
        foreach (var essence in combatant.EquippedEssences)
        {
            if (!string.IsNullOrWhiteSpace(essence.EssenceDefinitionId))
                yield return essence.EssenceDefinitionId;
        }

        foreach (var essenceId in GetTaggedEssenceIds(combatant.Tags))
            yield return essenceId;
    }

    private static IEnumerable<string> GetTaggedEssenceIds(IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            const string prefix = "Essence.";
            if (tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                yield return tag[prefix.Length..];
        }
    }

    private static Dictionary<AttributeType, float> CreateAttributeSnapshot(CombatEntity combatant)
    {
        var attributes = new Dictionary<AttributeType, float>(combatant.CombatAttributes);

        foreach (var (attribute, value) in combatant.BaseCombatAttributes)
            attributes.TryAdd(attribute, value);

        attributes.TryAdd(AttributeType.MaxHealth, Math.Max(1, combatant.GetAttributeValue(AttributeType.MaxHealth)));
        attributes.TryAdd(AttributeType.Power, combatant.GetAttributeValue(AttributeType.Power));
        attributes.TryAdd(AttributeType.CritDamage, combatant.GetAttributeValue(AttributeType.CritDamage));
        attributes.TryAdd(AttributeType.AttackSpeed, 0);

        if (attributes[AttributeType.MaxHealth] <= 0)
            attributes[AttributeType.MaxHealth] = 100;

        return attributes;
    }

    private sealed record ExecutionResult(
        CombatResult Result,
        IReadOnlyList<RuntimeCombatant> Friendly,
        IReadOnlyList<RuntimeCombatant> Hostile);

    private sealed record BasicAttackBehavior(
        double IntervalMultiplier,
        double DamageMultiplier,
        AttackType AttackType,
        DamageType DamageType)
    {
        public static BasicAttackBehavior Default { get; } =
            new(1d, 1d, AttackType.Melee, DamageType.Physical);
    }
}
