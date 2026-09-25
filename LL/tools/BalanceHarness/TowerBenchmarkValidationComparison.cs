namespace BalanceHarness;

public sealed record TowerProposalValidationDesign(int ValuesPerPair, int SharedRacingValues,
    int ControlSelectionValues, int NominationValues, int ValidationValues,
    string NominationPairing, string ValidationIsolation, string Reporting);

public static partial class TowerProposalComparison
{
    public const string ValidationVersion = "tower-benchmark-validation-comparison-v1";
    public const int ValidationValuesPerPair = 133; // root + 32 racing + 40 control selection + 60 validation
    public const int ValidationRequiredFreshValues = Roots * (ValidationValuesPerPair + HeldoutSamples);
    private const string ValidationPairing = "shared-proposal-root-racing-and-first-16-selection-values-separate-charges";
    private static TowerProposalValidationDesign ValidationDesign() => new(133, 32, 40, 16, 60,
        "candidate-nomination-is-first-16-of-control-selection-in-frozen-order",
        "validation-disjoint-from-entire-control-selection-and-all-heldout-panels",
        "all-root-gate-counts-fallback-and-novel-output-frequency-plus-paired-heldout-effects-and-costs");

    public static int FreshValues(string version) => version switch {
        PreservationVersion or AlliedActionVersion or LoadoutPlacementVersion or NominationVersion => PreservationRequiredFreshValues,
        ValidationVersion => ValidationRequiredFreshValues,
        Version or CreationVersion or SelectorVersion => RequiredFreshValues,
        _ => throw new InvalidDataException("Unknown prospective comparison version.")
    };

    public static TowerProposalComparisonPlan CreateValidationPlan(TowerProposalContext context, TowerProposalPolicy generator)
    {
        var baseline = CreateSelectorPlan(context, generator);
        var plan = baseline with { Version = ValidationVersion, RequiredFreshValues = ValidationRequiredFreshValues,
            Pairing = ValidationPairing,
            SelectionContrast = new(TowerProposalPolicies.BenchmarkTieSelectionVersion, TowerBenchmarkValidation.Version),
            ValidationProtocol = ValidationDesign() };
        Validate(plan); return plan;
    }

    private static void ValidateValidationPlan(TowerProposalComparisonPlan plan)
    {
        // Reuse the fixed generator, endpoint, barrier and resource contract; only
        // this explicitly versioned selector and allocation contrast may differ.
        Validate(plan with { Version = SelectorVersion, RequiredFreshValues = RequiredFreshValues, Pairing = Pairing,
            SelectionContrast = new(TowerBossStudyPolicy.IncumbentTieVersion, TowerProposalPolicies.BenchmarkTieSelectionVersion),
            ValidationProtocol = null });
        if (plan.RequiredFreshValues != ValidationRequiredFreshValues || plan.Pairing != ValidationPairing
            || plan.SelectionContrast != new TowerProposalSelectionContrast(TowerProposalPolicies.BenchmarkTieSelectionVersion, TowerBenchmarkValidation.Version)
            || plan.ValidationProtocol != ValidationDesign())
            throw new InvalidDataException("Changed benchmark validation comparison protocol.");
    }

