using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Engine;

public sealed class CombatEngineExecutor : ICombatEngineExecutor
{
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IEssenceDefinitionRepository? _essenceDefinitions;

    public CombatEngineExecutor(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository? essenceDefinitions = null)
    {
        _catalogProvider = catalogProvider;
        _essenceDefinitions = essenceDefinitions;
    }

    public Task<CombatResult> ExecuteAsync(
        CombatEncounterRuntime runtime,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var catalog = _catalogProvider.GetCatalog();
        var equippedEssenceIds = runtime.FriendlyParticipants
            .Concat(runtime.HostileParticipants)
            .SelectMany(participant => GetEquippedEssenceIds(participant.Combatant))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var abilitySpecs = SelectAbilitySpecsForEssencesAndSummons(catalog, equippedEssenceIds).ToList();
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
        var engine = new FastCombatEngine(compiledStatuses, compiledSummons, compiledAbilities);
        var result = engine.Run(friendly, hostile);
        SyncCombatEntityState(runtime.FriendlyParticipants, friendly);
        SyncCombatEntityState(runtime.HostileParticipants, hostile);
        result.StartedAt = runtime.Plan.StartsAt;

        return Task.FromResult(result);
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

        return new RuntimeCombatant(
            combatant.Id,
            combatant.Name,
            team,
            CreateAttributeSnapshot(combatant),
            abilities,
            combatant.Tags,
            combatant.ImagePath,
            combatant.IsSummoned);
    }

    private IEnumerable<CompiledAbility> CreateCombatantAbilities(
        CombatEntity combatant,
        AbilityCatalog catalog,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var essence in combatant.EquippedEssences)
        {
            if (string.IsNullOrWhiteSpace(essence.EssenceDefinitionId)
                || !catalog.AbilityIdsByOwningEssence.TryGetValue(essence.EssenceDefinitionId, out var abilityIds))
                continue;

            foreach (var abilityId in abilityIds)
            {
                if (!selected.Add(abilityId))
                    continue;

                var baseSpec = catalog.AbilitiesById[abilityId];
                var modifiedSpec = ApplyEvolutionModifiers(baseSpec, essence, catalog);
                yield return ReferenceEquals(baseSpec, modifiedSpec)
                    ? compiledAbilities[abilityId]
                    : AbilityCompiler.CompileAbility(modifiedSpec);
            }
        }

        foreach (var essenceId in GetTaggedEssenceIds(combatant.Tags))
        {
            if (!catalog.AbilityIdsByOwningEssence.TryGetValue(essenceId, out var abilityIds))
                continue;

            foreach (var abilityId in abilityIds)
            {
                if (selected.Add(abilityId))
                    yield return compiledAbilities[abilityId];
            }
        }
    }

    private static IEnumerable<AbilitySpec> SelectAbilitySpecsForEssencesAndSummons(
        AbilityCatalog catalog,
        IEnumerable<string> essenceIds)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(SelectAbilityIdsForEssences(catalog, essenceIds));

        while (queue.TryDequeue(out var abilityId))
        {
            if (!selected.Add(abilityId))
                continue;

            var ability = catalog.AbilitiesById[abilityId];
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
            Costs = [.. spec.Costs.Select(CloneCost)],
            Triggers = [.. spec.Triggers.Select(CloneTrigger)],
            Effects = [.. spec.Effects.Select(CloneEffect)]
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

    private static IEnumerable<string> SelectSummonIds(IEnumerable<AbilitySpec> abilities) =>
        abilities
            .SelectMany(ability => ability.Effects)
            .Where(effect => effect.Operation == AbilityEffectOperation.Summon && !string.IsNullOrWhiteSpace(effect.SummonId))
            .Select(effect => effect.SummonId!);

    private static IEnumerable<string> SelectAbilityIdsForEssences(
        AbilityCatalog catalog,
        IEnumerable<string> essenceIds)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var essenceId in essenceIds)
        {
            if (!catalog.AbilityIdsByOwningEssence.TryGetValue(essenceId, out var abilityIds))
                continue;

            foreach (var abilityId in abilityIds)
            {
                if (selected.Add(abilityId))
                    yield return abilityId;
            }
        }
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

        if (attributes[AttributeType.MaxHealth] <= 0)
            attributes[AttributeType.MaxHealth] = 100;

        return attributes;
    }
}
