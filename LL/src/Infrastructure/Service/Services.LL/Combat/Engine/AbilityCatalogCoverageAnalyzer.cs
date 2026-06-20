using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Essences.Definitions;

namespace Services.LL.Combat.Engine;

public sealed class AbilityCatalogCoverageAnalyzer : IAbilityCatalogCoverageAnalyzer
{
    private readonly IEssenceDefinitionRepository _essenceDefinitions;
    private readonly IAbilityCatalogProvider _catalogProvider;

    public AbilityCatalogCoverageAnalyzer(
        IEssenceDefinitionRepository essenceDefinitions,
        IAbilityCatalogProvider catalogProvider)
    {
        _essenceDefinitions = essenceDefinitions;
        _catalogProvider = catalogProvider;
    }

    public AbilityCatalogCoverageReport Analyze()
    {
        var essences = _essenceDefinitions.GetAll();
        var catalog = _catalogProvider.GetCatalog();
        var abilitiesByOwnerAndKind = catalog.Abilities
            .Where(x => !string.IsNullOrWhiteSpace(x.OwningEssenceId))
            .GroupBy(x => (Owner: x.OwningEssenceId!, x.Kind))
            .ToDictionary(x => x.Key, x => x.ToList());
        var slots = new List<AbilityCatalogSlotCoverage>();
        var gaps = new List<AbilityCatalogCoverageGap>();

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

        return new AbilityCatalogCoverageReport(
            essences.Count,
            slots.Count,
            slots.Count(x => x.HasOwnedAbility && x.KindMatches),
            slots.Count(x => x.CurrentReferenceExists),
            slots,
            gaps,
            unownedAbilityIds,
            runtimeLoadoutChecks);
    }

    private static AbilityCatalogRuntimeLoadoutCheck CheckRuntimeLoadout(
        EssenceDefinition essence,
        AbilityCatalog catalog)
    {
        var abilityIds = catalog.AbilityIdsByOwningEssence.GetValueOrDefault(essence.Id) ?? [];
        if (abilityIds.Count == 0)
        {
            return new AbilityCatalogRuntimeLoadoutCheck(
                essence.Id,
                abilityIds,
                IsReady: false,
                Outcome: null,
                Duration: 0,
                EventCount: 0,
                Failure: $"No abilities are owned by essence '{essence.Id}'.");
        }

        try
        {
            var compiledAbilities = AbilityCompiler.CompileAbilities(abilityIds.Select(id => catalog.AbilitiesById[id]));
            var compiledCatalogAbilities = AbilityCompiler.CompileAbilities(catalog.Abilities);
            var compiledStatuses = AbilityCompiler.CompileStatuses(catalog.Statuses);
            var compiledSummons = AbilityCompiler.CompileSummons(catalog.Summons);
            var friendly = CreateRuntimeCombatant($"readiness-{essence.Id}", CombatTeam.Friendly, compiledAbilities.Values);
            var hostile = CreateRuntimeCombatant($"readiness-target-{essence.Id}", CombatTeam.Hostile, []);
            var engine = new FastCombatEngine(
                compiledStatuses,
                compiledSummons,
                compiledCatalogAbilities,
                new FastCombatEngineOptions(MaxTicks: 5, BasicAttackIntervalTicks: 1000, RandomSeed: 17));
            var result = engine.Run([friendly], [hostile]);

            return new AbilityCatalogRuntimeLoadoutCheck(
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
            return new AbilityCatalogRuntimeLoadoutCheck(
                essence.Id,
                abilityIds,
                IsReady: false,
                Outcome: null,
                Duration: 0,
                EventCount: 0,
                Failure: ex.Message);
        }
    }

    private static RuntimeCombatant CreateRuntimeCombatant(
        string id,
        CombatTeam team,
        IEnumerable<CompiledAbility> abilities) =>
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
        string referencedAbilityId,
        AbilitySpecKind expectedKind,
        AbilityCatalog catalog,
        IReadOnlyDictionary<(string Owner, AbilitySpecKind Kind), List<AbilitySpec>> abilitiesByOwnerAndKind,
        ICollection<AbilityCatalogSlotCoverage> slots,
        ICollection<AbilityCatalogCoverageGap> gaps)
    {
        var owned = abilitiesByOwnerAndKind.GetValueOrDefault((essence.Id, expectedKind)) ?? [];
        var currentReference = !string.IsNullOrWhiteSpace(referencedAbilityId)
            && catalog.AbilitiesById.TryGetValue(referencedAbilityId, out var current)
                ? current
                : null;
        var selected = owned.Count == 1 ? owned[0] : currentReference;
        var kindMatches = selected?.Kind == expectedKind;

        slots.Add(new AbilityCatalogSlotCoverage(
            essence.Id,
            slot,
            referencedAbilityId,
            selected?.Id,
            owned.Count > 0,
            currentReference is not null,
            kindMatches));

        if (owned.Count == 0)
        {
            gaps.Add(new AbilityCatalogCoverageGap(
                essence.Id,
                slot,
                referencedAbilityId,
                $"No {expectedKind} ability is owned by essence '{essence.Id}'."));
            return;
        }

        if (owned.Count > 1)
        {
            gaps.Add(new AbilityCatalogCoverageGap(
                essence.Id,
                slot,
                referencedAbilityId,
                $"Multiple {expectedKind} abilities are owned by essence '{essence.Id}': {string.Join(", ", owned.Select(x => x.Id))}."));
        }

        foreach (var ability in owned.Where(x => x.Kind != expectedKind))
        {
            gaps.Add(new AbilityCatalogCoverageGap(
                essence.Id,
                slot,
                referencedAbilityId,
                $"Ability '{ability.Id}' has kind '{ability.Kind}' but slot expects '{expectedKind}'."));
        }
    }
}
