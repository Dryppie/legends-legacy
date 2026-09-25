using System.Text.Json;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record TowerProposalComparisonPlan(string Version, string ScopeHash, string InventoryHash,
    string BenchmarkPartyId, TowerProposalPolicy Control, TowerProposalPolicy Candidate,
    int Roots, int HeldoutSamples, int SearchFightsPerArm, int RequiredFreshValues, int MaximumFights,
    int MaximumSeconds, long MaximumBytes, string Pairing, string Barrier, string Interpretation,
    TowerProposalComparisonAnalysis Analysis,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerProposalSelectionContrast? SelectionContrast = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerProposalValidationDesign? ValidationProtocol = null);
public sealed record TowerProposalSelectionContrast(string Control, string Candidate);
public sealed record TowerProposalComparisonAnalysis(string PrimaryEndpoint, string GuardrailEndpoint,
    string IdenticalOutputs, string RootRetention, string RootUncertainty, string ConditionalUncertainty,
    double GoMethodAtLeast, double GoBenchmarkAtLeast, int GoDifferingRootsAtLeast,
    int GoPromisingNovelRootsAtLeast, double PromisingNovelGainAtLeast,
    double AbandonMethodAtMost, double AbandonBenchmarkAtMost, int AbandonPromisingNovelRootsBelow,
    string DecisionOrder);
public sealed record TowerProposalComparisonPair(int Root, TowerProposalRacingPlan Control,
    TowerProposalRacingPlan Candidate, IReadOnlyList<int> HeldoutSeeds);
public sealed record TowerProposalComparisonBinding(string Version, string PlanHash, string ValuesHash,
    IReadOnlyList<TowerProposalComparisonPair> Pairs);
public sealed record TowerProposalComparisonSearch(string PlanHash, string PairHash, string Status,
    TowerProposalRacingReport Control, TowerProposalRacingReport? Candidate);

/// <summary>Prospective comparison contract and native paired-search adapter. The
/// owning controller must admit/reserve values, freeze all pairs, own processes and
/// resources, then evaluate held-out outputs. This class never allocates or launches.</summary>
public static partial class TowerProposalComparison
{
    public const string Version = "tower-proposal-affinity-comparison-v1";
    public const string CreationVersion = "tower-affinity-creation-comparison-v1";
    public const string SelectorVersion = "tower-benchmark-tie-comparison-v1";
    internal const string SelectorDecisionOrder = "incomplete-then-abandon-without-novelty-gate-then-no-observed-output-differentiation-then-larger-fresh-evaluation-warranted-else-inconclusive-never-adopt";
    public const int Roots = 12;
    public const int HeldoutSamples = 256;
    public const int ValuesPerSearch = 73; // One root plus 8/8/8/8/40 common combat seeds.
    public const int RequiredFreshValues = Roots * (ValuesPerSearch + HeldoutSamples);
    public const int MaximumFights = Roots * (2 * TowerBatchRacing.PlannedEvaluations + 3 * HeldoutSamples);
    private const string Pairing = "same-proposal-root-and-search-panels-separate-charges";
    private const string Barrier = "freeze-all-24-outputs-before-any-heldout-observation";
    private const string Interpretation = "development-pilot-no-efficacy-or-adoption-claim";

    public static TowerProposalPolicy Control() => TowerProposalPolicies.BenchmarkDamageEdits([])
        with { Name = "benchmark-single-control-v2" };

    private static bool CandidateMatches(string version, TowerProposalPolicy candidate) => version switch {
        Version => candidate.PreservedDamageAffinityIds is { Count: > 0 }
            && HarnessJson.Hash(candidate) == HarnessJson.Hash(TowerProposalPolicies.BenchmarkDamageEdits(candidate.PreservedDamageAffinityIds)),
        CreationVersion or SelectorVersion => candidate.CreatedDamageAffinityIds is { Count: > 0 }
            && HarnessJson.Hash(candidate) == HarnessJson.Hash(TowerProposalPolicies.BenchmarkAffinityCreation(candidate.CreatedDamageAffinityIds)),
        _ => false
    };

