namespace BalanceHarness;

public static partial class TowerProposalComparison
{
    public const string PreservationVersion = "tower-affinity-preservation-comparison-v1";
    public const int PreservationValuesPerPair = 109; // root + 32 racing + 16 nomination + 60 validation
    public const int PreservationRequiredFreshValues = Roots * (PreservationValuesPerPair + HeldoutSamples);
    private const string PreservationPairing = "same-proposal-root-racing-nomination-and-validation-panels-separate-charges";
    private static TowerProposalValidationDesign PreservationDesign() => new(109, 32, 16, 16, 60,
        "both-arms-share-all-16-nomination-values-in-frozen-order",
        "both-arms-share-60-validation-values-disjoint-from-training-and-all-heldout-panels",
        "both-arm-all-root-gate-counts-fallback-and-novel-output-frequency-plus-paired-heldout-effects-and-costs");

    public static TowerProposalComparisonPlan CreatePreservationPlan(TowerProposalContext context, TowerProposalPolicy candidate)
    {
        context = TowerBatchRacing.Copy(context); candidate = TowerBatchRacing.Copy(candidate);
        TowerProposalPolicies.Validate(candidate);
        if (candidate.Version != TowerProposalPolicies.PreservingCreationPolicyVersion)
            throw new InvalidDataException("Preservation comparison requires the explicit v4 creation policy.");
        var control = TowerProposalPolicies.BenchmarkAffinityCreation(candidate.CreatedDamageAffinityIds!);
        TowerProposalPolicies.Validate(new TowerProposalExportRequest(TowerProposalPolicies.PreservingCreationExportVersion,
            context, [control, candidate]));
        var baseline = CreateValidationPlan(context, control);
        var benchmark = context.Scope.Starts.Single(s => s.ReferenceId == context.BenchmarkReferenceId).Party;
        var placements = new TowerAffinityCreation(TowerBossDiscovery.CopyGenerationInputs(context.Scope),
            TowerProposalPolicies.SelectedAffinities(context, candidate, creation: true), (_, _) => [], true);
        // Count the neighborhood, without drawing or screening prospective roots.
        var distinct = benchmark.Builds.Keys.SelectMany(owner => placements.Opportunities(benchmark, owner, default)
            .SelectMany(row => row.LegalEdits.Select(edit => HarnessJson.Hash(new { owner, edit.Removed, edit.Added }))))
            .Distinct(StringComparer.Ordinal).Count();
        if (distinct < 17) throw new InvalidDataException("Preserving creation requires at least 17 distinct legal benchmark edits before allocation.");
        var plan = baseline with { Version = PreservationVersion, Control = control, Candidate = candidate,
            RequiredFreshValues = PreservationRequiredFreshValues, Pairing = PreservationPairing,
            SelectionContrast = new(TowerBenchmarkValidation.Version, TowerBenchmarkValidation.Version),
            ValidationProtocol = PreservationDesign() };
        Validate(plan); return plan;
    }

