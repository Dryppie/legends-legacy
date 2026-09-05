using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Domain.Models.Damages;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Sets;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Common.Randomness;
using Microsoft.Extensions.Options;

namespace Services.LL.Combat.Engine;

public sealed class CombatEngineExecutor : ICombatEngineExecutor
{
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IEssenceDefinitionRepository? _essenceDefinitions;
    private readonly EquipmentCatalog? _equipmentCatalog;
    private readonly ThreatAndTankingOptions _threatAndTankingOptions;
    private readonly AbilityThreatTuning _abilityThreatTuning;
    private readonly Dictionary<EssenceAbilityCacheKey, CompiledAbility> _compiledEssenceAbilities = [];

    public CombatEngineExecutor(
        IAbilityCatalogProvider catalogProvider,
        IEssenceDefinitionRepository? essenceDefinitions = null,
        EquipmentCatalog? equipmentCatalog = null,
        IOptions<ThreatAndTankingOptions>? threatAndTankingOptions = null)
    {
        _catalogProvider = catalogProvider;
        _essenceDefinitions = essenceDefinitions;
        _equipmentCatalog = equipmentCatalog;
        _threatAndTankingOptions = threatAndTankingOptions?.Value ?? new ThreatAndTankingOptions();
        _abilityThreatTuning = _threatAndTankingOptions.ToAbilityThreatTuning();
    }

    public Task<CombatResult> ExecuteAsync(
        CombatEncounterRuntime runtime,
        CancellationToken cancellationToken) =>
        ExecuteAsync(runtime, captureEventLog: true, cancellationToken);

