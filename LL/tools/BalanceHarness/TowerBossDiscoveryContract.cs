using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Randomness;
using Domain.Models.Essences;
using Domain.Models.Items;
using Services.LL.PowerRatings;
using Services.LL.WorldTower;

namespace BalanceHarness;

public sealed record BossDiscoveryContext(string Id, IReadOnlyList<TowerPartyRecipe> CharacterTemplates);
public sealed record BossDiscoveryEssence(string Id, string Family);
public sealed record BossDiscoveryGeneration(IReadOnlyList<string> Methods, IReadOnlyList<int> Seeds,
    int CandidatesPerArm, int MaximumAttemptsPerArm, int FreshEvery, string Objective,
    string PolicyVersion = TowerBossGeneration.Version);
public sealed record BossDiscoverySchedule(IReadOnlyList<int> Discovery, IReadOnlyList<int> Selection,
    IReadOnlyList<int> Confirmation, IReadOnlyList<int> Diagnostics,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<int>? Feedback = null);
public sealed record BossDiscoveryStages(int Shortlist, int GeneratedFinalists, int DiagnosticCandidates, int ReplayReserve,
    IReadOnlyDictionary<string, BossDiscoverySchedule> Schedules, string SelectionPolicyVersion = TowerBossStudyPolicy.Version);
public sealed record BossBenchmarkReference(string Id, string Context, TowerScenario Scenario, string Source, string EvidenceHash);
public sealed record BossDiscoveryStart(string Id, string ReferenceId, PartyChoice Party);
public sealed record BossDiscoveryProvenance(string Id, int GenerationSeed, string Method, string Operator,
    IReadOnlyList<string> ParentIds, IReadOnlyList<string> ReferenceIds);
public sealed record TowerBossDiscoveryDefinition(int SchemaVersion, string Id, string Mode, TowerSearchBudget Budget,
    int RequiredPartySize, DateTimeOffset StartsAt, IReadOnlyList<BossDiscoveryContext> Contexts,
    IReadOnlyList<BossDiscoveryEssence> AllowedEssences, IReadOnlyDictionary<string, int>? OwnedCopies,
    BossDiscoveryGeneration Generation, BossDiscoveryStages Stages, IReadOnlyList<int> ExcludedCombatSeeds,
    IReadOnlyList<BossBenchmarkReference> References, IReadOnlyList<BossDiscoveryStart> Starts,
    IReadOnlyDictionary<string, string> ContentHashes, string SettingsHash, string ExecutionHash, int MaximumBattles,
    string BudgetPurpose = "intended-progression",
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PrimaryReferenceId = null);
public sealed record BossDiscoveryCost(int Discovery, int Selection, int GeneratedConfirmation,
    int ReferenceConfirmation, int Diagnostics, int ReplayReserve, int Total);

// This is the entire input boundary for independent candidate generation. References,
// historical results, fixed actor identities and confirmation-only seeds are absent.
public sealed record BossDiscoveryCharacterBudget(int PartySlot, IReadOnlyList<EquipmentReferenceEquipmentSelection> Equipment,
    double AttributeRollMultiplier);
public sealed record BossDiscoveryInputs(int Floor, TowerSearchBudget Budget, int RequiredPartySize,
    IReadOnlyList<BossDiscoveryEssence> AllowedEssences, IReadOnlyDictionary<string, int>? OwnedCopies,
    BossDiscoveryGeneration Generation, IReadOnlyDictionary<string, IReadOnlyList<int>> DiscoverySeeds,
    IReadOnlyDictionary<string, IReadOnlyList<BossDiscoveryCharacterBudget>> EquipmentContexts,
    IReadOnlyDictionary<string, string> ContentHashes, int ShortlistCandidates = 16,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, IReadOnlyList<int>>? FeedbackSeeds = null);

/// <summary>Schema 3 is a new contract; schemas 1/2 retain their original readers and execution.</summary>
public static class TowerBossDiscovery
{
    public const string Version = "tower-boss-discovery-v3";
    public const string Objective = "target-victories-worst-context-v1";
    public const string Independent = "independent";
    public const string Improve = "improve-supplied";
    public static readonly string[] Methods = ["random", "constructive-joint"];

    public static TowerBossDiscoveryDefinition Read(string path) => TowerContractJson.Read<TowerBossDiscoveryDefinition>(path);

    public static TowerBossDiscoveryDefinition Create(string root, string id, TowerSearchBudget budget,
        DateTimeOffset startsAt, IReadOnlyList<BossDiscoveryContext> contexts, int seed, IReadOnlyList<int> excludedSeeds,
        IReadOnlyList<BossBenchmarkReference>? references = null, string budgetPurpose = "intended-progression")
    {
        var settings = TowerBundle.ReadSettings(root);
        var content = new OfflineContent(root, settings.Threat);
        var floor = new JsonWorldTowerDefinitionProvider(Path.Combine(root, "Data", TowerBattleRunner.FloorFile), HarnessJson.Options)
            .GetFloor(budget.PriorityFloor) ?? throw new InvalidDataException("Unknown or unreleased target floor.");
        var excluded = excludedSeeds.Distinct().Order().ToArray();
        var used = excluded.ToHashSet();
        int[] Seeds(string name, int count)
        {
            var result = new List<int>();
            for (var attempt = 0; result.Count < count; attempt++)
            {
                if (attempt == 100000) throw new InvalidDataException("Unable to allocate a fresh bounded schedule.");
                var value = StableRandom.Seed(Version, seed.ToString(CultureInfo.InvariantCulture), name,
                    attempt.ToString(CultureInfo.InvariantCulture));
                if (used.Add(value)) result.Add(value);
            }
            return result.ToArray();
        }
        // All schedules are allocated before inspecting references or their cost.
        var generation = Seeds("generation", 3);
        var schedules = contexts.OrderBy(c => c.Id, StringComparer.Ordinal).ToDictionary(c => c.Id,
            c => new BossDiscoverySchedule(Seeds(c.Id + "/discovery", 8), Seeds(c.Id + "/selection", 64),
                Seeds(c.Id + "/confirmation", 1000), Seeds(c.Id + "/diagnostics", 32)), StringComparer.Ordinal);
        var definition = new TowerBossDiscoveryDefinition(3, id, Independent, budget, floor.RequiredSlots, startsAt, contexts,
            content.Essences.GetAll().OrderBy(e => e.Id, StringComparer.Ordinal).Select(e => new BossDiscoveryEssence(e.Id, e.SourceMonsterId)).ToArray(),
            null, new(Methods, generation, 128, 1024, 4, Objective), new(16, 5, 8, 200, schedules), excluded,
            references ?? [], [], TowerBundle.Files.ToDictionary(f => f, f => HarnessJson.FileHash(Path.Combine(root, "Data", f))),
            HarnessJson.Hash(settings), HarnessJson.Hash(ExecutionIdentity.Current()), 100000, budgetPurpose);
        Validate(root, definition);
        return definition;
    }