    private static void ValidatePreservationPlan(TowerProposalComparisonPlan plan)
    {
        TowerProposalPolicies.Validate(plan.Control); TowerProposalPolicies.Validate(plan.Candidate);
        if (plan.Candidate.CreatedDamageAffinityIds is not { Count: > 0 }
            || HarnessJson.Hash(plan.Candidate) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkPreservingAffinityCreation(plan.Candidate.CreatedDamageAffinityIds))
            || HarnessJson.Hash(plan.Control) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkAffinityCreation(plan.Candidate.CreatedDamageAffinityIds))
            || plan.RequiredFreshValues != PreservationRequiredFreshValues || plan.Pairing != PreservationPairing
            || plan.SelectionContrast != new TowerProposalSelectionContrast(TowerBenchmarkValidation.Version, TowerBenchmarkValidation.Version)
            || plan.ValidationProtocol != PreservationDesign())
            throw new InvalidDataException("Changed preservation-only proposer contrast or shared validation protocol.");
        // Keep the existing v5 pilot's endpoints, all-root barrier, decision rules
        // and resource ceilings. Only the proposal rule and paired layout change.
        ValidateValidationPlan(plan with { Version = ValidationVersion, Candidate = plan.Control,
            RequiredFreshValues = ValidationRequiredFreshValues, Pairing = ValidationPairing,
            SelectionContrast = new(TowerProposalPolicies.BenchmarkTieSelectionVersion, TowerBenchmarkValidation.Version),
            ValidationProtocol = ValidationDesign() });
    }

    private static TowerProposalComparisonBinding BindPreservation(TowerProposalComparisonPlan plan, TowerProposalContext context, int[] values)
    {
        if (HarnessJson.Hash(plan) != HarnessJson.Hash(CreatePreservationPlan(context, plan.Candidate)))
            throw new InvalidDataException("Preservation comparison differs from its frozen physical context.");
        var d = context.Scope;
        var prior = d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Append(context.RootSeed)
            .Concat(d.References.SelectMany(r => r.Scenario.Seeds))
            .Concat(d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection)
                .Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? []))).ToHashSet();
        if (values.Length != PreservationRequiredFreshValues || values.Distinct().Count() != values.Length || values.Any(prior.Contains))
            throw new InvalidDataException("Supply exactly 4,380 distinct admitted values disjoint from history, without refill.");
        var pairs = new List<TowerProposalComparisonPair>();
        for (var i = 0; i < Roots; i++)
        {
            var search = values.Skip(i * PreservationValuesPerPair).Take(PreservationValuesPerPair).ToArray();
            var heldout = values.Skip(Roots * PreservationValuesPerPair + i * HeldoutSamples).Take(HeldoutSamples).ToArray();
            var scope = d with { Generation = d.Generation with { Seeds = [search[0]] },
                ExcludedCombatSeeds = d.ExcludedCombatSeeds.Concat(values.Except(search)).Distinct().Order().ToArray() };
            var panels = TowerBenchmarkValidation.PanelRoles.Take(4)
                .Select((role, p) => new TowerRacingPanel(role, search.Skip(1 + 8 * p).Take(8).ToArray()))
                .Concat(new[] { new TowerRacingPanel(TowerBenchmarkValidation.NominationRole, search.Skip(33).Take(16).ToArray()),
                    new TowerRacingPanel(TowerBenchmarkValidation.ValidationRole, search.Skip(49).Take(60).ToArray()) }).ToArray();
            var racing = new TowerAdaptiveRacingPlan(TowerAdaptiveRacing.Version, scope, context.Mechanics,
                context.BenchmarkReferenceId, search[0], panels, 528);
            var pair = new TowerProposalComparisonPair(i + 1,
                new(TowerProposalPolicies.BenchmarkValidationRacingVersion, racing, plan.Control, context.DamageAffinityInventory, TowerBenchmarkValidation.Version),
                new(TowerProposalPolicies.PreservingValidationRacingVersion, racing, plan.Candidate, context.DamageAffinityInventory, TowerBenchmarkValidation.Version), heldout);
            ValidatePair(pair, PreservationVersion); pairs.Add(pair);
        }
        return new(PreservationVersion, HarnessJson.Hash(plan), HarnessJson.Hash(values), pairs);
    }

    private static void ValidatePreservationPair(TowerProposalComparisonPair pair)
    {
        var a = pair.Control; var b = pair.Candidate; var racing = a.Racing;
        var values = racing.Panels.SelectMany(p => p.Seeds).Append(racing.RootSeed).ToArray();
        if (a.Version != TowerProposalPolicies.BenchmarkValidationRacingVersion || b.Version != TowerProposalPolicies.PreservingValidationRacingVersion
            || b.Policy.CreatedDamageAffinityIds is not { Count: > 0 }
            || HarnessJson.Hash(a.Policy) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkAffinityCreation(b.Policy.CreatedDamageAffinityIds))
            || HarnessJson.Hash(b.Policy) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkPreservingAffinityCreation(b.Policy.CreatedDamageAffinityIds))
            || HarnessJson.Hash(a.DamageAffinityInventory) != HarnessJson.Hash(b.DamageAffinityInventory)
            || HarnessJson.Hash(racing) != HarnessJson.Hash(b.Racing)
            || values.Length != PreservationValuesPerPair || values.Distinct().Count() != PreservationValuesPerPair
            || !racing.Scope.Generation.Seeds.SequenceEqual([racing.RootSeed])
            || pair.HeldoutSeeds is not { Count: HeldoutSamples } || pair.HeldoutSeeds.Distinct().Count() != HeldoutSamples
            || pair.HeldoutSeeds.Intersect(values).Any() || pair.HeldoutSeeds.Except(racing.Scope.ExcludedCombatSeeds).Any())
            throw new InvalidDataException("Preservation pair changed its policies, shared panels or isolated held-out values.");
    }

    internal static void ValidatePreservationTrajectories(TowerProposalRacingReport control, TowerProposalRacingReport candidate)
    {
        // Different proposals and beams are intentional. A physical recipe seen
        // on the same paired panel must still have the same captured outcome.
        var a = control.Evaluation.Panels.SelectMany(p => p.Observations)
            .ToDictionary(o => (o.Request.Role, o.Request.PartyId, o.Request.Seed));
        foreach (var row in candidate.Evaluation.Panels.SelectMany(p => p.Observations))
        {
            if (!a.TryGetValue((row.Request.Role, row.Request.PartyId, row.Request.Seed), out var other)) continue;
            if (TowerBossDiscovery.RecipeHash(row.Request.Scenario.Party) != TowerBossDiscovery.RecipeHash(other.Request.Scenario.Party)
                || (row.Outcome with { RequestHash = "", TrialId = "" }) != (other.Outcome with { RequestHash = "", TrialId = "" }))
                throw new InvalidDataException("Changed shared physical observation in the proposer comparison.");
        }
    }
}
