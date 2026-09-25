namespace BalanceHarness;

public static partial class TowerProposalComparison
{
    public const string NominationVersion = "tower-affinity-nomination-comparison-v1";

    public static TowerProposalComparisonPlan CreateNominationPlan(TowerProposalContext context, TowerProposalPolicy generator)
    {
        var baseline = CreateValidationPlan(context, generator);
        var plan = baseline with { Version = NominationVersion, RequiredFreshValues = PreservationRequiredFreshValues,
            Pairing = PreservationPairing, SelectionContrast = new(TowerBenchmarkValidation.Version, TowerBenchmarkValidation.Version),
            ValidationProtocol = PreservationDesign(), Analysis = PlacementAnalysis() };
        Validate(plan);
        return plan;
    }

    private static void ValidateNominationPlan(TowerProposalComparisonPlan plan)
    {
        if (plan.RequiredFreshValues != PreservationRequiredFreshValues || plan.Pairing != PreservationPairing
            || plan.SelectionContrast != new TowerProposalSelectionContrast(TowerBenchmarkValidation.Version, TowerBenchmarkValidation.Version)
            || plan.ValidationProtocol != PreservationDesign() || HarnessJson.Hash(plan.Analysis) != HarnessJson.Hash(PlacementAnalysis()))
            throw new InvalidDataException("Changed single-cycle nomination comparison.");
        ValidateValidationPlan(plan with { Version = ValidationVersion, RequiredFreshValues = ValidationRequiredFreshValues,
            Pairing = ValidationPairing, SelectionContrast = new(TowerProposalPolicies.BenchmarkTieSelectionVersion, TowerBenchmarkValidation.Version),
            ValidationProtocol = ValidationDesign(), Analysis = Analysis() with {
                GoPromisingNovelRootsAtLeast = 0, AbandonPromisingNovelRootsBelow = 0, DecisionOrder = SelectorDecisionOrder } });
    }

    private static TowerProposalComparisonBinding BindNomination(TowerProposalComparisonPlan plan, TowerProposalContext context, int[] values)
    {
        if (HarnessJson.Hash(plan) != HarnessJson.Hash(CreateNominationPlan(context, plan.Candidate)))
            throw new InvalidDataException("Nomination comparison differs from its frozen context.");
        var d = context.Scope;
        var prior = d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Append(context.RootSeed)
            .Concat(d.References.SelectMany(r => r.Scenario.Seeds))
            .Concat(d.Stages.Schedules.Values.SelectMany(s => s.Discovery.Concat(s.Selection)
                .Concat(s.Confirmation).Concat(s.Diagnostics).Concat(s.Feedback ?? []))).ToHashSet();
        if (values.Length != PreservationRequiredFreshValues || values.Distinct().Count() != values.Length || values.Any(prior.Contains))
            throw new InvalidDataException("Supply exactly 4,380 fresh admitted values without refill.");
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
            var control = TowerAffinitySearch.CreatePlan(racing, context.DamageAffinityInventory!, plan.Control.CreatedDamageAffinityIds!);
            var pair = new TowerProposalComparisonPair(i + 1, control,
                control with { Version = TowerAffinitySearch.GeneratedNominationVersion }, heldout);
            ValidatePair(pair, NominationVersion); pairs.Add(pair);
        }
        return new(NominationVersion, HarnessJson.Hash(plan), HarnessJson.Hash(values), pairs);
    }

    private static void ValidateNominationPair(TowerProposalComparisonPair pair)
    {
        TowerAffinitySearch.Validate(pair.Control);
        if (pair.Candidate.Version != TowerAffinitySearch.GeneratedNominationVersion
            || HarnessJson.Hash(pair.Control) != HarnessJson.Hash(pair.Candidate with { Version = pair.Control.Version }))
            throw new InvalidDataException("Nomination comparison may only change nominee eligibility.");
        // Reuse the shared 109-value layout validator after substituting only its policy labels.
        var ids = pair.Control.Policy.CreatedDamageAffinityIds!;
        ValidatePreservationPair(pair with { Candidate = pair.Candidate with {
            Version = TowerProposalPolicies.PreservingValidationRacingVersion,
            Policy = TowerProposalPolicies.BenchmarkPreservingAffinityCreation(ids) } });
    }

    internal static void ValidateNominationTrajectories(TowerProposalRacingReport control, TowerProposalRacingReport candidate)
    {
        if (control.Evaluation.Panels.Count != 6 || candidate.Evaluation.Panels.Count != 6)
            throw new InvalidDataException("Nomination comparison requires complete six-panel searches.");
        ValidateSelectorTrajectories(
            control with { Evaluation = control.Evaluation with { Panels = control.Evaluation.Panels.Take(5).ToArray() } },
            candidate with { Evaluation = candidate.Evaluation with { Panels = candidate.Evaluation.Panels.Take(5).ToArray() } });
        ValidatePreservationTrajectories(control, candidate);
    }
}
