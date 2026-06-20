using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;

namespace Services.LL.Combat.V2;

public sealed class AbilityCatalogV2Diagnostics : IAbilityCatalogV2Diagnostics
{
    private const string TrainingEssenceId = "essence.v2.training";
    private readonly IAbilityCatalogV2Provider _catalogProvider;

    public AbilityCatalogV2Diagnostics(IAbilityCatalogV2Provider catalogProvider)
    {
        _catalogProvider = catalogProvider;
    }

    public AbilityCatalogV2DiagnosticReport RunTrainingEncounter()
    {
        var catalog = _catalogProvider.GetCatalog();
        var compiledAbilities = AbilityCompilerV2.CompileAbilities(
            catalog.OwningEssenceByAbilityId
                .Where(x => x.Value.Equals(TrainingEssenceId, StringComparison.OrdinalIgnoreCase))
                .Select(x => catalog.AbilitiesById[x.Key]));
        var compiledCatalogAbilities = AbilityCompilerV2.CompileAbilities(catalog.Abilities);
        var compiledStatuses = AbilityCompilerV2.CompileStatuses(catalog.Statuses);
        var compiledSummons = AbilityCompilerV2.CompileSummons(catalog.Summons);
        var friendly = CreateCombatant("diagnostic-friendly", CombatTeamV2.Friendly, compiledAbilities.Values);
        var hostile = CreateCombatant("diagnostic-hostile", CombatTeamV2.Hostile, []);
        var engine = new FastCombatEngineV2(
            compiledStatuses,
            compiledSummons,
            compiledCatalogAbilities,
            new FastCombatEngineV2Options(MaxTicks: 40, RandomSeed: 7));
        var result = engine.Run([friendly], [hostile]);
        var failures = new List<string>();

        var directDamageObserved = result.EventLog.Any(x => x.Source == "effect.damage.main" && x.EventType == EventType.Damage);
        var barrierObserved = result.EventLog.Any(x => x.Source == "effect.barrier.main" && x.EventType == EventType.RestoreBarrier);
        var damageOverTimeObserved = result.EventLog.Any(x => x.Source == "effect.burn.dot" && x.EventType == EventType.Damage);
        var reflectObserved = result.EventLog.Any(x => x.Source == "effect.reflect.damage" && x.EventType == EventType.Damage);
        var summonDiagnostics = catalog.Summons
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => new AbilityCatalogV2SummonDiagnostic(
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

        if (compiledAbilities.Count == 0)
            failures.Add($"No v2 abilities found for '{TrainingEssenceId}'.");
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

        return new AbilityCatalogV2DiagnosticReport(
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

    private static RuntimeCombatantV2 CreateCombatant(
        string id,
        CombatTeamV2 team,
        IEnumerable<CompiledAbilityV2> abilities) =>
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