    public static BossDiscoveryInputs GenerationInputs(TowerBossDiscoveryDefinition d)
    {
        Validate(d);
        if (d.Mode != Independent) throw new InvalidDataException("Independent inputs are only available in independent mode.");
        return CopyGenerationInputs(d);
    }

    // Callers validate the original experiment before projecting its common generator inputs.
    // Never substitute other methods: their fictitious cost can exceed a valid exact budget.
    internal static BossDiscoveryInputs CopyGenerationInputs(TowerBossDiscoveryDefinition d)
    {
        // Detach mutable caller-owned collections at the generator boundary.
        return JsonSerializer.Deserialize<BossDiscoveryInputs>(JsonSerializer.Serialize(new BossDiscoveryInputs(
            d.Budget.PriorityFloor, d.Budget, d.RequiredPartySize, d.AllowedEssences, d.OwnedCopies, d.Generation,
            d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Discovery),
            d.Contexts.ToDictionary(c => c.Id, c => (IReadOnlyList<BossDiscoveryCharacterBudget>)c.CharacterTemplates.Select(p =>
                new BossDiscoveryCharacterBudget(p.PartySlot, p.Build.Equipment, p.Build.AttributeRollMultiplier)).ToArray()),
            d.ContentHashes, d.Stages.Shortlist, d.Generation.PolicyVersion == TowerGenerationFeedback.Version
                ? d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Feedback!) : null), HarnessJson.Options), HarnessJson.Options)!;
    }

    public static string EquipmentBudgetHash(IReadOnlyList<TowerPartyRecipe> party) => HarnessJson.Hash(party.OrderBy(p => p.PartySlot)
        .Select(p => p with { Build = p.Build with { Id = $"slot-{p.PartySlot}", EssenceIds = [], IdentityEssenceIds = null,
            Equipment = p.Build.Equipment.OrderBy(e => e.Slot).ToArray() } }).ToArray());

    // Match production identity normalization while retaining actual ordered Essences.
    // Explicit identity equal to the actual vector and reordered equipment do not create another combat recipe.
    internal static string RecipeHash(IReadOnlyList<TowerPartyRecipe> party) => HarnessJson.Hash(party.OrderBy(p => p.PartySlot)
        .Select(p => p with { Build = p.Build with { Equipment = p.Build.Equipment.OrderBy(e => e.Slot).ToArray(),
            IdentityEssenceIds = p.Build.IdentityEssenceIds ?? p.Build.EssenceIds } }).ToArray());

    public static TowerScenario Scenario(TowerBossDiscoveryDefinition d, string context, PartyChoice party, IReadOnlyList<int> seeds)
    {
        ValidateParty(d, party);
        if (TowerCompositionSearch.IsCompositionOnly(d.Generation.PolicyVersion)
            && party.Builds.Values.Any(ids => !TowerCompositionSearch.IsCanonical(ids)))
            throw new InvalidDataException("Composition-only scenarios require the fixed ordinal Essence order.");
        var templates = d.Contexts.Single(c => c.Id == context).CharacterTemplates;
        return new(1, d.Id, d.Budget.PriorityFloor, d.StartsAt, "uncleared-no-contributions",
            ["Fixed complete party, equipment and identity; level-1 unascended/unevolved Essences; no styles or contributions."], seeds,
            templates.Select(p => p with { Build = p.Build with { EssenceIds = party.Builds[p.PartySlot],
                IdentityEssenceIds = Enumerable.Range(1, d.Budget.EssenceSlots).Select(i => $"neutral-identity-slot-{i}").ToArray() } }).ToArray());
    }

    public static BossDiscoveryCost Validate(TowerBossDiscoveryDefinition d) => ValidateCore(d, 0);

    // Diagnostic-only phase validation. Ordinary definitions still require all schedules.
    internal static BossDiscoveryCost ValidateDiagnosticPhase(TowerBossDiscoveryDefinition d, bool template)
    {
        if (d.Generation.PolicyVersion != TowerSuppliedCompositionSearch.IncumbentVersion)
            throw new InvalidDataException("Diagnostic phases require the unchanged incumbent policy.");
        return ValidateCore(d, template ? 1 : 2);
    }

    private static BossDiscoveryCost ValidateCore(TowerBossDiscoveryDefinition d, int diagnosticPhase)
    {
        if (d is null || d.SchemaVersion != 3 || !TowerBenchmark.SafeId(d.Id) || d.Mode is not (Independent or Improve)
            || !LegalBudget(d.Budget) || !LegalPurpose(d.Budget, d.BudgetPurpose) || d.RequiredPartySize is < 1 or > 50
            || d.Contexts is not { Count: > 0 and <= 4 } || d.Contexts.Any(c => c is null || !TowerBenchmark.SafeId(c.Id))
            || d.Contexts.Select(c => c.Id).Distinct().Count() != d.Contexts.Count
            || d.AllowedEssences is not { Count: >= 4 and <= 1000 }
            || d.AllowedEssences.Any(e => e is null || string.IsNullOrWhiteSpace(e.Id) || string.IsNullOrWhiteSpace(e.Family))
            || d.AllowedEssences.Select(e => e.Id).Distinct().Count() != d.AllowedEssences.Count
            || d.AllowedEssences.Select(e => e.Family).Distinct(StringComparer.OrdinalIgnoreCase).Count() < d.Budget.EssenceSlots
            || d.ExcludedCombatSeeds is null || d.ExcludedCombatSeeds.Count > TowerStudyLimits.HistoricalSeeds
            || d.ExcludedCombatSeeds.Distinct().Count() != d.ExcludedCombatSeeds.Count
            || d.References is null || d.References.Count > (d.Generation?.PolicyVersion == TowerSearchPortfolio.Version ? 112 : 96) || d.Starts is null || d.Starts.Count > 64
            || (d.MaximumBattles < 1 || d.MaximumBattles > (d.Generation?.PolicyVersion == TowerDeepChallenger.Version ? TowerDeepChallenger.MaximumFights : d.Generation?.PolicyVersion == TowerSearchPortfolio.Version ? TowerSearchPortfolio.MaximumFights : d.Generation?.PolicyVersion == TowerLateAllocation.Version ? TowerLateAllocation.MaximumFights : d.Generation?.PolicyVersion == TowerSearchAllocation.Version ? TowerSearchAllocation.MaximumFights : 100000)) || !TowerContractJson.Hash(d.SettingsHash) || !TowerContractJson.Hash(d.ExecutionHash)
            || d.ContentHashes is null || !d.ContentHashes.Keys.Order().SequenceEqual(TowerBundle.Files.Order())
            || d.ContentHashes.Values.Any(h => !TowerContractJson.Hash(h)))
            throw new InvalidDataException("Invalid independent boss-search scope, pool, budget or frozen content.");
        var pool = d.AllowedEssences.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        if (d.OwnedCopies is not null && (d.OwnedCopies.Keys.Except(pool).Any() || d.OwnedCopies.Values.Any(n => n is < 0 or > 50)))
            throw new InvalidDataException("Owned-copy limits must use eligible Essences and nonnegative bounded counts; omitted IDs have zero copies.");
        if (d.OwnedCopies is not null)
        {
            var usable = d.AllowedEssences.GroupBy(e => e.Family, StringComparer.OrdinalIgnoreCase)
                .Sum(g => Math.Min(d.RequiredPartySize, g.Sum(e => d.OwnedCopies.GetValueOrDefault(e.Id))));
            if (usable < d.RequiredPartySize * d.Budget.EssenceSlots)
                throw new InvalidDataException("Owned copies cannot fill the complete party under source-family limits.");
        }
        foreach (var context in d.Contexts)
        {
            ValidateEquipment(context.CharacterTemplates, d.Budget, d.RequiredPartySize, neutral: true);
            if (context.CharacterTemplates.Any(p => p.Build.Id != $"tower-discovery-character-{p.PartySlot}"))
                throw new InvalidDataException("Use neutral, stable character IDs independent of imported references.");
        }
        if (d.Contexts.Select(c => EquipmentBudgetHash(c.CharacterTemplates)).Distinct().Count() != d.Contexts.Count)
            throw new InvalidDataException("Identical effective equipment contexts must be declared once.");
        var g = d.Generation;
        var s = d.Stages;
        var suppliedComposition = TowerSuppliedCompositionSearch.IsSupported(g?.PolicyVersion);
        var improvement = d.Mode == Improve && (g?.PolicyVersion == TowerBossImprovement.Version || suppliedComposition);
        if (g is null || g.Methods is null || (improvement
                ? suppliedComposition ? !TowerSuppliedCompositionSearch.ValidGeneration(g) : !g.Methods.SequenceEqual(TowerBossImprovement.Methods)
                : !TowerBossGeneration.LegalPolicy(g))
            || g.PolicyVersion is TowerBossGeneration.CoordinatedVersion or TowerBossGeneration.MechanicsVersion or TowerBossGeneration.CoverageVersion or TowerCompositionSearch.Version or TowerJoinedMechanics.Version or TowerGroupCountSearch.Version or TowerGroupVariationSearch.Version or TowerGroupDiversitySearch.Version or TowerGroupCompletionSearch.Version or TowerGroupAllocationSearch.Version or TowerJointStructuralSearch.Version or TowerJointStructuralDiversity.Version or TowerTeamCoverageSearch.Version or TowerFillerDiversitySearch.Version or TowerCorePortfolioSearch.Version or TowerDiscoveryRefinementSearch.Version or TowerDiscoveryRefinementSearch.RoleSafeVersion or TowerDiscoveryRefinementSearch.NovelVersion or TowerDiscoveryRefinementSearch.LocalVersion or TowerDiscoveryRefinementSearch.FreshFirstVersion && d.Mode != Independent || g.Seeds is null || (diagnosticPhase == 1 ? g.Seeds.Count != 0 : g.Seeds.Count is < 1 or > 4)
            || g.Seeds.Distinct().Count() != g.Seeds.Count || (g.CandidatesPerArm < 1 || g.CandidatesPerArm > (g.PolicyVersion is TowerSearchPortfolio.Version or TowerDeepChallenger.Version ? TowerSearchPortfolio.Candidates : 1000))
            || g.MaximumAttemptsPerArm < g.CandidatesPerArm || g.MaximumAttemptsPerArm > (g.PolicyVersion is TowerSearchPortfolio.Version or TowerDeepChallenger.Version ? TowerSearchPortfolio.Attempts : 10000)
            || g.FreshEvery != 4 || g.Objective != Objective || s is null
            || s.Shortlist is < 1 or > 64 || s.Shortlist < g.Methods.Count * g.Seeds.Count
            || diagnosticPhase != 1 && s.Shortlist > TowerBossGeneration.CandidateTotal(g)
            || s.GeneratedFinalists is < 1 or > 5 || s.GeneratedFinalists > s.Shortlist
            || s.SelectionPolicyVersion is not (TowerBossStudyPolicy.Version or TowerBossStudyPolicy.ZeroWinVersion)
            || s.DiagnosticCandidates is < 0 or > 32 || s.ReplayReserve is < 0 or > 1000 || s.Schedules is null
            || !s.Schedules.Keys.Order().SequenceEqual(d.Contexts.Select(c => c.Id).Order()))
            throw new InvalidDataException("Invalid three-stage discovery allocation, objective, methods or bounded attempts.");
        TowerJointStructuralSearch.ValidateBounds(g.PolicyVersion, d.RequiredPartySize, d.Budget.EssenceSlots, d.AllowedEssences.Count);
        if (g.PolicyVersion is (TowerBossGeneration.DepthBehaviorVersion or TowerBossGeneration.LoadoutCompositionVersion or TowerGenerationFeedback.Version or TowerLoadoutRetention.Version or TowerPartyLineages.Version or TowerSearchAllocation.Version or TowerLateAllocation.Version or TowerSearchPortfolio.Version or TowerDeepChallenger.Version) && d.Mode != Independent)
            throw new InvalidDataException("Depth/behavior comparison requires independent generation.");
        var seeds = new List<int>();
        foreach (var schedule in s.Schedules.Values)
        {
            if (schedule is null || !Schedule(schedule.Discovery, diagnosticPhase == 1 ? 0 : 1, diagnosticPhase == 1 ? 0 : 100)
                || !Schedule(schedule.Selection, diagnosticPhase == 1 ? 0 : 1, diagnosticPhase == 1 ? 0 : 1000)
                || !Schedule(schedule.Confirmation, diagnosticPhase == 0 ? 1 : 0, diagnosticPhase == 0 ? 1000 : 0)
                || !Schedule(schedule.Diagnostics, s.DiagnosticCandidates == 0 ? 0 : 1, 1000))
                throw new InvalidDataException("Every context requires explicit bounded paired schedules.");
            seeds.AddRange(schedule.Discovery.Concat(schedule.Selection).Concat(schedule.Confirmation).Concat(schedule.Diagnostics));
            if (g.PolicyVersion == TowerGenerationFeedback.Version)
            {
                if (schedule.Discovery.Count != 8 || !Schedule(schedule.Feedback!, 32, 32))
                    throw new InvalidDataException("Generation feedback requires eight discovery and 32 separate feedback seeds.");
                seeds.AddRange(schedule.Feedback!);
            }
            else if (schedule.Feedback is not null) throw new InvalidDataException("Feedback seeds require the explicit feedback policy.");
        }
        if (s.Schedules.Values.Select(v => (v.Discovery.Count, v.Selection.Count, v.Confirmation.Count, v.Diagnostics.Count)).Distinct().Count() != 1
            || seeds.Count != seeds.Distinct().Count() || seeds.Intersect(d.ExcludedCombatSeeds).Any())
            throw new InvalidDataException("Use equal context samples and disjoint fresh combat stages; generation restarts share schedules.");
        var referenceIds = new HashSet<string>(StringComparer.Ordinal);
        var recipes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in d.References)
        {
            if (reference is null || !TowerBenchmark.SafeId(reference.Id) || !referenceIds.Add(reference.Id)
                || string.IsNullOrWhiteSpace(reference.Source) || !TowerContractJson.Hash(reference.EvidenceHash)
                || !d.Contexts.Any(c => c.Id == reference.Context) || reference.Scenario is null
                || reference.Scenario.FloorNumber != d.Budget.PriorityFloor || reference.Scenario.StartsAt != d.StartsAt
                || reference.Scenario.SchemaVersion != 1 || reference.Scenario.PreparationState != "uncleared-no-contributions"
                || reference.Scenario.Seeds is not { Count: 0 })
                throw new InvalidDataException("References require explicit target scope, provenance and seed-free recipes.");
            ValidateEquipment(reference.Scenario.Party, d.Budget, d.RequiredPartySize);
            if (EquipmentBudgetHash(reference.Scenario.Party) != EquipmentBudgetHash(d.Contexts.Single(c => c.Id == reference.Context).CharacterTemplates))
                throw new InvalidDataException("Reference gear must match its declared context budget.");
            ValidateParty(d, TowerPartySelection.Choice("reference", reference.Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds)));
            if (!recipes.Add(HarnessJson.Hash(new { reference.Context, Recipe = RecipeHash(reference.Scenario.Party) })))
                throw new InvalidDataException("Deduplicate exact reference recipes without dropping distinct identities or gear.");
        }
        if ((d.Mode == Independent && d.Starts.Count != 0) || (d.Mode == Improve && d.Starts.Count == 0)
            || d.Starts.Any(x => x is null || !TowerBenchmark.SafeId(x.Id)) || d.Starts.Select(x => x.Id).Distinct().Count() != d.Starts.Count)
            throw new InvalidDataException("Independent discovery has no supplied starts; improve mode requires explicitly labeled starts.");
        foreach (var start in d.Starts)
        {
            ValidateParty(d, start.Party);
            var reference = d.References.SingleOrDefault(r => r.Id == start.ReferenceId);
            if (reference is null || HarnessJson.Hash(reference.Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds)) != start.Party.Id)
                throw new InvalidDataException("Each supplied start must name the exact reference it descends from.");
            if (improvement && RecipeHash(reference.Scenario.Party) != RecipeHash(Scenario(d, reference.Context, start.Party, []).Party))
                throw new InvalidDataException("Improvement starts must match the fixed character identities as well as equipment and ordered Essences.");
        }
        if (improvement && (d.Starts.Count > g.CandidatesPerArm || d.Starts.Select(s => s.Party.Id).Distinct().Count() != d.Starts.Count))
            throw new InvalidDataException("Each distinct supplied start must fit inside every arm's candidate budget.");
        if (suppliedComposition && (d.Starts.Count is < 1 or > 2 || d.RequiredPartySize > 15
            || d.Starts.Count + 6 >= g.CandidatesPerArm || d.References.Count != d.Starts.Count
            || d.Starts.Any(start => !start.Party.Builds.Values.All(TowerCompositionSearch.IsCanonical))
            || d.References.Any(r => r.Scenario.Party.Any(p => !TowerCompositionSearch.IsCanonical(p.Build.EssenceIds)))
            || s.SelectionPolicyVersion != TowerBossStudyPolicy.ZeroWinVersion))
            throw new InvalidDataException("Supplied composition requires one or two canonical starts, capacity beyond the shared initial batch and its explicit selection policy.");
        if (TowerSuppliedCompositionSearch.PreservesIncumbents(g.PolicyVersion)
            && (d.Mode != Improve || g.Seeds.Count != (diagnosticPhase == 1 ? 0 : 1) || d.Contexts.Count != 1 || d.Starts.Count != 2 || d.References.Count != 2
                || d.Starts.Select(start => start.ReferenceId).Distinct(StringComparer.Ordinal).Count() != 2
                || s.Shortlist != 4 || s.GeneratedFinalists != 1 || s.DiagnosticCandidates != 0 || s.ReplayReserve != 0))
            throw new InvalidDataException("Incumbent nomination requires two distinct supplied references, one root/context, four nominees, one finalist and no diagnostics or replays.");
        if (g.PolicyVersion == TowerAnchoredNeighborhoodSearch.Version) TowerAnchoredNeighborhoodSearch.Validate(d);
        else if (d.PrimaryReferenceId is not null)
            throw new InvalidDataException("A primary reference designation requires the anchored-neighborhood policy.");
        if (g.PolicyVersion == TowerEvaluationAllocationSearch.Version) TowerEvaluationAllocationSearch.Validate(d);
        var discovery = g.PolicyVersion == TowerEvaluationAllocationSearch.Version ? TowerEvaluationAllocationSearch.MaximumFights(g)
            : checked(TowerBossGeneration.CandidateTotal(g) * s.Schedules.Values.Sum(v => v.Discovery.Count)
            + (g.PolicyVersion == TowerGenerationFeedback.Version ? g.Seeds.Count * 16 * s.Schedules.Values.Sum(v => v.Feedback!.Count) : 0));
        var selection = checked(s.Shortlist * s.Schedules.Values.Sum(v => v.Selection.Count));
        var generated = checked(s.GeneratedFinalists * s.Schedules.Values.Sum(v => v.Confirmation.Count));
        var referencesCost = checked(d.References.Sum(r => s.Schedules[r.Context].Confirmation.Count));
        var diagnostics = checked(s.DiagnosticCandidates * s.Schedules.Values.Sum(v => v.Diagnostics.Count));
        var total = checked(discovery + selection + generated + referencesCost + diagnostics + s.ReplayReserve);
        if (total > d.MaximumBattles) throw new InvalidDataException($"Planned {total} combats exceed cap {d.MaximumBattles}.");
        return new(discovery, selection, generated, referencesCost, diagnostics, s.ReplayReserve, total);
    }

    public static BossDiscoveryCost Validate(string root, TowerBossDiscoveryDefinition d)
    {
        var cost = Validate(d);
        ValidateContent(root, d, false);
        return cost;
    }

    internal static void ValidateDiagnosticContent(string root, TowerBossDiscoveryDefinition d, bool template)
    {
        ValidateDiagnosticPhase(d, template);
        ValidateContent(root, d, true);
    }

    private static void ValidateContent(string root, TowerBossDiscoveryDefinition d, bool diagnostic)
    {
        var settings = TowerBundle.ReadSettings(root);
        if (HarnessJson.Hash(settings) != d.SettingsHash || HarnessJson.Hash(ExecutionIdentity.Current()) != d.ExecutionHash
            || d.ContentHashes.Any(p => HarnessJson.FileHash(Path.Combine(root, "Data", p.Key)) != p.Value))
            throw new InvalidDataException("Discovery content, settings or executable differ from the frozen contract.");
        var content = new OfflineContent(root, settings.Threat);
        var actual = content.Essences.GetAll().ToDictionary(e => e.Id, e => e.SourceMonsterId, StringComparer.Ordinal);
        if (d.AllowedEssences.Any(e => !actual.TryGetValue(e.Id, out var family) || !StringComparer.OrdinalIgnoreCase.Equals(family, e.Family)))
            throw new InvalidDataException("Eligible Essences or families differ from production content.");
        var floor = new JsonWorldTowerDefinitionProvider(Path.Combine(root, "Data", TowerBattleRunner.FloorFile), HarnessJson.Options).GetFloor(d.Budget.PriorityFloor);
        if (floor is null || floor.RequiredSlots != d.RequiredPartySize) throw new InvalidDataException("The entire production RequiredSlots party must be mutable.");
        // Validate actual equipment through production materialization, without combat or proposing a search seed team.
        foreach (var context in d.Contexts)
        foreach (var template in context.CharacterTemplates) content.CreateBuild(template.Build);
        var runner = new TowerBattleRunner(root, content);
        foreach (var reference in d.References)
        {
            if (diagnostic && d.Stages.Schedules[reference.Context].Discovery.Count == 0)
            {
                foreach (var member in reference.Scenario.Party) content.CreateBuild(member.Build);
            }
            else
            {
                var seed = (diagnostic ? d.Stages.Schedules[reference.Context].Discovery : d.Stages.Schedules[reference.Context].Confirmation)[0];
                var recipe = reference.Scenario with { Seeds = [seed] };
                runner.CreateInput(recipe, seed, settings.Threat, settings.CheckpointIntervalTicks);
            }
        }
    }

    public static void ValidateParty(TowerBossDiscoveryDefinition d, PartyChoice party)
    {
        var families = d.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        if (party is null || party.Builds is null || party.Id != HarnessJson.Hash(party.Builds)
            || !party.Builds.Keys.Order().SequenceEqual(Enumerable.Range(1, d.RequiredPartySize))
            || party.Builds.Values.Any(ids => ids is null || ids.Count != d.Budget.EssenceSlots
                || ids.Any(id => id is null || !families.ContainsKey(id))
                || ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count)
            || (d.OwnedCopies is not null && party.Builds.Values.SelectMany(ids => ids).GroupBy(id => id)
                .Any(g => g.Count() > d.OwnedCopies.GetValueOrDefault(g.Key))))
            throw new InvalidDataException("Invalid complete ordered party, family legality or owned-copy budget.");
    }

    public static void ValidateProvenance(TowerBossDiscoveryDefinition d, IReadOnlyList<BossDiscoveryProvenance> proposals)
        => ValidateProvenanceCore(d, proposals, false);

    internal static void ValidateProvenanceCore(TowerBossDiscoveryDefinition d, IReadOnlyList<BossDiscoveryProvenance> proposals, bool diagnostic)
    {
        if (diagnostic) ValidateDiagnosticPhase(d, false); else Validate(d);
        ArgumentNullException.ThrowIfNull(proposals);
        var ancestry = d.Starts.ToDictionary(s => s.Id, s => new[] { s.ReferenceId }, StringComparer.Ordinal);
        foreach (var p in proposals)
        {
            if (p is null || !TowerBenchmark.SafeId(p.Id) || ancestry.ContainsKey(p.Id)
                || TowerCompositionSearch.IsCompositionOnly(d.Generation.PolicyVersion) && p.Operator == "order"
                || !d.Generation.Seeds.Contains(p.GenerationSeed) || !d.Generation.Methods.Contains(p.Method)
                || p.ParentIds is null || p.ReferenceIds is null || p.ParentIds.Distinct().Count() != p.ParentIds.Count)
                throw new InvalidDataException("Invalid proposal identity, generation provenance or duplicate parents.");
            var parents = TowerSuppliedCompositionSearch.IsSupported(d.Generation.PolicyVersion)
                ? TowerSuppliedCompositionSearch.ParentCount(p)
                : d.Generation.PolicyVersion is TowerDiscoveryRefinementSearch.Version or TowerDiscoveryRefinementSearch.RoleSafeVersion or TowerDiscoveryRefinementSearch.NovelVersion
                ? TowerDiscoveryRefinementSearch.ParentCount(p)
                : d.Generation.PolicyVersion is TowerDiscoveryRefinementSearch.LocalVersion or TowerDiscoveryRefinementSearch.FreshFirstVersion
                ? TowerDiscoveryRefinementSearch.LocalParentCount(p)
                : d.Generation.PolicyVersion == TowerJointStructuralSearch.Version
                ? p.Operator == TowerJointStructuralSearch.Operator && p.Method == TowerJointStructuralSearch.Method ? 0 : -1
                : d.Generation.PolicyVersion == TowerJointStructuralDiversity.Version
                ? p.Operator == TowerJointStructuralDiversity.Operator && p.Method == TowerJointStructuralDiversity.Method ? 0 : -1
                : d.Generation.PolicyVersion == TowerTeamCoverageSearch.Version
                ? p.Operator == TowerTeamCoverageSearch.Operator && p.Method == TowerTeamCoverageSearch.Method ? 0 : -1
                : d.Generation.PolicyVersion == TowerCorePortfolioSearch.Version
                ? p.Operator == TowerCorePortfolioSearch.Operator && p.Method == TowerCorePortfolioSearch.Method ? 0 : -1
                : d.Generation.PolicyVersion == TowerFillerDiversitySearch.Version
                ? p.Operator == TowerFillerDiversitySearch.Operator && p.Method == TowerFillerDiversitySearch.Method ? 0 : -1
                : d.Generation.PolicyVersion is TowerBossGeneration.DepthBehaviorVersion or TowerBossGeneration.LoadoutCompositionVersion or TowerGenerationFeedback.Version or TowerLoadoutRetention.Version or TowerPartyLineages.Version or TowerSearchAllocation.Version or TowerLateAllocation.Version or TowerSearchPortfolio.Version or TowerDeepChallenger.Version or TowerCompositionSearch.Version or TowerJoinedMechanics.Version or TowerGroupCountSearch.Version or TowerGroupVariationSearch.Version or TowerGroupDiversitySearch.Version or TowerGroupCompletionSearch.Version or TowerGroupAllocationSearch.Version ? p.Operator switch {
                "loadout-distribute" or "loadout-compose" or "loadout-refine" or "loadout-placement"
                    when (d.Generation.PolicyVersion == TowerBossGeneration.LoadoutCompositionVersion && p.Method == "loadout-composition-joint"
                        || d.Generation.PolicyVersion == TowerGenerationFeedback.Version && TowerGenerationFeedback.Methods.Contains(p.Method)
                        || d.Generation.PolicyVersion == TowerLoadoutRetention.Version && TowerLoadoutRetention.Methods.Contains(p.Method)
                        || d.Generation.PolicyVersion == TowerPartyLineages.Version && TowerPartyLineages.Methods.Contains(p.Method)
                        || d.Generation.PolicyVersion is TowerSearchAllocation.Version or TowerLateAllocation.Version or TowerSearchPortfolio.Version or TowerDeepChallenger.Version && TowerSearchAllocation.Methods.Contains(p.Method)
                        || d.Generation.PolicyVersion == TowerSearchPortfolio.Version && TowerSearchPortfolio.Methods.Contains(p.Method)
                        || d.Generation.PolicyVersion == TowerCompositionSearch.Version && p.Method == TowerCompositionSearch.Method
                        || d.Generation.PolicyVersion == TowerJoinedMechanics.Version && p.Method == TowerJoinedMechanics.Method
                        || d.Generation.PolicyVersion == TowerGroupCountSearch.Version && p.Method == TowerGroupCountSearch.Method
                        || d.Generation.PolicyVersion == TowerGroupVariationSearch.Version && p.Method == TowerGroupVariationSearch.Method
                        || d.Generation.PolicyVersion == TowerGroupDiversitySearch.Version && p.Method == TowerGroupDiversitySearch.Method
                        || d.Generation.PolicyVersion == TowerGroupCompletionSearch.Version && p.Method == TowerGroupCompletionSearch.Method
                        || d.Generation.PolicyVersion == TowerGroupAllocationSearch.Version && p.Method == TowerGroupAllocationSearch.Method)
                        && p.ParentIds.Count >= 1 && p.ParentIds.Count <= d.RequiredPartySize + 1 => p.ParentIds.Count,
                "fresh-coverage" => 0,
                "single" or "double" or "order" or "cross-character" or "whole-character"
                    or "mechanic-core" or "coverage-count" or "placement" => 1,
                "recombine" => 2,
                _ => -1
            } : p.Operator switch {
                "fresh-random" or "fresh-constructive" => 0,
                "fresh-coordinated" when d.Mode == Independent && d.Generation.PolicyVersion is TowerBossGeneration.CoordinatedVersion or TowerBossGeneration.MechanicsVersion && p.Method == "coordinated-joint" => 0,
                "broadcast-core" when d.Mode == Independent && (d.Generation.PolicyVersion == TowerBossGeneration.CoordinatedVersion && p.Method == "coordinated-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.MechanicsVersion && p.Method is "coordinated-joint" or "mechanics-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CoverageVersion && p.Method == "mechanics-joint") => 1,
                "fresh-mechanics" when d.Mode == Independent && d.Generation.PolicyVersion is TowerBossGeneration.MechanicsVersion or TowerBossGeneration.CoverageVersion && p.Method == "mechanics-joint" => 0,
                "mechanic-core" when d.Mode == Independent && (d.Generation.PolicyVersion == TowerBossGeneration.MechanicsVersion && p.Method == "mechanics-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CoverageVersion && p.Method is "mechanics-joint" or "coverage-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.ProviderVersion && p.Method is "coverage-joint" or "provider-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CollectiveVersion && p.Method is "provider-joint" or "collective-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CompletionVersion && p.Method is "collective-joint" or "completion-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.DefenseVersion && p.Method is "collective-joint" or "defense-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CompatibleDefenseVersion && p.Method is "defense-joint" or "compatible-defense-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.StaggerReservationVersion && p.Method is "compatible-defense-joint" or "stagger-reservation-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.LoadoutDiversityVersion && p.Method is "stagger-reservation-joint" or "loadout-diversity-joint") => 1,
                "fresh-coverage" when d.Mode == Independent && (d.Generation.PolicyVersion == TowerBossGeneration.CoverageVersion && p.Method == "coverage-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.ProviderVersion && p.Method is "coverage-joint" or "provider-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CollectiveVersion && p.Method is "provider-joint" or "collective-joint"
                    || d.Generation.PolicyVersion is TowerBossGeneration.CompletionVersion or TowerBossGeneration.DefenseVersion && p.Method == "collective-joint") => 0,
                "fresh-completion" when d.Mode == Independent && d.Generation.PolicyVersion == TowerBossGeneration.CompletionVersion && p.Method == "completion-joint" => 0,
                "fresh-defense" when d.Mode == Independent && d.Generation.PolicyVersion is TowerBossGeneration.DefenseVersion or TowerBossGeneration.CompatibleDefenseVersion && p.Method == "defense-joint" => 0,
                "fresh-compatible-defense" when d.Mode == Independent && d.Generation.PolicyVersion is TowerBossGeneration.CompatibleDefenseVersion or TowerBossGeneration.StaggerReservationVersion && p.Method == "compatible-defense-joint" => 0,
                "fresh-stagger-reservation" when d.Mode == Independent && d.Generation.PolicyVersion is TowerBossGeneration.StaggerReservationVersion or TowerBossGeneration.LoadoutDiversityVersion && p.Method == "stagger-reservation-joint" => 0,
                "fresh-loadout-diversity" when d.Mode == Independent && d.Generation.PolicyVersion == TowerBossGeneration.LoadoutDiversityVersion && p.Method == "loadout-diversity-joint" => 0,
                "coverage-count" when d.Mode == Independent && d.Generation.PolicyVersion is TowerBossGeneration.CoverageVersion or TowerBossGeneration.ProviderVersion && p.Method == "coverage-joint" => 1,
                "placement" when d.Mode == Independent && (d.Generation.PolicyVersion == TowerBossGeneration.CoverageVersion && p.Method == "coverage-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.ProviderVersion && p.Method is "coverage-joint" or "provider-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CollectiveVersion && p.Method is "provider-joint" or "collective-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CompletionVersion && p.Method is "collective-joint" or "completion-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.DefenseVersion && p.Method is "collective-joint" or "defense-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CompatibleDefenseVersion && p.Method is "defense-joint" or "compatible-defense-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.StaggerReservationVersion && p.Method is "compatible-defense-joint" or "stagger-reservation-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.LoadoutDiversityVersion && p.Method is "stagger-reservation-joint" or "loadout-diversity-joint") => 1,
                "coverage-provider" when d.Mode == Independent && d.Generation.PolicyVersion is TowerBossGeneration.ProviderVersion or TowerBossGeneration.CollectiveVersion && p.Method == "provider-joint" => 1,
                "collective-provider" when d.Mode == Independent && (d.Generation.PolicyVersion == TowerBossGeneration.CollectiveVersion && p.Method == "collective-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CompletionVersion && p.Method is "collective-joint" or "completion-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.DefenseVersion && p.Method is "collective-joint" or "defense-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.CompatibleDefenseVersion && p.Method is "defense-joint" or "compatible-defense-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.StaggerReservationVersion && p.Method is "compatible-defense-joint" or "stagger-reservation-joint"
                    || d.Generation.PolicyVersion == TowerBossGeneration.LoadoutDiversityVersion && p.Method is "stagger-reservation-joint" or "loadout-diversity-joint") => 1,
                "supplied" when d.Mode == Improve && d.Generation.PolicyVersion == TowerBossImprovement.Version => 1,
                "single" or "double" or "order" or "cross-character" or "whole-character" => 1,
                "recombine" => 2,
                _ => -1
            };
            if (parents < 0 || p.ParentIds.Count != parents || p.ParentIds.Any(id => id is null || !ancestry.ContainsKey(id)))
                throw new InvalidDataException("Proposal operator has missing, future, cyclic or invalid parents.");
            if (TowerSuppliedCompositionSearch.IsSupported(d.Generation.PolicyVersion) &&
                (p.Operator == "supplied" ? !d.Starts.Any(s => s.Id == p.ParentIds.Single())
                : p.ParentIds.Any(id => !proposals.Any(source => source.Id == id && source.Method == p.Method && source.GenerationSeed == p.GenerationSeed))))
                throw new InvalidDataException("Supplied descendants require parents from the same arm, and roots must name declared starts.");
            if (d.Generation.PolicyVersion is TowerBossGeneration.LoadoutCompositionVersion or TowerGenerationFeedback.Version or TowerLoadoutRetention.Version or TowerPartyLineages.Version or TowerSearchAllocation.Version or TowerLateAllocation.Version or TowerSearchPortfolio.Version or TowerDeepChallenger.Version or TowerCompositionSearch.Version or TowerJoinedMechanics.Version or TowerGroupCountSearch.Version or TowerGroupVariationSearch.Version or TowerGroupDiversitySearch.Version or TowerGroupCompletionSearch.Version or TowerGroupAllocationSearch.Version or TowerDiscoveryRefinementSearch.Version or TowerDiscoveryRefinementSearch.RoleSafeVersion or TowerDiscoveryRefinementSearch.NovelVersion or TowerDiscoveryRefinementSearch.LocalVersion or TowerDiscoveryRefinementSearch.FreshFirstVersion && p.ParentIds.Any(id =>
                !proposals.Any(source => source.Id == id && source.Method == p.Method && source.GenerationSeed == p.GenerationSeed)))
                throw new InvalidDataException("Loadout-composition parents must belong to the same generated arm.");
            var inherited = p.ParentIds.SelectMany(id => ancestry[id]).Distinct().Order(StringComparer.Ordinal).ToArray();
            if (!p.ReferenceIds.SequenceEqual(inherited) || (d.Mode == Independent && inherited.Length != 0))
                throw new InvalidDataException("Reference ancestry must propagate through mutations and recombination without relabeling.");
            ancestry.Add(p.Id, inherited);
        }
    }

    internal static bool LegalBudget(TowerSearchBudget? b) => b is not null && b.PriorityFloor is >= 1 and <= 15
        && b.EssenceSlots is >= 4 and <= 10 && b.CharacterLevel is >= 1 and <= 100
        && b.EssenceSlots <= EssenceSlotProgression.GetUnlockedSlotCount(b.CharacterLevel)
        && b.Tier is >= 1 and <= 2 && b.Rank is >= 0 and <= 4 && Enum.IsDefined(b.Quality);

    internal static bool LegalPurpose(TowerSearchBudget budget, string purpose) => purpose == "diagnostic"
        || purpose == "intended-progression" && (budget.PriorityFloor switch {
            1 => budget.EssenceSlots == 4,
            5 => budget.EssenceSlots == 5,
            10 => budget.EssenceSlots == 6,
            11 => budget.EssenceSlots >= 7,
            _ => true // Intermediate checkpoints remain explicitly declared, not inferred here.
        });

    internal static void ValidateEquipment(IReadOnlyList<TowerPartyRecipe>? party, TowerSearchBudget budget, int count, bool neutral = false)
    {
        if (party is null || party.Count != count || party.Any(p => p is null || p.Build is null)
            || !party.Select(p => p.PartySlot).SequenceEqual(Enumerable.Range(1, count))
            || party.Select(p => p.Build.Id).Distinct().Count() != count
            || party.Any(p => string.IsNullOrWhiteSpace(p.Build.Id) || p.Build.CharacterLevel != budget.CharacterLevel
                || p.Build.Tier != budget.Tier || p.Build.Rank != budget.Rank || p.Build.Quality != budget.Quality
                || !double.IsFinite(p.Build.AttributeRollMultiplier) || p.Build.AttributeRollMultiplier <= 0
                || p.Build.EssenceIds is null || p.Build.EssenceIds.Count != (neutral ? 0 : budget.EssenceSlots)
                || (neutral && p.Build.IdentityEssenceIds is not null)
                || (p.Build.IdentityEssenceIds is { } identities && (identities.Count != budget.EssenceSlots || identities.Any(string.IsNullOrWhiteSpace)))
                || p.Build.Equipment is not { Count: > 0 and <= 8 }
                || p.Build.Equipment.Any(e => e is null || !Enum.IsDefined(e.Slot) || string.IsNullOrWhiteSpace(e.DefinitionId) || e.ActiveStyleId is not null || e.UseNativeStyle)
                || p.Build.Equipment.Select(e => e.Slot).Distinct().Count() != p.Build.Equipment.Count))
            throw new InvalidDataException("Freeze complete ordered character equipment/level budgets, disable styles and keep neutral templates free of Essence vectors.");
    }

    private static bool Schedule(IReadOnlyList<int>? values, int minimum, int maximum) => values is not null
        && values.Count >= minimum && values.Count <= maximum && values.Count == values.Distinct().Count();
}

internal static class TowerContractJson
{
    public static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path),
        new JsonSerializerOptions(HarnessJson.Options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectRequiredConstructorParameters = true }) ?? throw new InvalidDataException("Empty Tower contract.");
    public static bool Hash(string? value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