    private static TowerProposalComparisonBinding BindValidation(TowerProposalComparisonPlan plan, TowerProposalContext context, int[] values)
    {
        if (HarnessJson.Hash(plan) != HarnessJson.Hash(CreateValidationPlan(context, plan.Candidate)))
            throw new InvalidDataException("Validation comparison differs from its frozen physical context.");
        var d = context.Scope;
        var prior = d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Append(context.RootSeed)
            .Concat(d.References.SelectMany(r => r.Scenario.Seeds))
            .Concat(d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection)
                .Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? []))).ToHashSet();
        if (values.Length != ValidationRequiredFreshValues || values.Distinct().Count() != values.Length || values.Any(prior.Contains))
            throw new InvalidDataException("Supply exactly 4,668 distinct admitted values disjoint from history, without refill.");
        var pairs = new List<TowerProposalComparisonPair>();
        for (var i = 0; i < Roots; i++)
        {
            var search = values.Skip(i * ValidationValuesPerPair).Take(ValidationValuesPerPair).ToArray();
            var heldout = values.Skip(Roots * ValidationValuesPerPair + i * HeldoutSamples).Take(HeldoutSamples).ToArray();
            // Both scopes retain identical training identity. Only the fixed
            // schedules expose arm-specific values; all held-out/other-root values
            // are excluded in both arms before either search begins.
            var scope = d with { Generation = d.Generation with { Seeds = [search[0]] },
                ExcludedCombatSeeds = d.ExcludedCombatSeeds.Concat(values.Except(search)).Distinct().Order().ToArray() };
            var shared = TowerBenchmarkValidation.PanelRoles.Take(4).Select((r, p) => new TowerRacingPanel(r, search.Skip(1+8*p).Take(8).ToArray())).ToArray();
            var racing = new TowerAdaptiveRacingPlan(TowerAdaptiveRacing.Version, scope, context.Mechanics,
                context.BenchmarkReferenceId, search[0], shared.Append(new("selection", search.Skip(33).Take(40).ToArray())).ToArray(), 528);
            var candidate = racing with { Panels = shared.Concat(new[] {
                new TowerRacingPanel("nomination", search.Skip(33).Take(16).ToArray()),
                new TowerRacingPanel("validation", search.Skip(73).Take(60).ToArray()) }).ToArray() };
            var pair = new TowerProposalComparisonPair(i+1,
                new(TowerProposalPolicies.BenchmarkTieRacingVersion, racing, plan.Control, context.DamageAffinityInventory, plan.SelectionContrast!.Control),
                new(TowerProposalPolicies.BenchmarkValidationRacingVersion, candidate, plan.Candidate, context.DamageAffinityInventory, plan.SelectionContrast.Candidate), heldout);
            ValidatePair(pair, ValidationVersion); pairs.Add(pair);
        }
        return new(ValidationVersion, HarnessJson.Hash(plan), HarnessJson.Hash(values), pairs);
    }

    private static void ValidateValidationPair(TowerProposalComparisonPair pair)
    {
        var a = pair.Control; var b = pair.Candidate; var racing = a.Racing;
        var values = racing.Panels.SelectMany(p => p.Seeds).Append(racing.RootSeed)
            .Concat(b.Racing.Panels[^1].Seeds).ToArray();
        if (a.Version != TowerProposalPolicies.BenchmarkTieRacingVersion || b.Version != TowerProposalPolicies.BenchmarkValidationRacingVersion
            || !CandidateMatches(SelectorVersion, b.Policy) || HarnessJson.Hash(a.Policy) != HarnessJson.Hash(b.Policy)
            || HarnessJson.Hash(a.DamageAffinityInventory) != HarnessJson.Hash(b.DamageAffinityInventory)
            || HarnessJson.Hash(racing) != HarnessJson.Hash(b.Racing with { Panels = racing.Panels })
            || HarnessJson.Hash(racing.Panels.Take(4)) != HarnessJson.Hash(b.Racing.Panels.Take(4))
            || !b.Racing.Panels[4].Seeds.SequenceEqual(racing.Panels[4].Seeds.Take(16))
            || values.Length != ValidationValuesPerPair || values.Distinct().Count() != ValidationValuesPerPair
            || !racing.Scope.Generation.Seeds.SequenceEqual([racing.RootSeed])
            || pair.HeldoutSeeds is not { Count: HeldoutSamples } || pair.HeldoutSeeds.Distinct().Count() != HeldoutSamples
            || pair.HeldoutSeeds.Intersect(values).Any() || pair.HeldoutSeeds.Except(racing.Scope.ExcludedCombatSeeds).Any())
            throw new InvalidDataException("Validation pair changed its generator, shared prefix or isolated panels.");
    }

    internal static void ValidateValidationTrajectories(TowerProposalRacingReport control, TowerProposalRacingReport candidate)
    {
        // First 328 observations, generated batches and nominees must agree.
        // The candidate's nomination is paired with the control's first 16 values;
        // selection scores, final membership and decisions intentionally differ.
        var a = control with { Evaluation = control.Evaluation with { Panels = control.Evaluation.Panels.Take(4).ToArray() } };
        var b = candidate with { Evaluation = candidate.Evaluation with { Panels = candidate.Evaluation.Panels.Take(4).ToArray() } };
        ValidateSelectorTrajectories(a, b);
        if (control.Evaluation.Panels.Count != 5 || candidate.Evaluation.Panels.Count != 6)
            throw new InvalidDataException("Validation comparison requires five control and six candidate panels.");
        var selection = control.Evaluation.Panels[4]; var nomination = candidate.Evaluation.Panels[4];
        if (!nomination.Freeze.Seeds.SequenceEqual(selection.Freeze.Seeds.Take(16))
            || HarnessJson.Hash(nomination.Freeze.Parties) != HarnessJson.Hash(selection.Freeze.Parties))
            throw new InvalidDataException("Changed paired nomination membership.");
        foreach (var row in nomination.Observations)
        {
            var other = selection.Observations.Single(o => o.Request.PartyId == row.Request.PartyId && o.Request.Seed == row.Request.Seed);
            // Scenario IDs include the panel seed list. Compare the physical
            // recipe and all literal result metrics, not those evidence labels.
            if (TowerBossDiscovery.RecipeHash(row.Request.Scenario.Party) != TowerBossDiscovery.RecipeHash(other.Request.Scenario.Party)
                || (row.Outcome with { RequestHash = "", TrialId = "" }) != (other.Outcome with { RequestHash = "", TrialId = "" }))
                throw new InvalidDataException("Changed shared selection/nomination observation.");
        }
    }
}
