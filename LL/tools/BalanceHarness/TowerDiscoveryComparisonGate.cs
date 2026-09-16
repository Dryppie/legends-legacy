namespace BalanceHarness;

public sealed record TowerDiscoveryComparisonInput(BossDiscoveryInputs Inputs, BossDiscoveryRunReport Report);
public sealed record TowerDiscoveryComparisonArm(string Policy, string InputHash, string ReportHash,
    string ReportStatus, string? GenerationStatus, string? StopReason, int Proposals, int Evaluations,
    int PlannedBattles, int ActualBattles, IReadOnlyDictionary<string, int> ProposalResults, IReadOnlyList<string> Issues);
public sealed record TowerDiscoveryComparisonDecision(string Version, string Status, IReadOnlyList<TowerDiscoveryComparisonArm> Arms);
public sealed record TowerDiscoveryComparisonNomination(string Policy, int Rank, PartyChoice Party);

/// <summary>Fail-closed nomination boundary for already verified baseline/refinement discovery archives.</summary>
public static class TowerDiscoveryComparisonGate
{
    public const string Version = "tower-discovery-comparison-gate-v1";
    public const string NovelVersion = "tower-discovery-comparison-gate-v2";
    public const string LocalVersion = "tower-discovery-comparison-gate-v3";

    public const string FreshFirstVersion = "tower-discovery-comparison-gate-v4";

    public static TowerDiscoveryComparisonDecision Inspect(IReadOnlyList<TowerDiscoveryComparisonInput> pair)
    {
        if (pair.Count != 2 || pair[0].Inputs.Generation.PolicyVersion != TowerTeamCoverageSearch.Version
            || pair[1].Inputs.Generation.PolicyVersion is not (TowerDiscoveryRefinementSearch.Version or TowerDiscoveryRefinementSearch.NovelVersion or TowerDiscoveryRefinementSearch.LocalVersion or TowerDiscoveryRefinementSearch.FreshFirstVersion))
            throw new InvalidDataException("Expected baseline then refinement discovery.");
        foreach (var item in pair)
        {
            TowerBossGeneration.ValidateInputs(item.Inputs);
            var g = item.Inputs.Generation;
            if (g.Seeds.Count != 1 || g.Methods.Count != 1 || g.CandidatesPerArm != 16
                || g.MaximumAttemptsPerArm != 16 || item.Inputs.ShortlistCandidates < 2)
                throw new InvalidDataException("Comparison requires the unchanged single-arm 16-candidate/16-proposal allocation.");
        }
        var baseline = pair[0].Inputs;
        var candidate = pair[1].Inputs;
        if (HarnessJson.Hash(baseline) != HarnessJson.Hash(candidate with { Generation = candidate.Generation with {
                PolicyVersion = baseline.Generation.PolicyVersion, Methods = baseline.Generation.Methods } }))
            throw new InvalidDataException("Comparison inputs differ beyond policy and method.");
        var arms = pair.Select(InspectArm).ToArray();
        return new(candidate.Generation.PolicyVersion == TowerDiscoveryRefinementSearch.FreshFirstVersion ? FreshFirstVersion
            : candidate.Generation.PolicyVersion == TowerDiscoveryRefinementSearch.LocalVersion ? LocalVersion
            : candidate.Generation.PolicyVersion == TowerDiscoveryRefinementSearch.NovelVersion ? NovelVersion : Version,
            arms.All(a => a.Issues.Count == 0) ? "Ready" : "StoppedDiscovery", arms);
    }

