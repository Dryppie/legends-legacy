using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Services.LL.Combat.Engine;

public sealed class AbilityCatalogDiagnostics : IAbilityCatalogDiagnostics
{
    private static readonly string[] DiagnosticAbilityIds =
    [
        "ability.creature.goblin.shiv_jab",
        "ability.creature.lumo_wisp.lumo_barrier",
        "ability.creature.flame_imp.firebomb_toss",
        "ability.creature.thornback_boar.bristling_hide"
    ];
    private readonly IAbilityCatalogProvider _catalogProvider;

    public AbilityCatalogDiagnostics(IAbilityCatalogProvider catalogProvider)
    {
        _catalogProvider = catalogProvider;
    }

    public AbilityCatalogDiagnosticReport RunTrainingEncounter()
    {
        var catalog = _catalogProvider.GetCatalog();
        var missingDiagnosticAbilities = DiagnosticAbilityIds
            .Where(id => !catalog.AbilitiesById.ContainsKey(id))
            .ToList();
        var compiledAbilities = AbilityCompiler.CompileAbilities(
            DiagnosticAbilityIds
                .Where(catalog.AbilitiesById.ContainsKey)
                .Select(id => catalog.AbilitiesById[id]));
        var compiledCatalogAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
        var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
        var compiledSummons = AbilityCompiler.CompileSummons(catalog.Summons);
        var friendly = CreateCombatant("diagnostic-friendly", CombatTeam.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant(
            "diagnostic-hostile",
            CombatTeam.Hostile,
            compiledAbilities.TryGetValue("ability.creature.goblin.shiv_jab", out var hostileStrike)
                ? [hostileStrike]
                : []);
        var engine = new FastCombatEngine(
            compiledStatuses,
            compiledSummons,
            compiledCatalogAbilities,
            new FastCombatEngineOptions(MaxTicks: 40, RandomSeed: 7));
        var result = engine.Run([friendly], [hostile]);
        var failures = new List<string>();

        var directDamageObserved = result.EventLog.Any(x => x.EventType == EventType.Damage && !x.Source.StartsWith("condition.", StringComparison.OrdinalIgnoreCase));
        var barrierObserved = result.EventLog.Any(x => x.EventType == EventType.RestoreBarrier);
        var damageOverTimeObserved = result.EventLog.Any(x => x.Source == "condition.burn" && x.EventType == EventType.Damage);
        var reflectObserved = result.EventLog.Any(x => x.Source == "condition.thorns" && x.EventType == EventType.ReflectedDamage);
        var summonDiagnostics = catalog.Summons
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => new AbilityCatalogSummonDiagnostic(
                x.Id,
                x.Name,
                x.ImagePath,
                x.DurationTicks,
                x.MaxActive,
                x.DurationTicks > 0,
                ExpiresOnOwnerDeath: true,
                x.AbilityIds,
                x.Tags))
            .ToList();
        var summonAbilityReferenceCount = catalog.Summons.Sum(x => x.AbilityIds.Count);

        foreach (var missingAbilityId in missingDiagnosticAbilities)
            failures.Add($"Diagnostic ability '{missingAbilityId}' was not found.");
        if (!directDamageObserved)
            failures.Add("Training direct damage was not observed.");
        if (!barrierObserved)
            failures.Add("Training barrier was not observed.");
        if (!damageOverTimeObserved)
            failures.Add("Training damage over time was not observed.");
        if (!reflectObserved)
            failures.Add("Training reflect damage was not observed.");
        foreach (var summon in catalog.Summons)
        {
            foreach (var abilityId in summon.AbilityIds)
            {
                if (!catalog.AbilitiesById.ContainsKey(abilityId))
                    failures.Add($"Summon '{summon.Id}' references missing ability '{abilityId}'.");
            }
        }

        return new AbilityCatalogDiagnosticReport(
            catalog.Abilities.Count,
            catalog.Statuses.Count,
            catalog.Summons.Count,
            catalog.AbilityIdsByTag.Count,
            catalog.StatusIdsByTag.Count,
            catalog.SummonIdsByTag.Count,
            catalog.AbilityIdsByTrigger.Count,
            catalog.Summons.Count(x => x.DurationTicks > 0),
            catalog.Summons.Count(x => x.DurationTicks <= 0),
            summonAbilityReferenceCount,
            summonDiagnostics,
            result.Outcome.ToString(),
            result.Duration,
            result.EventLog.Count,
            directDamageObserved,
            barrierObserved,
            damageOverTimeObserved,
            reflectObserved,
            failures);
    }

    private static RuntimeCombatant CreateCombatant(
        string id,
        CombatTeam team,
        IEnumerable<CompiledAbility> abilities) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 200,
                [AttributeType.Power] = 50,
                [AttributeType.CritDamage] = 100
            },
            abilities,
            ["Role.Diagnostic"]);
}