    private static TowerProposalComparisonAnalysis Analysis() => new(
        "equal-root-mean-heldout-candidate-minus-control-win-rate",
        "equal-root-mean-heldout-candidate-minus-fixed-benchmark-win-rate",
        "reuse-one-physical-recipe-per-root-heldout-panel-exact-zero-method-contrast-retain-absolute-benchmark-results",
        "all-12-roots-no-preview-screening-no-replacement-no-refill-any-incomplete-root-invalidates-study",
        "report-all-12-paired-root-contrasts-and-descriptive-95-percent-t-df11-not-a-powered-efficacy-test",
        "paired-seed-standard-errors-and-covariance-conditional-on-frozen-outputs-not-future-root-reliability",
        .02, 0, 3, 3, .03, -.02, -.02, 3,
        "incomplete-then-abandon-then-no-observed-output-differentiation-then-larger-fresh-evaluation-warranted-else-inconclusive-never-adopt");

    // Episode labels, proposal roots and accumulated exclusions can change at
    // admission. Physical settings, runtime, references and all other rules cannot.
    internal static string ScopeHash(TowerBossDiscoveryDefinition scope) => HarnessJson.Hash(scope with {
        Id = "proposal-comparison-scope", StartsAt = DateTimeOffset.UnixEpoch,
        Generation = scope.Generation with { Seeds = [] }, ExcludedCombatSeeds = []
    });

    public static TowerProposalComparisonPlan CreatePlan(TowerProposalContext context, TowerProposalPolicy candidate)
    {
        context = TowerBatchRacing.Copy(context); candidate = TowerBatchRacing.Copy(candidate);
        var creation = candidate.Version == TowerProposalPolicies.CreationPolicyVersion;
        TowerProposalPolicies.Validate(new TowerProposalExportRequest(creation ? TowerProposalPolicies.CreationExportVersion : TowerProposalPolicies.DamageExportVersion,
            context, [Control(), candidate]));
        var benchmark = context.Scope.Starts.Single(s => s.ReferenceId == context.BenchmarkReferenceId).Party;
        var selected = TowerProposalPolicies.SelectedAffinities(context, candidate, creation);
        if (selected.Length == 0 || !creation && selected.Any(a => !benchmark.Builds.Values.Any(ids =>
                ids.Contains(a.ProducerEssenceId) && ids.Contains(a.ModifierEssenceId))))
            throw new InvalidDataException("Comparison requires explicit nonempty affinities co-located in its fixed benchmark.");
        if (creation)
        {
            // Deterministic feasibility only: no proposal roots are previewed or screened.
            // Fixed benchmark parentage needs 17 distinct legal edits across both waves.
            var placements = new TowerAffinityCreation(TowerBossDiscovery.CopyGenerationInputs(context.Scope), selected, (_, _) => []);
            var distinct = benchmark.Builds.Keys.SelectMany(owner => placements.Opportunities(benchmark, owner, default)
                .SelectMany(row => row.LegalEdits.Select(edit => HarnessJson.Hash(new { owner, edit.Removed, edit.Added }))))
                .Distinct(StringComparer.Ordinal).Count();
            if (distinct < 17) throw new InvalidDataException("Affinity creation requires at least 17 distinct legal benchmark edits before allocation.");
        }
        var result = new TowerProposalComparisonPlan(creation ? CreationVersion : Version, ScopeHash(context.Scope),
            HarnessJson.Hash(context.DamageAffinityInventory!), benchmark.Id, Control(), candidate,
            Roots, HeldoutSamples, 528, RequiredFreshValues, MaximumFights, 10800, 6L * 1024 * 1024 * 1024,
            Pairing, Barrier, Interpretation, Analysis());
        Validate(result); return result;
    }

    public static TowerProposalComparisonPlan CreateSelectorPlan(TowerProposalContext context, TowerProposalPolicy generator)
    {
        if (generator.Version != TowerProposalPolicies.CreationPolicyVersion)
            throw new InvalidDataException("Selector comparison fixes the existing affinity-creation generator in both arms.");
        var creation = CreatePlan(context, generator);
        var result = creation with { Version = SelectorVersion, Control = TowerBatchRacing.Copy(creation.Candidate),
            SelectionContrast = new(TowerBossStudyPolicy.IncumbentTieVersion, TowerProposalPolicies.BenchmarkTieSelectionVersion),
            Analysis = Analysis() with { GoPromisingNovelRootsAtLeast = 0, AbandonPromisingNovelRootsBelow = 0,
                DecisionOrder = SelectorDecisionOrder } };
        Validate(result); return result;
    }

