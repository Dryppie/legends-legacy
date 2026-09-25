namespace BalanceHarness;

public static partial class TowerProposalComparison
{
    public const string LoadoutPlacementVersion = "tower-loadout-placement-comparison-v1";
    internal const string LoadoutPlacementDecisionOrder = "incomplete-then-abandon-either-endpoint-then-no-differentiation-then-relative-and-absolute-gain-with-novelty-else-inconclusive-never-adopt";

    private static TowerProposalComparisonAnalysis PlacementAnalysis() => Analysis() with {
        GoBenchmarkAtLeast = .02, AbandonPromisingNovelRootsBelow = 0, DecisionOrder = LoadoutPlacementDecisionOrder
    };

    public static TowerProposalComparisonPlan CreateLoadoutPlacementPlan(TowerProposalContext context, TowerProposalPolicy control)
    {
        context = TowerBatchRacing.Copy(context); control = TowerBatchRacing.Copy(control);
        // The old factory verifies the exact control and its complete legal capacity.
        var baseline = CreateAlliedActionPlan(context, control);
        var candidate = TowerProposalPolicies.BenchmarkLoadoutPlacement();
        TowerProposalPolicies.Validate(new TowerProposalExportRequest(TowerProposalPolicies.LoadoutPlacementExportVersion,
            context, [control, candidate]));
        var plan = baseline with { Version = LoadoutPlacementVersion, Control = control, Candidate = candidate,
            Analysis = PlacementAnalysis() };
        Validate(plan); return plan;
    }

    private static void ValidateLoadoutPlacementPlan(TowerProposalComparisonPlan plan)
    {
        TowerProposalPolicies.Validate(plan.Control); TowerProposalPolicies.Validate(plan.Candidate);
        var ids = plan.Control.CreatedDamageAffinityIds;
        if (ids is not { Count: > 0 }
            || HarnessJson.Hash(plan.Control) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkAlliedActionAffinityCreation(ids))
            || HarnessJson.Hash(plan.Candidate) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkLoadoutPlacement())
            || HarnessJson.Hash(plan.Analysis) != HarnessJson.Hash(PlacementAnalysis()))
            throw new InvalidDataException("Changed frozen whole-loadout placement comparison.");
        // Validate the inherited layout, selector and resource bounds without changing old contracts.
        ValidateAlliedActionPlan(plan with { Version = AlliedActionVersion,
            Control = TowerProposalPolicies.BenchmarkPreservingAffinityCreation(ids), Candidate = plan.Control,
            Analysis = Analysis() with { GoPromisingNovelRootsAtLeast = 0, AbandonPromisingNovelRootsBelow = 0,
                DecisionOrder = SelectorDecisionOrder } });
    }

    private static TowerProposalComparisonBinding BindLoadoutPlacement(TowerProposalComparisonPlan plan,
        TowerProposalContext context, int[] values)
    {
        if (HarnessJson.Hash(plan) != HarnessJson.Hash(CreateLoadoutPlacementPlan(context, plan.Control)))
            throw new InvalidDataException("Placement comparison differs from its frozen physical context.");
        var baseline = BindAlliedAction(CreateAlliedActionPlan(context, plan.Control), context, values);
        var pairs = baseline.Pairs.Select(p => new TowerProposalComparisonPair(p.Root, p.Candidate,
            p.Candidate with { Version = TowerProposalPolicies.LoadoutPlacementRacingVersion,
                Policy = plan.Candidate, DamageAffinityInventory = null }, p.HeldoutSeeds)).ToArray();
        foreach (var pair in pairs) ValidatePair(pair, LoadoutPlacementVersion);
        return new(LoadoutPlacementVersion, HarnessJson.Hash(plan), baseline.ValuesHash, pairs);
    }

    private static void ValidateLoadoutPlacementPair(TowerProposalComparisonPair pair)
    {
        var a = pair.Control; var b = pair.Candidate; var ids = a.Policy.CreatedDamageAffinityIds;
        if (a.Version != TowerProposalPolicies.AlliedActionValidationRacingVersion
            || b.Version != TowerProposalPolicies.LoadoutPlacementRacingVersion || ids is not { Count: > 0 }
            || a.DamageAffinityInventory is null || b.DamageAffinityInventory is not null
            || HarnessJson.Hash(a.Policy) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkAlliedActionAffinityCreation(ids))
            || HarnessJson.Hash(b.Policy) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkLoadoutPlacement())
            || HarnessJson.Hash(a.Racing) != HarnessJson.Hash(b.Racing))
            throw new InvalidDataException("Changed placement paired policies, inventory or shared racing scope.");
        ValidateAlliedActionPair(pair with { Control = a with {
            Version = TowerProposalPolicies.PreservingValidationRacingVersion,
            Policy = TowerProposalPolicies.BenchmarkPreservingAffinityCreation(ids) }, Candidate = a });
    }
}
