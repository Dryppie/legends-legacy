using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Services.LL.Combat.Engine;

public sealed class AbilityCatalogBehaviorDiagnostics : IAbilityCatalogBehaviorDiagnostics
{
    private readonly IAbilityCatalogProvider _catalogProvider;
    private readonly IEssenceDefinitionRepository? _essenceDefinitions;
    private readonly IConfiguration _config;
    private readonly string _contentRootPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AbilityCatalogBehaviorDiagnostics(
        IAbilityCatalogProvider catalogProvider,
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions jsonOptions,
        IEssenceDefinitionRepository? essenceDefinitions = null)
    {
        _catalogProvider = catalogProvider;
        _essenceDefinitions = essenceDefinitions;
        _config = config;
        _contentRootPath = contentRootPath;
        _jsonOptions = jsonOptions;
    }

    public AbilityCatalogBehaviorDiagnosticReport Analyze()
    {
        var catalog = _catalogProvider.GetCatalog();
        var scenarios = ReadManifest();
        var results = scenarios
            .Select(scenario => RunScenario(scenario, catalog))
            .ToList();

        return new AbilityCatalogBehaviorDiagnosticReport(
            results.Count,
            results.Count(x => x.Passed),
            results.Count(x => !x.Passed),
            results,
            catalog.Abilities.Count,
            scenarios.Select(x => x.AbilityId).Distinct(StringComparer.OrdinalIgnoreCase).Count(id => catalog.AbilitiesById.ContainsKey(id)),
            catalog.Abilities
                .Select(x => x.Id)
                .Except(scenarios.Select(x => x.AbilityId), StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private IReadOnlyList<AbilityBehaviorScenario> ReadManifest()
    {
        var contentRoot = _config["Content:Root"] ?? "Data";
        var path = Path.Combine(_contentRootPath, contentRoot, "combat", "ability-behaviors.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find ability behavior manifest '{path}'.", path);

        return JsonSerializer.Deserialize<List<AbilityBehaviorScenario>>(File.ReadAllText(path), _jsonOptions) ?? [];
    }

    private AbilityCatalogBehaviorScenarioResult RunScenario(
        AbilityBehaviorScenario scenario,
        AbilityCatalog catalog)
    {
        var failures = new List<string>();
        if (!catalog.AbilitiesById.ContainsKey(scenario.AbilityId))
        {
            failures.Add($"Ability '{scenario.AbilityId}' does not exist in catalog.");
            return BuildResult(scenario, failures);
        }

        var friendlyAbilityIds = scenario.FriendlyAbilityIds.Count == 0
            ? [scenario.AbilityId]
            : scenario.FriendlyAbilityIds;
        var missingAbilityId = friendlyAbilityIds
            .Concat(scenario.HostileAbilityIds)
            .FirstOrDefault(id => !catalog.AbilitiesById.ContainsKey(id));
        if (missingAbilityId is not null)
        {
            failures.Add($"Referenced ability '{missingAbilityId}' does not exist in catalog.");
            return BuildResult(scenario, failures);
        }

        try
        {
            if (scenario.UsesEssenceLoadout)
                return RunEssenceScenario(scenario, catalog, failures);

            var allAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
            var friendlyAbilities = friendlyAbilityIds.Select(id => allAbilities[id]).ToList();
            var hostileAbilities = scenario.HostileAbilityIds.Select(id => allAbilities[id]).ToList();
            var statuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
            var summons = AbilityCompiler.CompileSummons(catalog.Summons);
            var friendly = CreateCombatant(
                "friendly",
                CombatTeam.Friendly,
                friendlyAbilities,
                scenario.FriendlyStats,
                scenario.FriendlyHealth);
            var hostiles = Enumerable.Range(1, Math.Max(1, scenario.HostileCount))
                .Select(index => CreateCombatant(
                    $"hostile-{index}",
                    CombatTeam.Hostile,
                    hostileAbilities,
                    scenario.HostileStats,
                    scenario.HostileHealth))
                .ToList();
            var combatants = hostiles.Concat([friendly]).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            ApplyInitialStatuses(scenario.InitialStatuses, statuses, combatants);
            ApplyInitialConditions(scenario.InitialConditions, combatants);
            var engine = new FastCombatEngine(
                statuses,
                summons,
                allAbilities,
                new FastCombatEngineOptions(
                    MaxTicks: Math.Max(1, scenario.MaxTicks),
                    BasicAttackIntervalTicks: scenario.BasicAttackIntervalTicks,
                    RandomSeed: scenario.RandomSeed));
            var result = engine.Run([friendly], hostiles);

            foreach (var expected in scenario.ExpectedLogs)
                CheckExpectedLog(catalog, expected, result, failures);

            foreach (var expected in scenario.ExpectedStatuses)
                CheckExpectedStatus(scenario, expected, combatants, failures);
            foreach (var expected in scenario.ExpectedConditions)
                CheckExpectedCondition(expected, combatants, failures);

            return new AbilityCatalogBehaviorScenarioResult(
                scenario.Id,
                scenario.AbilityId,
                failures.Count == 0,
                result.Outcome.ToString(),
                result.Duration,
                result.EventLog.Count,
                failures);
        }
        catch (Exception ex)
        {
            failures.Add($"Scenario threw {ex.GetType().Name}: {ex.Message}");
            return BuildResult(scenario, failures);
        }
    }

    private AbilityCatalogBehaviorScenarioResult RunEssenceScenario(
        AbilityBehaviorScenario scenario,
        AbilityCatalog catalog,
        List<string> failures)
    {
        if (_essenceDefinitions is null)
        {
            failures.Add("Scenario requires essence loadouts, but no essence definition repository is available.");
            return BuildResult(scenario, failures);
        }

        var friendlyCharacter = CreateCharacter("friendly");
        var friendlyCombatant = CreateCombatEntity(
            "friendly",
            friendlyCharacter,
            scenario.FriendlyStats,
            scenario.FriendlyHealth,
            scenario.FriendlyEssenceIds,
            scenario.EvolvedFriendlyEssenceIds);
        var hostileParticipants = Enumerable.Range(1, Math.Max(1, scenario.HostileCount))
            .Select(index =>
            {
                var character = CreateCharacter($"hostile-{index}");
                var combatant = CreateCombatEntity(
                    $"hostile-{index}",
                    character,
                    scenario.HostileStats,
                    scenario.HostileHealth,
                    scenario.HostileEssenceIds,
                    scenario.EvolvedHostileEssenceIds);
                return (Character: character, Combatant: combatant);
            })
            .ToList();
        var plan = new CombatEncounterPlan(
            Guid.NewGuid(),
            CombatMode.Idle,
            1,
            DateTimeOffset.UtcNow,
            [
                new CombatParticipantSlot("friendly", friendlyCharacter.Id, CombatSide.Friendly),
                .. hostileParticipants.Select(item => new CombatParticipantSlot(item.Combatant.Id, item.Character.Id, CombatSide.Hostile))
            ],
            new IdleEncounterSourceContext(friendlyCharacter.Id, new Area(), TimeSpan.FromSeconds(1)))
        {
            ContentType = CombatContentType.Idle
        };
        var runtime = new CombatEncounterRuntime(
            plan,
            [new CombatRuntimeParticipant(plan.FriendlyParticipants.Single(), friendlyCharacter, friendlyCombatant)],
            [.. hostileParticipants.Select((item, index) => new CombatRuntimeParticipant(plan.HostileParticipants[index], item.Character, item.Combatant))]);
        var executor = new CombatEngineExecutor(_catalogProvider, _essenceDefinitions);
        var result = executor.ExecuteSimulationAsync(
                runtime,
                new CombatRuleset(
                    scenario.RandomSeed,
                    MaxTicks: 6000,
                    StartActiveAbilitiesOnCooldown: true),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        foreach (var expected in scenario.ExpectedLogs)
            CheckExpectedLog(catalog, expected, result, failures);

        if (scenario.ExpectedStatuses.Count > 0)
            failures.Add("Essence-loadout scenarios currently support expectedLogs only.");

        return new AbilityCatalogBehaviorScenarioResult(
            scenario.Id,
            scenario.AbilityId,
            failures.Count == 0,
            result.Outcome.ToString(),
            result.Duration,
            result.EventLog.Count,
            failures);
    }

    private static AbilityCatalogBehaviorScenarioResult BuildResult(
        AbilityBehaviorScenario scenario,
        IReadOnlyList<string> failures) =>
        new(
            scenario.Id,
            scenario.AbilityId,
            failures.Count == 0,
            Outcome: null,
            Duration: 0,
            EventCount: 0,
            failures);

    private static void CheckExpectedLog(
        AbilityCatalog catalog,
        ExpectedLogObservation expected,
        CombatResult result,
        ICollection<string> failures)
    {
        var minimumMagnitude = expected.MinMagnitude;
        if (minimumMagnitude is not null
            && expected.EventType is EventType.Damage
                or EventType.DamageCrit
                or EventType.Heal
                or EventType.HealCrit
            && CatalogEffectUsesCombatMagnitudeVariance(catalog, expected.Source))
        {
            minimumMagnitude = (int)Math.Floor(
                minimumMagnitude.Value
                * (1d - FastCombatEngine.CombatMagnitudeVariance));
        }

        var matches = result.EventLog
            .Where(x => string.Equals(x.Source, expected.Source, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.EventType == expected.EventType)
            .Where(x => expected.TargetId is null || string.Equals(x.TargetId, expected.TargetId, StringComparison.OrdinalIgnoreCase))
            .Where(x => minimumMagnitude is null || x.Magnitude >= minimumMagnitude)
            .ToList();

        if (matches.Count < expected.MinCount)
        {
            var observedSources = result.EventLog
                .Where(x => x.EventType == expected.EventType)
                .Select(x => x.Source)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            failures.Add(
                $"{expected.Source}: expected at least {expected.MinCount} {expected.EventType} log(s), found {matches.Count}. "
                + $"Observed sources: {string.Join(", ", observedSources)}.");
        }
    }

    private static bool CatalogEffectUsesCombatMagnitudeVariance(
        AbilityCatalog catalog,
        string effectId) =>
        catalog.Abilities
            .SelectMany(ability => ability.Effects)
            .Concat(catalog.Statuses.SelectMany(status => status.Effects))
            .Any(effect =>
                effect.Id.Equals(effectId, StringComparison.OrdinalIgnoreCase)
                && effect.ScalingAttribute == AttributeType.Power
                && effect.Operation is AbilityEffectOperation.Damage
                    or AbilityEffectOperation.Heal);

    private static void CheckExpectedStatus(
        AbilityBehaviorScenario scenario,
        ExpectedStatusObservation expected,
        IReadOnlyDictionary<string, RuntimeCombatant> combatants,
        ICollection<string> failures)
    {
        if (!combatants.TryGetValue(expected.CombatantId, out var combatant))
        {
            failures.Add($"{expected.StatusId}: combatant '{expected.CombatantId}' was not present.");
            return;
        }

        var stacks = combatant.GetStatusStacks(expected.StatusId);
        if (stacks < expected.MinStacks)
        {
            failures.Add(
                $"{expected.StatusId}: expected at least {expected.MinStacks} stack(s) on {expected.CombatantId}, found {stacks}.");
        }
    }

    private static RuntimeCombatant CreateCombatant(
        string id,
        CombatTeam team,
        IEnumerable<CompiledAbility> abilities,
        IReadOnlyDictionary<AttributeType, float> statOverrides,
        float? health)
    {
        var stats = new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = 200,
            [AttributeType.Power] = 50,
            [AttributeType.HealingPowerPercent] = 50,
            [AttributeType.CritDamage] = 100,
            [AttributeType.DodgeChance] = 0
        };

        foreach (var stat in statOverrides)
            stats[stat.Key] = stat.Value;

        var combatant = new RuntimeCombatant(
            id,
            id,
            team,
            stats,
            abilities,
            ["Role.BehaviorDiagnostic"]);

        if (health is not null)
            combatant.SetHealth(health.Value);

        return combatant;
    }

    private static Character CreateCharacter(string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Level = 10
        };

    private static CombatEntity CreateCombatEntity(
        string id,
        Character character,
        IReadOnlyDictionary<AttributeType, float> statOverrides,
        float? health,
        IReadOnlyList<string> essenceIds,
        IReadOnlyList<string> evolvedEssenceIds)
    {
        var combatant = new CombatEntity(character)
        {
            Id = id,
            Name = character.Name,
            Level = character.Level
        };
        var stats = CreateStats(statOverrides);
        foreach (var stat in stats)
        {
            character.BaseCombatAttributes[stat.Key] = stat.Value;
            character.CombatAttributes[stat.Key] = stat.Value;
            combatant.BaseCombatAttributes[stat.Key] = stat.Value;
            combatant.CombatAttributes[stat.Key] = stat.Value;
        }

        combatant.SyncCurrentHealthToMax();
        if (health is not null)
            combatant.SetCurrentHealth(health.Value);

        var evolvedIds = evolvedEssenceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var essenceId in essenceIds.Concat(evolvedEssenceIds).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            combatant.EquippedEssences.Add(new PlayerEssence
            {
                Id = Guid.NewGuid(),
                CharacterId = character.Id,
                EssenceDefinitionId = essenceId,
                Level = 1,
                IsEvolved = evolvedIds.Contains(essenceId)
            });
        }

        return combatant;
    }

    private static Dictionary<AttributeType, float> CreateStats(IReadOnlyDictionary<AttributeType, float> statOverrides)
    {
        var stats = new Dictionary<AttributeType, float>
        {
            [AttributeType.MaxHealth] = 200,
            [AttributeType.Power] = 50,
            [AttributeType.HealingPowerPercent] = 50,
            [AttributeType.CritDamage] = 100,
            [AttributeType.DodgeChance] = 0
        };

        foreach (var stat in statOverrides)
            stats[stat.Key] = stat.Value;

        return stats;
    }

    private static void ApplyInitialStatuses(
        IReadOnlyList<InitialStatusSpec> initialStatuses,
        IReadOnlyDictionary<string, CompiledStatus> statuses,
        IReadOnlyDictionary<string, RuntimeCombatant> combatants)
    {
        foreach (var initialStatus in initialStatuses)
        {
            if (!combatants.TryGetValue(initialStatus.CombatantId, out var owner))
                throw new InvalidOperationException($"Initial status combatant '{initialStatus.CombatantId}' was not present.");

            if (!statuses.TryGetValue(initialStatus.StatusId, out var status))
                throw new InvalidOperationException($"Initial status '{initialStatus.StatusId}' has not been compiled.");

            var source = !string.IsNullOrWhiteSpace(initialStatus.SourceCombatantId)
                && combatants.TryGetValue(initialStatus.SourceCombatantId, out var requestedSource)
                    ? requestedSource
                    : owner;

            owner.Statuses.Add(new RuntimeStatus(status, source, owner, Math.Max(1, initialStatus.Stacks)));
        }
    }

    private static void CheckExpectedCondition(
        ExpectedConditionObservation expected,
        IReadOnlyDictionary<string, RuntimeCombatant> combatants,
        ICollection<string> failures)
    {
        if (!combatants.TryGetValue(expected.CombatantId, out var combatant))
        {
            failures.Add($"{expected.Condition}: combatant '{expected.CombatantId}' was not present.");
            return;
        }

        var stacks = combatant.GetConditionStacks(expected.Condition);
        if (stacks < expected.MinStacks)
        {
            failures.Add(
                $"{expected.Condition}: expected at least {expected.MinStacks} stack(s) on {expected.CombatantId}, found {stacks}.");
        }
    }

    private static void ApplyInitialConditions(
        IReadOnlyList<InitialConditionSpec> initialConditions,
        IReadOnlyDictionary<string, RuntimeCombatant> combatants)
    {
        long applicationOrder = 0;
        foreach (var initialCondition in initialConditions)
        {
            if (!combatants.TryGetValue(initialCondition.CombatantId, out var owner))
                throw new InvalidOperationException($"Initial condition combatant '{initialCondition.CombatantId}' was not present.");

            var source = !string.IsNullOrWhiteSpace(initialCondition.SourceCombatantId)
                && combatants.TryGetValue(initialCondition.SourceCombatantId, out var requestedSource)
                    ? requestedSource
                    : owner;

            owner.Conditions.Add(
                new RuntimeCondition(
                    initialCondition.Condition,
                    source,
                    owner,
                    Math.Max(1, initialCondition.Stacks),
                    Math.Max(0, initialCondition.DurationTicks),
                    source.GetAttribute(AttributeType.Power),
                    ++applicationOrder,
                    $"condition.{initialCondition.Condition.ToString().ToLowerInvariant()}"));
        }
    }

    private sealed class AbilityBehaviorScenario
    {
        public string Id { get; set; } = string.Empty;
        public string AbilityId { get; set; } = string.Empty;
        public List<string> FriendlyAbilityIds { get; set; } = [];
        public List<string> HostileAbilityIds { get; set; } = [];
        public List<string> FriendlyEssenceIds { get; set; } = [];
        public List<string> HostileEssenceIds { get; set; } = [];
        public List<string> EvolvedFriendlyEssenceIds { get; set; } = [];
        public List<string> EvolvedHostileEssenceIds { get; set; } = [];
        public int HostileCount { get; set; } = 1;
        public int MaxTicks { get; set; } = 1;
        public int BasicAttackIntervalTicks { get; set; } = 1000;
        public int RandomSeed { get; set; } = 29;
        public float? FriendlyHealth { get; set; }
        public float? HostileHealth { get; set; }
        public Dictionary<AttributeType, float> FriendlyStats { get; set; } = [];
        public Dictionary<AttributeType, float> HostileStats { get; set; } = [];
        public List<InitialStatusSpec> InitialStatuses { get; set; } = [];
        public List<InitialConditionSpec> InitialConditions { get; set; } = [];
        public List<ExpectedLogObservation> ExpectedLogs { get; set; } = [];
        public List<ExpectedStatusObservation> ExpectedStatuses { get; set; } = [];
        public List<ExpectedConditionObservation> ExpectedConditions { get; set; } = [];
        public bool UsesEssenceLoadout =>
            FriendlyEssenceIds.Count > 0
            || HostileEssenceIds.Count > 0
            || EvolvedFriendlyEssenceIds.Count > 0
            || EvolvedHostileEssenceIds.Count > 0;
    }

    private sealed class InitialStatusSpec
    {
        public string CombatantId { get; set; } = string.Empty;
        public string? SourceCombatantId { get; set; }
        public string StatusId { get; set; } = string.Empty;
        public int Stacks { get; set; } = 1;
    }

    private sealed class InitialConditionSpec
    {
        public string CombatantId { get; set; } = string.Empty;
        public string? SourceCombatantId { get; set; }
        public StandardConditionType Condition { get; set; }
        public int Stacks { get; set; } = 1;
        public int DurationTicks { get; set; }
    }

    private sealed class ExpectedLogObservation
    {
        public string Source { get; set; } = string.Empty;
        public EventType EventType { get; set; }
        public string? TargetId { get; set; }
        public int MinCount { get; set; } = 1;
        public int? MinMagnitude { get; set; }
    }

    private sealed class ExpectedStatusObservation
    {
        public string CombatantId { get; set; } = string.Empty;
        public string StatusId { get; set; } = string.Empty;
        public int MinStacks { get; set; } = 1;
    }

    private sealed class ExpectedConditionObservation
    {
        public string CombatantId { get; set; } = string.Empty;
        public StandardConditionType Condition { get; set; }
        public int MinStacks { get; set; } = 1;
    }
}