    internal static TowerProposalComparisonPlan Recreate(TowerProposalComparisonPlan plan, TowerProposalContext context) =>
        plan.Version == NominationVersion ? CreateNominationPlan(context, plan.Candidate)
            : plan.Version == LoadoutPlacementVersion ? CreateLoadoutPlacementPlan(context, plan.Control)
            : plan.Version == AlliedActionVersion ? CreateAlliedActionPlan(context, plan.Candidate)
            : plan.Version == PreservationVersion ? CreatePreservationPlan(context, plan.Candidate)
            : plan.Version == ValidationVersion ? CreateValidationPlan(context, plan.Candidate)
            : plan.Version == SelectorVersion ? CreateSelectorPlan(context, plan.Candidate) : CreatePlan(context, plan.Candidate);

    public static void Validate(TowerProposalComparisonPlan plan)
    {
        if (plan?.Version == NominationVersion) { ValidateNominationPlan(plan); return; }
        if (plan?.Version == LoadoutPlacementVersion) { ValidateLoadoutPlacementPlan(plan); return; }
        if (plan?.Version == AlliedActionVersion) { ValidateAlliedActionPlan(plan); return; }
        if (plan?.Version == PreservationVersion) { ValidatePreservationPlan(plan); return; }
        if (plan?.Version == ValidationVersion) { ValidateValidationPlan(plan); return; }
        var selector = plan?.Version == SelectorVersion;
        var expectedAnalysis = selector ? Analysis() with { GoPromisingNovelRootsAtLeast = 0,
            AbandonPromisingNovelRootsBelow = 0, DecisionOrder = SelectorDecisionOrder } : Analysis();
        if (plan is null || plan.Version is not (Version or CreationVersion or SelectorVersion) || !TowerContractJson.Hash(plan.ScopeHash)
            || !TowerContractJson.Hash(plan.InventoryHash) || !TowerContractJson.Hash(plan.BenchmarkPartyId)
            || plan.Control is null || plan.Candidate is null || plan.ValidationProtocol is not null
            || HarnessJson.Hash(plan.Control) != HarnessJson.Hash(selector ? plan.Candidate : Control())
            || (selector ? plan.SelectionContrast != new TowerProposalSelectionContrast(TowerBossStudyPolicy.IncumbentTieVersion,
                TowerProposalPolicies.BenchmarkTieSelectionVersion) : plan.SelectionContrast is not null)
            || !CandidateMatches(plan.Version, plan.Candidate)
            || plan.Roots != Roots || plan.HeldoutSamples != HeldoutSamples || plan.SearchFightsPerArm != 528
            || plan.RequiredFreshValues != RequiredFreshValues || plan.MaximumFights != MaximumFights
            || plan.MaximumSeconds != 10800 || plan.MaximumBytes != 6L * 1024 * 1024 * 1024
            || plan.Pairing != Pairing || plan.Barrier != Barrier || plan.Interpretation != Interpretation
            || HarnessJson.Hash(plan.Analysis) != HarnessJson.Hash(expectedAnalysis))
            throw new InvalidDataException("Changed prospective affinity comparison contract; declare a new version for a different design.");
        TowerProposalPolicies.Validate(plan.Candidate);
    }