    private static TowerDiscoveryComparisonArm InspectArm(TowerDiscoveryComparisonInput item)
    {
        var d = item.Inputs; var r = item.Report; var g = r.Generation;
        var arm = g?.Arms.Count == 1 ? g.Arms[0] : null;
        var proposals = arm?.Proposals ?? []; var evaluations = arm?.Evaluations ?? [];
        var issues = new List<string>();
        void Check(bool condition, string reason) { if (!condition) issues.Add(reason); }
        var perCandidate = d.DiscoverySeeds.Values.Sum(s => s.Count);
        Check(r.Status == "Complete" && g?.Status == "Complete" && r.Error is null && g.Error is null, "discovery-not-complete");
        Check(g?.Version == d.Generation.PolicyVersion && arm?.Method == d.Generation.Methods[0]
            && arm?.Seed == d.Generation.Seeds[0], "generation-identity-mismatch");
        Check(arm?.StopReason == "CandidateBudgetReached", "candidate-budget-not-reached");
        Check(proposals.Count == 16 && evaluations.Count == 16, "incomplete-allocation");
        Check(r.PlannedDiscoveryBattles == 16 * perCandidate && r.ActualBattles == evaluations.Count * perCandidate
            && r.ActualBattles == r.PlannedDiscoveryBattles && r.CacheHits == 0, "discovery-battle-count-mismatch");
        var accepted = proposals.Where(p => p.Result == "evaluated").ToArray();
        Check(accepted.Length == evaluations.Count && accepted.All(p => p.Party is not null)
            && accepted.Select(p => p.Party?.Id).Distinct().Count() == accepted.Length
            && evaluations.Select(e => e.Id).Distinct().Count() == evaluations.Count
            && accepted.Select(p => p.Party?.Id).Order().SequenceEqual(evaluations.Select(e => e.Id).Order()), "evaluation-proposal-mismatch");
        Check(proposals.Select(p => p.Provenance.Id).Distinct().Count() == proposals.Count
            && proposals.All(p => p.Provenance.GenerationSeed == d.Generation.Seeds[0]
                && p.Provenance.Method == d.Generation.Methods[0] && p.Provenance.ReferenceIds.Count == 0), "proposal-identity-mismatch");
        Check(accepted.All(p => p.Party is not null && p.Party.Id == HarnessJson.Hash(p.Party.Builds)
            && p.Party.Builds.Values.All(TowerCompositionSearch.IsCanonical)), "recipe-identity-or-order-mismatch");
        try
        {
            Check(evaluations.All(e => e.Fitness == TowerBossGeneration.Fitness(d, e.Cells, e.Fitness.VictoryDuration)), "fitness-mismatch");
        }
        catch (InvalidDataException) { issues.Add("invalid-measurement-cells"); }
        return new(d.Generation.PolicyVersion, HarnessJson.Hash(d), HarnessJson.Hash(r), r.Status, g?.Status,
            arm?.StopReason, proposals.Count, evaluations.Count, r.PlannedDiscoveryBattles, r.ActualBattles,
            proposals.GroupBy(p => p.Result, StringComparer.Ordinal).OrderBy(p => p.Key, StringComparer.Ordinal)
                .ToDictionary(p => p.Key, p => p.Count(), StringComparer.Ordinal), issues);
    }

    public static IReadOnlyList<TowerDiscoveryComparisonNomination> Nominate(
        IReadOnlyList<TowerDiscoveryComparisonInput> pair, string receiptPath, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var decision = Inspect(pair);
        // The stop/ready receipt is durable before returning any nominees. Never overwrite or refill.
        TowerCompleteFamilyRun.Durable(receiptPath, decision);
        token.ThrowIfCancellationRequested();
        if (decision.Status != "Ready")
            throw new InvalidDataException("Discovery stopped the entire comparison; inspect the saved gate receipt. No nominations or later stages.");
        return pair.SelectMany(item =>
        {
            var arm = item.Report.Generation!.Arms.Single();
            var parties = arm.Proposals.ToDictionary(p => p.Party!.Id, p => p.Party!);
            return TowerBossGeneration.Rank(arm.Evaluations).Take(2)
                .Select((e, i) => new TowerDiscoveryComparisonNomination(item.Inputs.Generation.PolicyVersion, i + 1, parties[e.Id]));
        }).ToArray();
    }
}
