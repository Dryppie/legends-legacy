namespace BalanceHarness;

public static partial class TowerProposalComparison
{
    public const string AlliedActionVersion = "tower-affinity-allied-action-comparison-v1";

    public static TowerProposalComparisonPlan CreateAlliedActionPlan(TowerProposalContext context, TowerProposalPolicy candidate)
    {
        context = TowerBatchRacing.Copy(context); candidate = TowerBatchRacing.Copy(candidate);
        TowerProposalPolicies.Validate(candidate);
        if (candidate.Version != TowerProposalPolicies.AlliedActionPolicyVersion)
            throw new InvalidDataException("Allied-action comparison requires the explicit v5 creation policy.");
        var control = TowerProposalPolicies.BenchmarkPreservingAffinityCreation(candidate.CreatedDamageAffinityIds!);
        TowerProposalPolicies.Validate(new TowerProposalExportRequest(TowerProposalPolicies.AlliedActionExportVersion,
            context, [control, candidate]));
        var baseline = CreatePreservationPlan(context, control);
        var parent = context.Scope.Starts.Single(s => s.ReferenceId == context.BenchmarkReferenceId).Party;
        var placements = new TowerAffinityCreation(TowerBossDiscovery.CopyGenerationInputs(context.Scope),
            TowerProposalPolicies.SelectedAffinities(context, candidate, creation: true), (_, _) => [], true,
            TowerAlliedActionProtection.Create(context.DamageAffinityInventory!));
        // Complete legal neighborhood only; never sample prospective roots here.
        var distinct = parent.Builds.Keys.SelectMany(owner => placements.Opportunities(parent, owner, default)
            .SelectMany(row => row.LegalEdits.Select(edit => HarnessJson.Hash(new { owner, edit.Removed, edit.Added }))))
            .Distinct(StringComparer.Ordinal).Count();
        if (distinct < 17) throw new InvalidDataException("Allied-action creation requires at least 17 distinct legal benchmark edits before allocation.");
        var plan = baseline with { Version = AlliedActionVersion, Control = control, Candidate = candidate };
        Validate(plan); return plan;
    }

    private static void ValidateAlliedActionPlan(TowerProposalComparisonPlan plan)
    {
        TowerProposalPolicies.Validate(plan.Control); TowerProposalPolicies.Validate(plan.Candidate);
        var ids = plan.Candidate.CreatedDamageAffinityIds;
        if (ids is not { Count: > 0 }
            || HarnessJson.Hash(plan.Control) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkPreservingAffinityCreation(ids))
            || HarnessJson.Hash(plan.Candidate) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkAlliedActionAffinityCreation(ids)))
            throw new InvalidDataException("Changed allied-action-only proposer contrast.");
        // All scientific settings are identical to the frozen preceding design.
        ValidatePreservationPlan(plan with { Version = PreservationVersion,
            Control = TowerProposalPolicies.BenchmarkAffinityCreation(ids), Candidate = plan.Control });
    }

    private static TowerProposalComparisonBinding BindAlliedAction(TowerProposalComparisonPlan plan, TowerProposalContext context, int[] values)
    {
        if (HarnessJson.Hash(plan) != HarnessJson.Hash(CreateAlliedActionPlan(context, plan.Candidate)))
            throw new InvalidDataException("Allied-action comparison differs from its frozen physical context.");
        // Reuse the exact 109-value paired layout and all historical exclusions.
        var baseline = BindPreservation(CreatePreservationPlan(context, plan.Control), context, values);
        var pairs = baseline.Pairs.Select(pair => new TowerProposalComparisonPair(pair.Root, pair.Candidate,
            pair.Candidate with { Version = TowerProposalPolicies.AlliedActionValidationRacingVersion, Policy = plan.Candidate },
            pair.HeldoutSeeds)).ToArray();
        foreach (var pair in pairs) ValidatePair(pair, AlliedActionVersion);
        return new(AlliedActionVersion, HarnessJson.Hash(plan), baseline.ValuesHash, pairs);
    }

    private static void ValidateAlliedActionPair(TowerProposalComparisonPair pair)
    {
        var a = pair.Control; var b = pair.Candidate; var ids = b.Policy.CreatedDamageAffinityIds;
        if (a.Version != TowerProposalPolicies.PreservingValidationRacingVersion
            || b.Version != TowerProposalPolicies.AlliedActionValidationRacingVersion || ids is not { Count: > 0 }
            || HarnessJson.Hash(a.Policy) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkPreservingAffinityCreation(ids))
            || HarnessJson.Hash(b.Policy) != HarnessJson.Hash(TowerProposalPolicies.BenchmarkAlliedActionAffinityCreation(ids)))
            throw new InvalidDataException("Changed allied-action paired policy contract.");
        ValidatePreservationPair(pair with {
            Control = a with { Version = TowerProposalPolicies.BenchmarkValidationRacingVersion,
                Policy = TowerProposalPolicies.BenchmarkAffinityCreation(ids) },
            Candidate = b with { Version = TowerProposalPolicies.PreservingValidationRacingVersion, Policy = a.Policy }
        });
    }
}