    /// <summary>Deterministically bind an already admitted ordered allocation. No
    /// entropy, reservation, candidate preview or outcome-based screening occurs.</summary>
    public static TowerProposalComparisonBinding Bind(TowerProposalComparisonPlan plan, TowerProposalContext context,
        IReadOnlyList<int> admittedValues)
    {
        plan = TowerBatchRacing.Copy(plan); context = TowerBatchRacing.Copy(context);
        var values = admittedValues.ToArray();
        Validate(plan);
        if (plan.Version == NominationVersion) return BindNomination(plan, context, values);
        if (plan.Version == LoadoutPlacementVersion) return BindLoadoutPlacement(plan, context, values);
        if (plan.Version == AlliedActionVersion) return BindAlliedAction(plan, context, values);
        if (plan.Version == PreservationVersion) return BindPreservation(plan, context, values);
        if (plan.Version == ValidationVersion) return BindValidation(plan, context, values);
        if (HarnessJson.Hash(plan) != HarnessJson.Hash(Recreate(plan, context)))
            throw new InvalidDataException("Comparison differs from its frozen scope, benchmark, content or inventory.");
        var d = context.Scope;
        var historical = d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Append(context.RootSeed)
            .Concat(d.References.SelectMany(r => r.Scenario.Seeds))
            .Concat(d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection)
                .Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? []))).ToHashSet();
        if (values.Length != RequiredFreshValues || values.Distinct().Count() != values.Length || values.Any(historical.Contains))
            throw new InvalidDataException("Supply exactly 3,948 distinct, admitted values disjoint from history, without refill.");
        string[] roles = ["wave-1-screen", "wave-1-continuation", "wave-2-screen", "wave-2-continuation", "selection"];
        var pairs = new List<TowerProposalComparisonPair>();
        for (var i = 0; i < Roots; i++)
        {
            var search = values.Skip(i * ValuesPerSearch).Take(ValuesPerSearch).ToArray();
            var heldout = values.Skip(Roots * ValuesPerSearch + i * HeldoutSamples).Take(HeldoutSamples).ToArray();
            var scope = d with {
                Generation = d.Generation with { Seeds = [search[0]] },
                // Legacy schedules remain declared in the scope and are already
                // forbidden by the racing validator; do not also mark those
                // declarations excluded (the legacy contract rejects that).
                ExcludedCombatSeeds = d.ExcludedCombatSeeds.Concat(values.Except(search)).Distinct().Order().ToArray()
            };
            var racing = new TowerAdaptiveRacingPlan(TowerAdaptiveRacing.Version, scope, context.Mechanics,
                context.BenchmarkReferenceId, search[0], roles.Select((role, p) =>
                    new TowerRacingPanel(role, search.Skip(1 + p * 8).Take(p == 4 ? 40 : 8).ToArray())).ToArray(), 528);
            var pair = new TowerProposalComparisonPair(i + 1,
                new(plan.Version == SelectorVersion ? TowerProposalPolicies.CreationRacingVersion : TowerProposalPolicies.DamageRacingVersion,
                    racing, plan.Control, context.DamageAffinityInventory),
                new(plan.Version == SelectorVersion ? TowerProposalPolicies.BenchmarkTieRacingVersion
                    : plan.Version == CreationVersion ? TowerProposalPolicies.CreationRacingVersion : TowerProposalPolicies.DamageRacingVersion,
                    racing, plan.Candidate, context.DamageAffinityInventory, plan.SelectionContrast?.Candidate), heldout);
            ValidatePair(pair, plan.Version); pairs.Add(pair);
        }
        return new(plan.Version, HarnessJson.Hash(plan), HarnessJson.Hash(values), pairs);
    }

    internal static void ValidatePair(TowerProposalComparisonPair pair, string? comparisonVersion = null)
    {
        if (pair is null || pair.Root is < 1 or > Roots || pair.Control is null || pair.Candidate is null)
            throw new InvalidDataException("Unknown comparison pair.");
        TowerProposalPolicies.Validate(pair.Control); TowerProposalPolicies.Validate(pair.Candidate);
        if (comparisonVersion == NominationVersion || comparisonVersion is null && pair.Candidate.Version == TowerAffinitySearch.GeneratedNominationVersion)
        { ValidateNominationPair(pair); return; }
        if (comparisonVersion == LoadoutPlacementVersion || comparisonVersion is null && pair.Candidate.Version == TowerProposalPolicies.LoadoutPlacementRacingVersion)
        { ValidateLoadoutPlacementPair(pair); return; }
        if (comparisonVersion == AlliedActionVersion || comparisonVersion is null && pair.Candidate.Version == TowerProposalPolicies.AlliedActionValidationRacingVersion)
        { ValidateAlliedActionPair(pair); return; }
        if (comparisonVersion == PreservationVersion || comparisonVersion is null && pair.Candidate.Version == TowerProposalPolicies.PreservingValidationRacingVersion)
        { ValidatePreservationPair(pair); return; }
        if (comparisonVersion == ValidationVersion || comparisonVersion is null && pair.Candidate.Version == TowerProposalPolicies.BenchmarkValidationRacingVersion)
        { ValidateValidationPair(pair); return; }
        var racing = pair.Control.Racing;
        var candidate = pair.Candidate.Policy;
        comparisonVersion ??= pair.Candidate.Version == TowerProposalPolicies.BenchmarkTieRacingVersion ? SelectorVersion
            : candidate.Version == TowerProposalPolicies.CreationPolicyVersion ? CreationVersion : Version;
        var selector = comparisonVersion == SelectorVersion;
        if (comparisonVersion is not (Version or CreationVersion or SelectorVersion)
            || pair.Control.Version != (selector ? TowerProposalPolicies.CreationRacingVersion : TowerProposalPolicies.DamageRacingVersion)
            || pair.Candidate.Version != (selector ? TowerProposalPolicies.BenchmarkTieRacingVersion : candidate.Version == TowerProposalPolicies.CreationPolicyVersion
                ? TowerProposalPolicies.CreationRacingVersion : TowerProposalPolicies.DamageRacingVersion)
            || HarnessJson.Hash(pair.Control.Policy) != HarnessJson.Hash(selector ? candidate : Control())
            || !CandidateMatches(comparisonVersion, candidate)
            || HarnessJson.Hash(racing) != HarnessJson.Hash(pair.Candidate.Racing)
            || HarnessJson.Hash(pair.Control.DamageAffinityInventory) != HarnessJson.Hash(pair.Candidate.DamageAffinityInventory)
            || !racing.Scope.Generation.Seeds.SequenceEqual([racing.RootSeed])
            || pair.HeldoutSeeds is not { Count: HeldoutSamples } || pair.HeldoutSeeds.Distinct().Count() != HeldoutSamples
            || pair.HeldoutSeeds.Intersect(racing.Panels.SelectMany(p => p.Seeds).Append(racing.RootSeed)).Any()
            || pair.HeldoutSeeds.Except(racing.Scope.ExcludedCombatSeeds).Any())
            throw new InvalidDataException("Paired search requires the declared proposal policies, shared search values and isolated held-out seeds.");
    }

    public static async Task<TowerProposalComparisonSearch> RunPairAsync(TowerProposalComparisonPlan design, TowerProposalComparisonPair pair,
        TowerLoadoutArchive control, TowerLoadoutArchive candidate, long maximumEvidenceBytesPerArm,
        Action checkLimits, CancellationToken token = default, Action<bool>? attempt = null,
        TowerProposalEvidenceStorage.Writer? storage = null)
    {
        ArgumentNullException.ThrowIfNull(checkLimits);
        pair = TowerBatchRacing.Copy(pair); ValidatePair(pair, design.Version);
        var first = Path.GetFullPath(control.OutputRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var second = Path.GetFullPath(candidate.OutputRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (first.StartsWith(second, StringComparison.OrdinalIgnoreCase) || second.StartsWith(first, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Comparison arms need separate, non-nested, exclusively owned archives.");
        checkLimits(); token.ThrowIfCancellationRequested();
        return await ExecutePairAsync(design, pair, (arm, plan) => {
            checkLimits(); token.ThrowIfCancellationRequested();
            TowerProposalRacingNative.Preflight(plan, arm == "control" ? control : candidate, maximumEvidenceBytesPerArm, token);
        }, (arm, plan) => TowerProposalRacingNative.RunAsync(plan, arm == "control" ? control : candidate,
            maximumEvidenceBytesPerArm, checkLimits, token, attempt, storage));
    }

    internal static async Task<TowerProposalComparisonSearch> ExecutePairAsync(TowerProposalComparisonPlan design, TowerProposalComparisonPair pair,
        Action<string, TowerProposalRacingPlan> preflight,
        Func<string, TowerProposalRacingPlan, Task<TowerProposalRacingReport>> run)
    {
        design = TowerBatchRacing.Copy(design); pair = TowerBatchRacing.Copy(pair); ValidatePair(pair, design.Version); Validate(design);
        var p = pair.Candidate;
        if (HarnessJson.Hash(design) != HarnessJson.Hash(Recreate(design, new(p.Racing.Scope, p.Racing.Mechanics,
                p.Racing.BenchmarkReferenceId, p.Racing.RootSeed, design.Version == LoadoutPlacementVersion ? pair.Control.DamageAffinityInventory : p.DamageAffinityInventory))))
            throw new InvalidDataException("Native pair differs from the frozen prospective design.");
        // Check both arms before spending any combat budget on the first.
        preflight("control", pair.Control); preflight("candidate", pair.Candidate);
        var a = await run("control", pair.Control);
        var b = a.Evaluation.Status == "Complete"
            ? await run("candidate", pair.Candidate) : null;
        if (design.Version == SelectorVersion && b?.Evaluation.Status == "Complete") ValidateSelectorTrajectories(a, b);
        if (design.Version == NominationVersion && b?.Evaluation.Status == "Complete") ValidateNominationTrajectories(a, b);
        if (design.Version == ValidationVersion && b?.Evaluation.Status == "Complete") ValidateValidationTrajectories(a, b);
        if (design.Version is PreservationVersion or AlliedActionVersion or LoadoutPlacementVersion && b?.Evaluation.Status == "Complete") ValidatePreservationTrajectories(a, b);
        return new(HarnessJson.Hash(design), HarnessJson.Hash(pair), b?.Evaluation.Status == "Complete" ? "Complete" : "Incomplete", a, b);
    }

    internal static void ValidateSelectorTrajectories(TowerProposalRacingReport control, TowerProposalRacingReport candidate)
    {
        // Only evidence bindings and the final choice may differ. Identical physical
        // training requests must agree, even though each arm is separately charged.
        object Training(TowerProposalRacingReport report) => new {
            report.PolicyHash,
            Batches = report.Batches.Select(b => b with { FeedbackPanels = [] }),
            report.Evaluation.Decisions, report.Evaluation.Nominees,
            Panels = report.Evaluation.Panels.Select(p => new {
                Freeze = p.Freeze with { Version = "", PlanHash = "" }, p.Complete, p.Scores, p.Contrasts,
                Observations = p.Observations.Select(o => new {
                    Request = o.Request with { PanelHash = "" }, Outcome = o.Outcome with { RequestHash = "" }
                })
            })
        };
        if (HarnessJson.Hash(Training(control)) != HarnessJson.Hash(Training(candidate)))
            throw new InvalidDataException("Selector comparison changed generation, training evidence, pruning or nomination.");
    }

    public static int Command(string[] args, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (args is ["tower-proposal-comparison-plan" or "tower-benchmark-tie-comparison-plan" or "tower-benchmark-validation-comparison-plan" or "tower-affinity-preservation-comparison-plan" or "tower-affinity-allied-action-comparison-plan" or "tower-loadout-placement-comparison-plan" or "tower-affinity-nomination-comparison-plan", var requestPath, var output])
        {
            var request = TowerContractJson.Read<TowerProposalExportRequest>(requestPath);
            TowerProposalPolicies.Validate(request);
            var placement = args[0] == "tower-loadout-placement-comparison-plan";
            var alliedAction = args[0] == "tower-affinity-allied-action-comparison-plan";
            var preservation = args[0] == "tower-affinity-preservation-comparison-plan";
            var candidates = request.Policies.Where(p => placement ? p.Version == TowerProposalPolicies.AlliedActionPolicyVersion
                : alliedAction ? p.Version == TowerProposalPolicies.AlliedActionPolicyVersion
                : preservation ? p.Version == TowerProposalPolicies.PreservingCreationPolicyVersion
                : request.Version == TowerProposalPolicies.CreationExportVersion
                ? p.Version == TowerProposalPolicies.CreationPolicyVersion
                : p.Version == TowerProposalPolicies.DamagePolicyVersion && p.PreservedDamageAffinityIds is { Count: > 0 }).ToArray();
            if (candidates.Length != 1) throw new InvalidDataException("Supply exactly one candidate of the declared comparison version.");
            var candidate = candidates[0];
            var plan = args[0] == "tower-affinity-nomination-comparison-plan" ? CreateNominationPlan(request.Context, candidate)
                : placement ? CreateLoadoutPlacementPlan(request.Context, candidate)
                : alliedAction ? CreateAlliedActionPlan(request.Context, candidate)
                : preservation ? CreatePreservationPlan(request.Context, candidate)
                : args[0] == "tower-benchmark-validation-comparison-plan" ? CreateValidationPlan(request.Context, candidate)
                : args[0] == "tower-benchmark-tie-comparison-plan" ? CreateSelectorPlan(request.Context, candidate)
                : CreatePlan(request.Context, candidate);
            token.ThrowIfCancellationRequested(); HarnessJson.WriteNew(output, plan);
            Console.WriteLine(JsonSerializer.Serialize(new { status = "PlannedNotAdmitted", planHash = HarnessJson.Hash(plan),
                plan.RequiredFreshValues, plan.MaximumFights, newFights = 0, newReservedValues = 0 }, HarnessJson.Options));
            return 0;
        }
        if (args is ["tower-proposal-comparison-check", var planPath])
        {
            var plan = TowerContractJson.Read<TowerProposalComparisonPlan>(planPath); Validate(plan);
            Console.WriteLine(JsonSerializer.Serialize(new { status = "ValidDesignNotAdmitted", planHash = HarnessJson.Hash(plan),
                newFights = 0, newReservedValues = 0 }, HarnessJson.Options));
            return 0;
        }
        throw new InvalidDataException("Use tower-proposal-comparison-plan <generation-request> <new-plan.json> or -check <plan.json>. No launch or reservation command is provided.");
    }
}
