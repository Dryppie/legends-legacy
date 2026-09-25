using Domain.Models.Combat;
using C = BalanceHarness.TowerReferenceExplorationComparison;

namespace BalanceHarness;

public sealed record AdaptivePilotRoot(int Restart, string BenchmarkParty, int BenchmarkWins, bool Novel,
    int BenchmarkGains, int BenchmarkLosses, double BenchmarkDifference,
    double MethodSeedStandardError, double BenchmarkSeedStandardError, double SeedCovariance);
public sealed record AdaptiveRootInterval(double Mean, double StandardDeviation, double Lower, double Upper);
public sealed record AdaptivePilotMetrics(string Interpretation, IReadOnlyList<AdaptivePilotRoot> Roots,
    AdaptiveRootInterval MethodRoots, AdaptiveRootInterval BenchmarkRoots, double MedianBenchmarkDifference,
    double WorstBenchmarkDifference, int NovelOutputs, int PromisingNovelOutputs,
    int AboveThreePoints, int AboveFivePoints, int BelowMinusThreePoints, int BelowMinusFivePoints,
    double BenchmarkConditionalLowerBound, string RootUncertainty, string ConditionalUncertainty);

/// <summary>Prospective development pilot. Shares admission, reservation, archive and
/// process ownership with the established comparison runner; never authorizes adoption.</summary>
public static class TowerAdaptiveRacingComparison
{
    public const string Version = "tower-adaptive-racing-comparison-v1";
    internal const string PlanHash = "268159cb2f03a44066e76012e34591e560ce74f1d84acf79617d07503378d550";
    internal const int Samples = 256;

    internal static TowerAdaptiveRacingPlan Bind(TowerBossDiscoveryDefinition scope, ExplorationReservation allocation,
        int restart, BossGenerationMechanics mechanics)
    {
        C.Require(allocation.Version == Version && restart is >= 0 and < 12
            && allocation.Selected.Count == 4428 && allocation.Selected.Distinct().Count() == 4428
            && !allocation.Selected.Intersect(scope.ExcludedCombatSeeds).Any()
            && scope.Generation.Seeds.SequenceEqual([allocation.Selected[restart * 113]]), "Wrong adaptive comparison binding.");
        // 1 shared construction root + 8/32 baseline + 8/8/8/8/40 adaptive.
        // All held-out panels follow all 1,356 search values. Nothing is recycled.
        var values = allocation.Selected.Skip(restart * 113 + 41).Take(72).ToArray();
        string[] roles = ["wave-1-screen", "wave-1-continuation", "wave-2-screen", "wave-2-continuation", "selection"];
        var panels = roles.Select((role, i) => new TowerRacingPanel(role, values.Skip(i * 8).Take(i == 4 ? 40 : 8).ToArray())).ToArray();
        var plan = new TowerAdaptiveRacingPlan(TowerAdaptiveRacing.Version, scope, mechanics,
            scope.Starts[2].ReferenceId, allocation.Selected[restart * 113], panels, 528);
        TowerAdaptiveRacing.Validate(plan); return plan;
    }

    internal static async Task<ExplorationArm> Search(TowerBossDiscoveryDefinition scope, ExplorationReservation allocation,
        int restart, BossGenerationMechanics mechanics, TowerBossDiscoveryRun.Battle battle,
        Action<string, object> save, CancellationToken ct)
    {
        var plan = Bind(scope, allocation, restart, mechanics);
        var prefix = $"pair-{restart + 1:D2}-candidate";
        save(prefix + "-plan.json", plan);
        var batches = 0; var panels = 0;
        var report = await TowerAdaptiveRacing.RunAsync(plan, async (request, token) => {
            var value = await battle(prefix + "/" + request.PanelHash, request.Role, request.Scenario, request.Seed, token);
            // The enclosing archive authenticates prepared inputs on reconstruction.
            // Trial IDs live in the global ledger, while panel ordinals are local.
            return TowerAdaptiveRacingNative.Authenticate(request, value.Trial, int.MaxValue, value.Trial, value.Report,
                restart * 1056 + 528);
        }, ct, checkpoint => {
            foreach (var batch in checkpoint.Batches.Skip(batches)) save(prefix + $"-batch-{++batches:D2}.json", batch);
            foreach (var panel in checkpoint.Evaluation.Panels.Skip(panels)) save(prefix + $"-panel-{++panels:D2}.json", panel.Freeze);
        }, panelFreezesOnly: true);
        foreach (var batch in report.Batches.Skip(batches)) save(prefix + $"-batch-{++batches:D2}.json", batch);
        save(prefix + "-adaptive.json", report);
        if (report.Evaluation.Status != "Complete")
            throw new ExplorationIncompleteException("Adaptive search failed; retain the failed root and attempts, without replacement: " + report.Evaluation.Error);
        C.Require(report.Evaluation.ChargedEvaluations == 528, "Changed adaptive cost.");
        var party = report.Evaluation.Panels[^1].Freeze.Parties.Single(p => p.Id == report.Evaluation.RawSelectedId);
        var scenario = TowerBossDiscovery.Scenario(scope, scope.Contexts.Single().Id, party, []);
        var finalist = new BossFinalist(party, true, "", "", "Adaptive racing provisional selection; held-out evaluation pending.");
        var output = new ExplorationOutput(TowerAdaptiveRacing.Version, scope.Stages.SelectionPolicyVersion,
            finalist, TowerBossDiscovery.RecipeHash(scenario.Party), scenario);
        var arm = new ExplorationArm(null, [], output, Adaptive: report);
        save(prefix + ".json", arm); return arm;
    }