    public async Task<CombatResult> ExecuteAsync(
        CombatEncounterRuntime runtime,
        bool captureEventLog,
        CancellationToken cancellationToken)
    {
        var execution = await ExecuteCoreAsync(
            runtime,
            new CombatRuleset(
                ResolveRandomSeed(runtime),
                6000,
                StartActiveAbilitiesOnCooldown: true),
            cancellationToken,
            captureEventLog: captureEventLog);
        SyncCombatEntityState(runtime.FriendlyParticipants, execution.Friendly);
        SyncCombatEntityState(runtime.AllHostileParticipants, execution.Hostile);
        PopulatePostCombatTeams(execution.Result, execution.Friendly, execution.Hostile);
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
            new CombatRuleset(
                ResolveRandomSeed(runtime),
                6000,
                StartActiveAbilitiesOnCooldown: true),
            cancellationToken,
            checkpoint => checkpoints.Add(checkpoint),
            checkpointIntervalTicks);
        SyncCombatEntityState(runtime.FriendlyParticipants, execution.Friendly);
        SyncCombatEntityState(runtime.AllHostileParticipants, execution.Hostile);
        PopulatePostCombatTeams(execution.Result, execution.Friendly, execution.Hostile);
        execution.Result.StartedAt = runtime.Plan.StartsAt;
        return new CombatExecutionWithCheckpoints(execution.Result, checkpoints);
    }

    public async Task<CombatExecutionWithCheckpoints> ExecuteTowerPlaybackAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CancellationToken cancellationToken)
        => await ExecuteCompactPlaybackAsync(runtime, checkpointIntervalTicks, cancellationToken);

    public async Task<CombatExecutionWithCheckpoints> ExecuteCompactPlaybackAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CancellationToken cancellationToken)
        => await ExecuteCompactPlaybackCoreAsync(
            runtime,
            checkpointIntervalTicks,
            new CombatRuleset(
                ResolveRandomSeed(runtime),
                6000,
                StartActiveAbilitiesOnCooldown: true,
                CaptureEventLog: false),
            cancellationToken);

    public async Task<CombatExecutionWithCheckpoints> ExecuteRaidPlaybackAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CombatRuleset ruleset,
        CancellationToken cancellationToken)
    {
        var checkpoints = new List<CombatCheckpoint>();
        var execution = await ExecuteCoreAsync(
            runtime,
            ruleset,
            cancellationToken,
            checkpoint => checkpoints.Add(checkpoint),
            checkpointIntervalTicks,
            captureEventLog: ruleset.CaptureEventLog);
        SyncCombatEntityState(runtime.FriendlyParticipants, execution.Friendly);
        SyncCombatEntityState(runtime.AllHostileParticipants, execution.Hostile);
        PopulatePostCombatTeams(execution.Result, execution.Friendly, execution.Hostile);
        execution.Result.StartedAt = runtime.Plan.StartsAt;
        return new CombatExecutionWithCheckpoints(execution.Result, checkpoints);
    }

    public async Task<CombatExecutionWithCheckpoints> ExecuteTournamentPlaybackAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CombatRuleset ruleset,
        CancellationToken cancellationToken)
        => await ExecuteCompactPlaybackCoreAsync(
            runtime,
            checkpointIntervalTicks,
            ruleset,
            cancellationToken);

    private async Task<CombatExecutionWithCheckpoints> ExecuteCompactPlaybackCoreAsync(
        CombatEncounterRuntime runtime,
        int checkpointIntervalTicks,
        CombatRuleset ruleset,
        CancellationToken cancellationToken)
    {
        var checkpoints = new List<CombatCheckpoint>();
        var execution = await ExecuteCoreAsync(
            runtime,
            ruleset,
            cancellationToken,
            checkpoint => checkpoints.Add(checkpoint),
            checkpointIntervalTicks,
            captureEventLog: false);
        SyncCombatEntityState(runtime.FriendlyParticipants, execution.Friendly);
        SyncCombatEntityState(runtime.AllHostileParticipants, execution.Hostile);
        PopulatePostCombatTeams(execution.Result, execution.Friendly, execution.Hostile);
        execution.Result.StartedAt = runtime.Plan.StartsAt;
        return new CombatExecutionWithCheckpoints(execution.Result, checkpoints);
    }

    public async Task<CombatResult> ExecuteSimulationAsync(
        CombatEncounterRuntime runtime,
        CombatRuleset ruleset,
        CancellationToken cancellationToken)
    {
        var execution = await ExecuteCoreAsync(
            runtime,
            ruleset,
            cancellationToken,
            captureEventLog: ruleset.CaptureEventLog);
        PopulatePostCombatTeams(execution.Result, execution.Friendly, execution.Hostile);
        execution.Result.StartedAt = runtime.Plan.StartsAt;
        return execution.Result;
    }

    private static int ResolveRandomSeed(CombatEncounterRuntime runtime) =>
        runtime.Plan.RandomSeed ?? StableRandom.Seed(
            "combat-encounter-v1",
            runtime.Plan.EncounterId.ToString("N"));

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
        Level = combatant.Level,
        ImagePath = combatant.ImagePath,
        MaxHealth = (int)combatant.GetAttribute(AttributeType.MaxHealth),
        Health = (int)combatant.Health,
        Barrier = (int)combatant.Barrier,
        Threat = combatant.Threat,
        PartyNumber = combatant.PartyNumber
    };

    private Task<ExecutionResult> ExecuteCoreAsync(
        CombatEncounterRuntime runtime,
        CombatRuleset options,
        CancellationToken cancellationToken,
        Action<CombatCheckpoint>? checkpointObserver = null,
        int checkpointIntervalTicks = 0,
        bool captureEventLog = true)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var catalog = _catalogProvider.GetCatalog();
        var precompiledCatalog = (_catalogProvider as ICompiledAbilityCatalogProvider)?.GetCompiledCatalog();
        var supplementalAbilities = (options.SupplementalAbilities ?? [])
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var abilityIds = runtime.FriendlyParticipants
            .Concat(runtime.AllHostileParticipants)
            .SelectMany(participant => GetCombatantAbilityIds(participant.Combatant, catalog))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var supplementalId in runtime.AllCombatants
                     .SelectMany(x => x.NativeAbilityIds)
                     .Where(supplementalAbilities.ContainsKey))
            abilityIds.Add(supplementalId);

        var abilitySpecs = SelectAbilitySpecsAndSummons(catalog, supplementalAbilities, abilityIds).ToList();
        var summonIds = SelectSummonIds(abilitySpecs).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var compiledAbilities = CompileSelectedAbilities(catalog, precompiledCatalog, abilitySpecs);
        var compiledStatuses = precompiledCatalog?.StatusesById
            ?? AbilityCompiler.CompileStatuses(catalog.Statuses);
        var compiledSummons = CompileSelectedSummons(catalog, precompiledCatalog, summonIds);
        var friendly = runtime.FriendlyParticipants
            .Select(participant => CreateRuntimeCombatant(
                participant.Combatant,
                CombatTeam.Friendly,
                participant.Slot.PartyNumber,
                catalog,
                compiledAbilities,
                precompiledCatalog is not null))
            .ToList();
        var hostile = runtime.HostileParticipants
            .Select(participant => CreateRuntimeCombatant(
                participant.Combatant,
                CombatTeam.Hostile,
                participant.Slot.PartyNumber,
                catalog,
                compiledAbilities,
                precompiledCatalog is not null))
            .ToList();
        var hostileReinforcementWaves = runtime.HostileReinforcementWaves
            .Select(wave => (IReadOnlyList<RuntimeCombatant>)wave
                .Select(participant => CreateRuntimeCombatant(
                    participant.Combatant,
                    CombatTeam.Hostile,
                    participant.Slot.PartyNumber,
                    catalog,
                    compiledAbilities,
                    precompiledCatalog is not null))
                .ToList())
            .ToList();
        var allHostile = hostile
            .Concat(hostileReinforcementWaves.SelectMany(x => x))
            .ToList();
        var dynamicHostiles = new List<RuntimeCombatant>();
        Func<int, IReadOnlyList<RuntimeCombatant>?>? hostileWaveFactory = null;
        if (runtime.HostileWaveFactory is not null)
        {
            hostileWaveFactory = waveNumber =>
            {
                var participants = runtime.HostileWaveFactory(waveNumber);
                if (participants is null)
                    return null;
                var wave = participants.Select(participant => CreateRuntimeCombatant(
                    participant.Combatant,
                    CombatTeam.Hostile,
                    participant.Slot.PartyNumber,
                    catalog,
                    compiledAbilities,
                    precompiledCatalog is not null)).ToList();
                dynamicHostiles.AddRange(wave);
                return wave;
            };
        }
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledAbilities,
            new FastCombatEngineOptions(
                options.MaxTicks,
                BasicAttackIntervalTicks: options.BasicAttackIntervalTicks,
                RandomSeed: options.RandomSeed,
                StartActiveAbilitiesOnCooldown: options.StartActiveAbilitiesOnCooldown,
                CaptureEventLog: captureEventLog,
                OvertimeStartsAtTick: options.OvertimeStartsAtTick,
                OvertimePowerIncreaseIntervalTicks: options.OvertimePowerIncreaseIntervalTicks,
                OvertimePowerIncreasePercent: options.OvertimePowerIncreasePercent,
                ThreatAndTankingEnabled: _threatAndTankingOptions.Enabled,
                AttentionExponent: _threatAndTankingOptions.AttentionExponent,
                MinimumAttentionWeight: _threatAndTankingOptions.MinimumAttentionWeight,
                MaximumAttentionWeight: _threatAndTankingOptions.MaximumAttentionWeight,
                ThreatHalfLifeSeconds: _threatAndTankingOptions.ThreatHalfLifeSeconds,
                BasicAttackThreatValue: _threatAndTankingOptions.BasicAttackThreatValue,
                MarkThreatBonus: _threatAndTankingOptions.MarkThreatBonus,
                CoverBudgetMaxHealthFraction: _threatAndTankingOptions.CoverBudgetMaxHealthFraction,
                Downed: options.Downed,
                WaveRecovery: options.WaveRecovery,
                HostileFury: options.HostileFury,
                CaptureCompactTelemetry: options.CaptureCompactTelemetry));
        var result = engine.Run(
            friendly,
            hostile,
            cancellationToken,
            checkpointObserver,
            checkpointIntervalTicks,
            hostileReinforcementWaves,
            hostileWaveFactory);
        AttachAbilityDefinitions(
            result,
            friendly.Concat(allHostile).Concat(dynamicHostiles),
            compiledAbilities.Values);
        var participatingHostileIds = result.EntityStats
            .Select(x => x.EntityId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(new ExecutionResult(
            result,
            friendly,
            allHostile.Concat(dynamicHostiles).Where(x => participatingHostileIds.Contains(x.Id)).ToList()));
    }

    private static void AttachAbilityDefinitions(
        CombatResult result,
        IEnumerable<RuntimeCombatant> combatants,
        IEnumerable<CompiledAbility> fallbackAbilities)
    {
        var fallbackByName = fallbackAbilities
            .Where(ability => ability.SourceSpec is not null)
            .GroupBy(ability => ability.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().SourceSpec!,
                StringComparer.OrdinalIgnoreCase);
        var definitionsByEntityId = combatants
            .GroupBy(combatant => combatant.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .SelectMany(combatant => combatant.Abilities)
                    .Where(ability => ability.Definition.SourceSpec is not null)
                    .GroupBy(ability => ability.Definition.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        abilities => abilities.Key,
                        abilities => abilities.First().Definition.SourceSpec!,
                        StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < result.EntityStats.Count; index++)
        {
            var stats = result.EntityStats[index];
            definitionsByEntityId.TryGetValue(stats.EntityId, out var definitionsByName);
            result.EntityStats[index] = stats with
            {
                Abilities = stats.Abilities.Select(ability =>
                {
                    var definition = definitionsByName?.GetValueOrDefault(ability.Name)
                                     ?? fallbackByName.GetValueOrDefault(ability.Name);
                    return definition is null ? ability : ability with { Definition = definition };
                }).ToList()
            };
        }
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
        int? partyNumber,
        AbilityCatalog catalog,
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        bool cacheStableEssenceAbilities)
    {
        var abilities = CreateCombatantAbilities(
                combatant,
                catalog,
                compiledAbilities,
                cacheStableEssenceAbilities)
            .ToList();
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
            basicAttackDamageType: behavior.DamageType,
            partyNumber: partyNumber,
            staggerDefinition: combatant.StaggerDefinition,
            staggerParticipantCount: combatant.StaggerParticipantCount,
            level: combatant.Level);
    }

    private BasicAttackBehavior ResolveBasicAttackBehavior(CombatEntity combatant)
    {
        if (combatant.MainHandEquipment?.ProgressionData is { } progression)
            return ToBasicAttackBehavior(progression.Behavior);
        return BasicAttackBehavior.Default;
    }

    private static BasicAttackBehavior ToBasicAttackBehavior(EquipmentBehaviorDefinition behavior)
    {
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
        IReadOnlyDictionary<string, CompiledAbility> compiledAbilities,
        bool cacheStableEssenceAbilities)
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
                if (cacheStableEssenceAbilities && combatant.TemporaryAbilityModifiers.Count == 0)
                {
                    var cacheKey = new EssenceAbilityCacheKey(
                        catalog,
                        abilityId,
                        essence.EssenceDefinitionId,
                        essence.IsEvolved,
                        essence.AscensionTier);
                    if (!_compiledEssenceAbilities.TryGetValue(cacheKey, out var compiledAbility))
                    {
                        var modifiedSpec = ApplyEvolutionModifiers(baseSpec, essence, catalog);
                        modifiedSpec = EssenceAbilityProgressionScaler.Apply(modifiedSpec, essence.AscensionTier);
                        compiledAbility = ReferenceEquals(baseSpec, modifiedSpec)
                            ? compiledAbilities[abilityId]
                            : AbilityCompiler.CompileAbility(modifiedSpec, _abilityThreatTuning);
                        _compiledEssenceAbilities.Add(cacheKey, compiledAbility);
                    }

                    yield return compiledAbility;
                    continue;
                }

                var uncachedSpec = ApplyEvolutionModifiers(baseSpec, essence, catalog);
                uncachedSpec = EssenceAbilityProgressionScaler.Apply(uncachedSpec, essence.AscensionTier);
                uncachedSpec = ApplyTemporaryAbilityModifiers(uncachedSpec, combatant, catalog);
                yield return ReferenceEquals(baseSpec, uncachedSpec)
                    ? compiledAbilities[abilityId]
                    : AbilityCompiler.CompileAbility(uncachedSpec, _abilityThreatTuning);
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
                : AbilityCompiler.CompileAbility(modifiedSpec, _abilityThreatTuning);
        }

        foreach (var abilityId in GetEquipmentSetAbilityIds(combatant))
        {
            if (!selected.Add(abilityId)
                || !catalog.AbilitiesById.TryGetValue(abilityId, out var baseSpec)
                || !compiledAbilities.TryGetValue(abilityId, out var compiledAbility))
                continue;

            var modifiedSpec = ApplyTemporaryAbilityModifiers(baseSpec, combatant, catalog);
            yield return ReferenceEquals(baseSpec, modifiedSpec)
                ? compiledAbility
                : AbilityCompiler.CompileAbility(modifiedSpec, _abilityThreatTuning);
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
                    : AbilityCompiler.CompileAbility(modifiedSpec, _abilityThreatTuning);
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

    private IReadOnlyDictionary<string, CompiledAbility> CompileSelectedAbilities(
        AbilityCatalog catalog,
        CompiledAbilityCatalog? precompiledCatalog,
        IReadOnlyList<AbilitySpec> abilitySpecs)
    {
        if (precompiledCatalog is null)
            return AbilityCompiler.CompileAbilities(abilitySpecs, _abilityThreatTuning);

        var compiled = new Dictionary<string, CompiledAbility>(abilitySpecs.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var spec in abilitySpecs)
        {
            if (catalog.AbilitiesById.TryGetValue(spec.Id, out var catalogSpec)
                && ReferenceEquals(spec, catalogSpec))
            {
                compiled.Add(spec.Id, precompiledCatalog.AbilitiesById[spec.Id]);
            }
            else
            {
                compiled.Add(spec.Id, AbilityCompiler.CompileAbility(spec, _abilityThreatTuning));
            }
        }

        return compiled;
    }

    private IReadOnlyDictionary<string, CompiledSummon> CompileSelectedSummons(
        AbilityCatalog catalog,
        CompiledAbilityCatalog? precompiledCatalog,
        IReadOnlySet<string> summonIds)
    {
        if (precompiledCatalog is null)
        {
            return AbilityCompiler.CompileSummons(
                summonIds.Select(summonId => catalog.SummonsById[summonId]),
                _abilityThreatTuning);
        }

        var compiled = new Dictionary<string, CompiledSummon>(summonIds.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var summonId in summonIds)
            compiled.Add(summonId, precompiledCatalog.SummonsById[summonId]);
        return compiled;
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
            else if (modifier.Operation.Equals("DelayCooldowns", StringComparison.OrdinalIgnoreCase))
                ApplyCooldownDelay(clone, modifier.Value);
        }

        return clone ?? spec;
    }

    private static bool CanApplyModifier(AbilitySpec spec, EssenceAbilityModifierDefinition modifier)
    {
        if (string.IsNullOrWhiteSpace(modifier.Target))
            return false;

        if (modifier.Operation.Equals("DelayCooldowns", StringComparison.OrdinalIgnoreCase))
            return spec.Id.Equals(modifier.Target, StringComparison.OrdinalIgnoreCase);

        if (spec.Effects.Any(x => x.Id.Equals(modifier.Target, StringComparison.OrdinalIgnoreCase)))
            return true;

        return modifier.Effect is not null
            && spec.Triggers.Any(x => x.EffectIds.Contains(modifier.Target, StringComparer.OrdinalIgnoreCase));
    }

    private static void ApplyCooldownDelay(AbilitySpec spec, double delayFraction)
    {
        if (delayFraction <= 0)
            return;

        if (spec.CooldownTicks > 0)
            spec.CooldownTicks = ScaleCooldownTicks(spec.CooldownTicks, delayFraction);

        foreach (var trigger in spec.Triggers.Where(x => x.InternalCooldownTicks > 0))
            trigger.InternalCooldownTicks = ScaleCooldownTicks(trigger.InternalCooldownTicks, delayFraction);
    }

    private static int ScaleCooldownTicks(int ticks, double delayFraction) =>
        Math.Max(1, (int)Math.Ceiling(ticks * (1 + delayFraction)));

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
        effect.SummonPowerMultiplier *= 1 + modifier.Value;
        effect.SummonHealthMultiplier *= 1 + modifier.Value;
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
            ThreatValue = spec.ThreatValue,
            ThreatMultiplier = spec.ThreatMultiplier,
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
            ScalingAttributeSubject = effect.ScalingAttributeSubject,
            ScalingCoefficient = effect.ScalingCoefficient,
            MaximumScalingCoefficient = effect.MaximumScalingCoefficient,
            EventMagnitudeCoefficient = effect.EventMagnitudeCoefficient,
            ScalingCondition = effect.ScalingCondition,
            ScalingConditionSubject = effect.ScalingConditionSubject,
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
            AlternativeStatusId = effect.AlternativeStatusId,
            AbilityId = effect.AbilityId,
            Condition = effect.Condition,
            AlternativeCondition = effect.AlternativeCondition,
            TargetCondition = effect.TargetCondition,
            SummonId = effect.SummonId,
            CountAllOwnedSummons = effect.CountAllOwnedSummons,
            MaximumCount = effect.MaximumCount,
            RepeatCount = effect.RepeatCount,
            HealthStepPercent = effect.HealthStepPercent,
            RepeatPerOwnedSummonId = effect.RepeatPerOwnedSummonId,
            ScalingOwnedSummonId = effect.ScalingOwnedSummonId,
            OwnedSummonScalingCoefficient = effect.OwnedSummonScalingCoefficient,
            SummonGroupId = effect.SummonGroupId,
            LinkedEffectId = effect.LinkedEffectId,
            SummonPowerMultiplier = effect.SummonPowerMultiplier,
            SummonHealthMultiplier = effect.SummonHealthMultiplier,
            Resource = effect.Resource,
            DurationTicks = effect.DurationTicks,
            RefreshDuration = effect.RefreshDuration,
            IntervalTicks = effect.IntervalTicks,
            Uses = effect.Uses,
            OncePerTarget = effect.OncePerTarget,
            ExcludeEventTarget = effect.ExcludeEventTarget,
            IgnoreTaunt = effect.IgnoreTaunt,
            ExcludeSummons = effect.ExcludeSummons,
            UseHealthPercentage = effect.UseHealthPercentage,
            RandomizeTies = effect.RandomizeTies,
            GuaranteedConditionApplication = effect.GuaranteedConditionApplication,
            StaggerPower = effect.StaggerPower,
            MaintainWhileConditionsMet = effect.MaintainWhileConditionsMet,
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

        foreach (var abilityId in GetEquipmentSetAbilityIds(combatant))
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

    private IEnumerable<string> GetEquipmentSetAbilityIds(CombatEntity combatant) =>
        _equipmentCatalog is null
            ? []
            : EquipmentSetBonusResolver.ResolveGrantedAbilityIds(
                combatant.Equipment,
                _equipmentCatalog.EquipmentSets);

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

    private readonly record struct EssenceAbilityCacheKey(
        AbilityCatalog Catalog,
        string AbilityId,
        string EssenceDefinitionId,
        bool IsEvolved,
        int AscensionTier);

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
