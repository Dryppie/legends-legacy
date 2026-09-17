namespace BalanceHarness;

public sealed record AnchoredComparisonPair(int Restart, string BaselineParty, string AnchoredParty,
    int BaselineWins, int AnchoredWins, int Gains, int Losses, double Difference,
    int BaselineDiscoveryFights, int AnchoredDiscoveryFights);
public sealed record AnchoredComparisonResult(string Version, string Status, string Decision,
    IReadOnlyList<AnchoredComparisonPair> Pairs, double MeanDifference, double LowerBound,
    double CollisionAllowance, int Started, int Completed, double ElapsedSeconds);

/// <summary>Prospective anchored-versus-incumbent comparison; the historical racing contract stays separate.</summary>
public static class TowerAnchoredComparison
{
    public const string Version = "tower-anchored-comparison-v1";
    internal const int ValuesPerRestart = 1041, SelectedValues = 3123;

    internal static TowerBossDiscoveryDefinition Bind(TowerBossDiscoveryDefinition template, int[] values, int restart, bool anchored)
    {
        TowerPracticalSearch.Require(template.Generation.PolicyVersion == TowerAnchoredNeighborhoodSearch.Version
            && template.PrimaryReferenceId is not null && template.Starts.Any(s => s.ReferenceId == template.PrimaryReferenceId)
            && values.Length == SelectedValues && values.Distinct().Count() == values.Length
            && restart is >= 0 and < 3 && !values.Intersect(template.ExcludedCombatSeeds).Any(), "Invalid anchored comparison allocation or primary reference.");
        var panel = values.Skip(restart * ValuesPerRestart).Take(ValuesPerRestart).ToArray();
        var d = template with {
            Id = "anchored-pair-" + (restart + 1),
            PrimaryReferenceId = anchored ? template.PrimaryReferenceId : null,
            Generation = template.Generation with { Seeds = [panel[0]], CandidatesPerArm = 46,
                PolicyVersion = anchored ? TowerAnchoredNeighborhoodSearch.Version : TowerSuppliedCompositionSearch.IncumbentVersion },
            Stages = template.Stages with { Schedules = template.Stages.Schedules.ToDictionary(p => p.Key,
                _ => new BossDiscoverySchedule(panel.Skip(1).Take(8).ToArray(), panel.Skip(9).Take(32).ToArray(), panel.Skip(41).ToArray(), [])) },
            MaximumBattles = TowerAllocationComparison.MaximumFights / 6
        };
        var cost = TowerBossDiscovery.Validate(d);
        TowerPracticalSearch.Require(cost.Discovery == 368 && cost.Selection == 128 && cost.Total == 3496, "Unequal anchored comparison ceiling.");
        return d;
    }

    internal static AnchoredComparisonResult Assess(IReadOnlyList<BossStudyReport> reports, int historicalCount,
        int started, int completed, double elapsed)
    {
        TowerPracticalSearch.Require(reports.Count == 6 && reports.Select((r, i) =>
            r.Discovery?.Version == (i % 2 == 0 ? TowerSuppliedCompositionSearch.IncumbentVersion : TowerAnchoredNeighborhoodSearch.Version)
            && r.Accounting.Completed["discovery"] == 368 && r.Accounting.Completed["selection"] == 128).All(valid => valid),
            "Anchored comparison requires all six declared policies and complete fixed search budgets.");
        var shared = TowerAllocationComparison.Assess(reports, historicalCount, started, completed, elapsed, searchValuesPerRestart: 41);
        return new(Version, shared.Status, shared.Decision == "SupportsRacingForFrozenOutputs"
            ? "SupportsAnchoredForFrozenOutputs" : "DoNotPromoteAnchored",
            shared.Pairs.Select(p => new AnchoredComparisonPair(p.Restart, p.BaselineParty, p.RacingParty,
                p.BaselineWins, p.RacingWins, p.Gains, p.Losses, p.Difference, p.BaselineDiscoveryFights, p.RacingDiscoveryFights)).ToArray(),
            shared.MeanDifference, shared.LowerBound, shared.CollisionAllowance, started, completed, elapsed);
    }

    public static Task<AnchoredComparisonResult> RunAsync(TowerAllocationComparisonRequest request, CancellationToken token = default)
        => TowerAllocationComparison.RunCoreAsync(request, Version, TowerAnchoredNeighborhoodSearch.Version, 46, 8, SelectedValues,
            Bind, Assess, token);
}