    internal static ExplorationResult Summarize(TowerBossDiscoveryDefinition template, ExplorationStudy study,
        IReadOnlyList<ExplorationPair> pairs)
    {
        C.Require(pairs.Count == 12 && pairs.Select(p => p.Restart).SequenceEqual(Enumerable.Range(1, 12)), "Incomplete pilot roots.");
        var roots = new List<AdaptivePilotRoot>();
        foreach (var family in study.Freeze.Families)
        {
            bool[] Wins(ExplorationMember member) => study.Evidence.Single(e => e.Restart == family.Restart && e.RecipeHash == member.RecipeHash)
                .Trials.Select(t => t.Outcome == BattleOutcome.Victory).ToArray();
            var b = family.Members.Single(m => m.Baseline); var n = family.Members.Single(m => m.Candidate);
            var anchor = family.Members.Single(m => m.ReferenceIds.Contains(template.Starts[2].ReferenceId));
            var bw = Wins(b); var nw = Wins(n); var rw = Wins(anchor);
            var d = nw.Zip(bw).Select(p => (p.First ? 1d : 0d) - (p.Second ? 1d : 0d)).ToArray();
            var g = nw.Zip(rw).Select(p => (p.First ? 1d : 0d) - (p.Second ? 1d : 0d)).ToArray();
            var dm = d.Average(); var gm = g.Average();
            // Paired covariance retains dependence through the shared candidate/reference rows.
            double Cov(double[] x, double[] y, double xm, double ym) => x.Zip(y).Sum(p => (p.First-xm)*(p.Second-ym)) / (Samples * (Samples-1d));
            roots.Add(new(family.Restart, anchor.PartyId, rw.Count(x => x), n.ReferenceIds.Count == 0,
                g.Count(x => x > 0), g.Count(x => x < 0), gm, Math.Sqrt(Cov(d, d, dm, dm)),
                Math.Sqrt(Cov(g, g, gm, gm)), Cov(d, g, dm, gm)));
        }
        var method = Interval(pairs.Select(p => p.Difference).ToArray());
        var benchmark = Interval(roots.Select(p => p.BenchmarkDifference).ToArray());
        var ordered = roots.Select(p => p.BenchmarkDifference).Order().ToArray();
        var promising = roots.Count(p => p.Novel && p.BenchmarkDifference >= .03);
        var decision = Decision(method.Mean, benchmark.Mean, promising);
        const int denominator = 12 * Samples;
        var population = 4294967296d - template.ExcludedCombatSeeds.Count - C.SearchCount(Version);
        var depletion = (denominator - 1d) / population;
        var margin = Math.Sqrt(2 * Math.Log(20) / denominator) + depletion;
        var metrics = new AdaptivePilotMetrics("DevelopmentCriteriaOnlyNoPolicyPromotionOrTeamAdoption", roots,
            method, benchmark, (ordered[5]+ordered[6])/2, ordered[0], roots.Count(p => p.Novel), promising,
            ordered.Count(x => x >= .03), ordered.Count(x => x >= .05), ordered.Count(x => x <= -.03), ordered.Count(x => x <= -.05),
            Math.Max(-1, benchmark.Mean-margin), "TwoSided95PercentPairedRootT11SmallSampleApproximation",
            "SeparateOneSided95PercentConditionalHoeffdingWithDepletionNotFutureRootBounds");
        return new(Version, "Verified", decision, C.BoundVersion, pairs, pairs.Count(p => !p.Identical),
            pairs.Sum(p => p.Gains-p.Losses), pairs.Count(p => p.Difference > 0), denominator,
            method.Mean, depletion, margin, Math.Max(-1, method.Mean-margin),
            C.SearchFights + Samples * pairs.Sum(p => p.RecipeCount), [], metrics);
    }

    internal static string Decision(double method, double benchmark, int promising) =>
        method <= -.02 || benchmark <= -.02 && promising < 3 ? "AbandonThisConfiguration" :
        method >= .02 && benchmark >= 0 && promising >= 3 ? "LargerFreshEvaluationWarranted" : "InconclusiveRetainBaselineAndBenchmark";

    internal static AdaptiveRootInterval Interval(double[] values)
    {
        C.Require(values.Length == 12 && values.All(v => double.IsFinite(v) && Math.Abs(v) <= 1), "Invalid root differences.");
        var mean = values.Average(); var sd = Math.Sqrt(values.Sum(v => (v-mean)*(v-mean))/11);
        var margin = 2.200985160082949 * sd / Math.Sqrt(12);
        return new(mean, sd, Math.Max(-1, mean-margin), Math.Min(1, mean+margin));
    }
}
