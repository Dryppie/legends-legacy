using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities.V2;
using Domain.Models.Essences.Definitions;

namespace Services.LL.Combat.V2;

public sealed class AbilityCatalogV2CoverageAnalyzer : IAbilityCatalogV2CoverageAnalyzer
{
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly IAbilityCatalogV2Provider _v2CatalogProvider;

    public AbilityCatalogV2CoverageAnalyzer(
        IEssenceDefinitionRepository essenceDefinitions,
        IAbilityCatalogV2Provider v2CatalogProvider)
    {
        _essenceDefinitions = essenceDefinitions;
        _v2CatalogProvider = v2CatalogProvider;
    }

    public AbilityCatalogV2CoverageReport Analyze()
    {
        var essences = _essenceDefinitions.GetAll();
        var catalog = _v2CatalogProvider.GetCatalog();
        var abilitiesByOwnerAndKind = catalog.Abilities
            .Where(x => !string.IsNullOrWhiteSpace(x.OwningEssenceId))
            .GroupBy(x => (Owner: x.OwningEssenceId!, x.Kind))
            .ToDictionary(x => x.Key, x => x.ToList());
        var slots = new List<AbilityCatalogV2SlotCoverage>();
        var gaps = new List<AbilityCatalogV2CoverageGap>();

        foreach (var essence in essences.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
        {
            AddSlot(essence, "Active", essence.ActiveAbilityId, AbilitySpecKind.Active, catalog, abilitiesByOwnerAndKind, slots, gaps);
            AddSlot(essence, "Passive", essence.PassiveAbilityId, AbilitySpecKind.Passive, catalog, abilitiesByOwnerAndKind, slots, gaps);
        }

        var ownedEssenceIds = essences.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var summonAbilityIds = catalog.Summons
            .SelectMany(x => x.AbilityIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unownedAbilityIds = catalog.Abilities
            .Where(x => !summonAbilityIds.Contains(x.Id)
                && (string.IsNullOrWhiteSpace(x.OwningEssenceId) || !ownedEssenceIds.Contains(x.OwningEssenceId)))
            .Select(x => x.Id)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var runtimeLoadoutChecks = essences
            .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(essence => CheckRuntimeLoadout(essence, catalog))
            .ToList();

        return new AbilityCatalogV2CoverageReport(
            essences.Count,
            slots.Count,
            slots.Count(x => x.HasOwnedV2Ability && x.KindMatches),
            slots.Count(x => x.CurrentReferenceExistsInV2),
            slots,
            gaps,
            unownedAbilityIds,
            runtimeLoadoutChecks);
    }

    private static AbilityCatalogV2RuntimeLoadoutCheck CheckRuntimeLoadout(
        EssenceDefinition essence,
        AbilityCatalogV2 catalog)
    {
        var abilityIds = catalog.AbilityIdsByOwningEssence.GetValueOrDefault(essence.Id) ?? [];
        if (abilityIds.Count == 0)
        {
            return new AbilityCatalogV2RuntimeLoadoutCheck(
                essence.Id,
                abilityIds,
                IsReady: false,
                Outcome: null,
                Duration: 0,
                EventCount: 0,
                Failure: $"No v2 abilities are owned by essence '{essence.Id}'.");
        }

        try
        {
            var compiledAbilities = AbilityCompilerV2.CompileAbilities(abilityIds.Select(id => catalog.AbilitiesById[id]));
            var compiledCatalogAbilities = AbilityCompilerV2.CompileAbilities(catalog.Abilities);
            var compiledStatuses = AbilityCompilerV2.CompileStatuses(catalog.Statuses);
            var compiledSummons = AbilityCompilerV2.CompileSummons(catalog.Summons);
            var friendly = CreateRuntimeCombatant($"readiness-{essence.Id}", CombatTeamV2.Friendly, compiledAbilities.Values);
            var hostile = CreateRuntimeCombatant($"readiness-target-{essence.Id}", CombatTeamV2.Hostile, []);
            var engine = new FastCombatEngineV2(
                compiledStatuses,
                compiledSummons,
                compiledCatalogAbilities,
                new FastCombatEngineV2Options(MaxTicks: 5, BasicAttackIntervalTicks: 1000, RandomSeed: 17));
            var result = engine.Run([friendly], [hostile]);

            return new AbilityCatalogV2RuntimeLoadoutCheck(
                essence.Id,
                abilityIds,
                IsReady: true,
                result.Outcome.ToString(),
                result.Duration,
                result.EventLog.Count,
                Failure: null);
        }
        catch (Exception ex)
        {
            return new AbilityCatalogV2RuntimeLoadoutCheck(
                essence.Id,
                abilityIds,
                IsReady: false,
                Outcome: null,
                Duration: 0,
                EventCount: 0,
                Failure: ex.Message);
        }
    }

    private static RuntimeCombatantV2 CreateRuntimeCombatant(
        string id,
        CombatTeamV2 team,
        IEnumerable<CompiledAbilityV2> abilities) =>
        new(
            id,
            id,
            team,
            new Dictionary<AttributeType, float>
            {
                [AttributeType.MaxHealth] = 500,
                [AttributeType.Power] = 50,
                [AttributeType.Spirit] = 50,
                [AttributeType.CritDamage] = 100,
                [AttributeType.DodgeChance] = 0
            },
            abilities,
            ["Role.Readiness"]);

    private static void AddSlot(
        EssenceDefinition essence,
        string slot,
        string legacyAbilityId,
        AbilitySpecKind expectedKind,
        AbilityCatalogV2 catalog,
        IReadOnlyDictionary<(string Owner, AbilitySpecKind Kind), List<AbilitySpec>> abilitiesByOwnerAndKind,
        ICollection<AbilityCatalogV2SlotCoverage> slots,
        ICollection<AbilityCatalogV2CoverageGap> gaps)
    {
        var owned = abilitiesByOwnerAndKind.GetValueOrDefault((essence.Id, expectedKind)) ?? [];
        var currentReference = !string.IsNullOrWhiteSpace(legacyAbilityId)
            && catalog.AbilitiesById.TryGetValue(legacyAbilityId, out var current)
                ? current
                : null;
        var selected = owned.Count == 1 ? owned[0] : currentReference;
        var kindMatches = selected?.Kind == expectedKind;

        slots.Add(new AbilityCatalogV2SlotCoverage(
            essence.Id,
            slot,
            legacyAbilityId,
            selected?.Id,
            owned.Count > 0,
            currentReference is not null,
            kindMatches));

        if (owned.Count == 0)
        {
            gaps.Add(new AbilityCatalogV2CoverageGap(
                essence.Id,
                slot,
                legacyAbilityId,
                $"No v2 {expectedKind} ability is owned by essence '{essence.Id}'."));
            return;
        }

        if (owned.Count > 1)
        {
            gaps.Add(new AbilityCatalogV2CoverageGap(
                essence.Id,
                slot,
                legacyAbilityId,
                $"Multiple v2 {expectedKind} abilities are owned by essence '{essence.Id}': {string.Join(", ", owned.Select(x => x.Id))}."));
        }

        foreach (var ability in owned.Where(x => x.Kind != expectedKind))
        {
            gaps.Add(new AbilityCatalogV2CoverageGap(
                essence.Id,
                slot,
                legacyAbilityId,
                $"V2 ability '{ability.Id}' has kind '{ability.Kind}' but slot expects '{expectedKind}'."));
        }
    }
}
