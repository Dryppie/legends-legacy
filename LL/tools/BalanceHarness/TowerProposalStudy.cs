using Domain.Models.Combat;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record ProposalStudyMember(string RecipeHash, PartyChoice Party, TowerScenario Scenario, IReadOnlyList<string> Roles);
public sealed record ProposalStudyFamily(int Root, IReadOnlyList<int> Seeds, IReadOnlyList<ProposalStudyMember> Members);
public sealed record ProposalStudyFreeze(string Version, string PlanHash, string ContextHash, string ValuesHash,
    string SearchesHash, string AttemptsHash, IReadOnlyList<ProposalStudyFamily> Families);
public sealed record ProposalStudyEvidence(int Root, string RecipeHash, IReadOnlyList<TowerPanelObservation> Observations);
public sealed record ProposalStudyRoot(int Root, string ControlParty, string CandidateParty, string BenchmarkParty,
    bool Identical, bool Novel, IReadOnlyList<int> ChangedPositionsPerWave, int ControlWins, int CandidateWins, int BenchmarkWins,
    int MethodGains, int MethodLosses, int BenchmarkGains, int BenchmarkLosses, double MethodDifference, double BenchmarkDifference,
    double MethodSeedStandardError, double BenchmarkSeedStandardError, double SeedCovariance);
public sealed record ProposalStudyResult(string Version, string Status, string Decision, string PlanHash, string FreezeHash,
    int Fights, int SearchFights, int HeldoutFights, int DifferingRoots, int NovelRoots, int PromisingNovelRoots,
    IReadOnlyList<ProposalStudyRoot> Roots, AdaptiveRootInterval Method, AdaptiveRootInterval Benchmark,
    double MethodMedian, double MethodWorst, double BenchmarkMedian, double BenchmarkWorst, string Interpretation,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ProposalValidationDiagnostics? Validation = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ProposalValidationDiagnostics? ControlValidation = null);
public sealed record ProposalValidationDiagnostics(int PassedRoots, int FallbackRoots,
    IReadOnlyList<TowerBenchmarkValidationDecision> Decisions);

/// <summary>All-root search barrier followed by shared physical-recipe held-out
/// evaluation. Production adapters authenticate observations; fixtures inject only
/// that boundary, retaining the real searches, freeze, allocation and arithmetic.</summary>
public static partial class TowerProposalStudy
{
    public const string Version = TowerProposalComparison.Version;
    public const string CreationVersion = TowerProposalComparison.CreationVersion;
    public const string SelectorVersion = TowerProposalComparison.SelectorVersion;
    public const string ValidationVersion = TowerProposalComparison.ValidationVersion;
    public const string PreservationVersion = TowerProposalComparison.PreservationVersion;
    public const string AlliedActionVersion = TowerProposalComparison.AlliedActionVersion;
    public const string NominationVersion = TowerProposalComparison.NominationVersion;
    public const string LoadoutPlacementVersion = TowerProposalComparison.LoadoutPlacementVersion;
    internal const int SearchFights = 12 * 2 * 528;
    internal static void Require(bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }

    internal static void ValidateVersion(string version) => Require(version is Version or CreationVersion or SelectorVersion or ValidationVersion or PreservationVersion or AlliedActionVersion or LoadoutPlacementVersion or NominationVersion, "Unknown owned proposal study version.");

    internal static void ValidateDesign(TowerProposalComparisonPlan plan) => TowerProposalComparison.Validate(plan);

    internal static async Task<ProposalStudyResult> Execute(TowerProposalComparisonPlan plan, TowerProposalContext context,
        IReadOnlyList<int> values, Func<TowerProposalComparisonPair, Task<TowerProposalComparisonSearch>> search,
        Func<ProposalStudyFreeze, TowerPanelTrial, Task<TowerPanelOutcome>> measure,
        Action<string, object> save, Func<string> attemptPrefix, Action<bool> attempt, CancellationToken ct)
    {
        plan = TowerBatchRacing.Copy(plan); context = TowerBatchRacing.Copy(context);
        ValidateDesign(plan);
        var binding = TowerProposalComparison.Bind(plan, context, values);
        save("binding.json", new { version = plan.Version, planHash = HarnessJson.Hash(plan), contextHash = HarnessJson.Hash(context), valuesHash = binding.ValuesHash });
        var searches = new List<TowerProposalComparisonSearch>(); var families = new List<ProposalStudyFamily>();
        foreach (var pair in binding.Pairs)
        {
            ct.ThrowIfCancellationRequested();
            if (plan.Version == LoadoutPlacementVersion)
                save($"placement-catalogue-{pair.Root:D2}.json", TowerLoadoutPlacement.Create(
                    pair.Candidate.Racing.Scope, pair.Candidate.Racing.BenchmarkReferenceId, ct));
            var result = await search(pair);
            save($"pair-{pair.Root:D2}.json", result);
            Require(result.PlanHash == HarnessJson.Hash(plan) && result.PairHash == HarnessJson.Hash(pair)
                && result.Status == "Complete" && Complete(result.Control, pair.Control)
                && result.Candidate is not null && Complete(result.Candidate, pair.Candidate),
                "Incomplete or changed search pair; retain failures without replacement.");
            if (plan.Version == SelectorVersion) TowerProposalComparison.ValidateSelectorTrajectories(result.Control, result.Candidate!);
            if (plan.Version == NominationVersion) TowerProposalComparison.ValidateNominationTrajectories(result.Control, result.Candidate!);
            if (plan.Version == ValidationVersion) TowerProposalComparison.ValidateValidationTrajectories(result.Control, result.Candidate!);
            if (plan.Version is PreservationVersion or AlliedActionVersion or LoadoutPlacementVersion or NominationVersion) TowerProposalComparison.ValidatePreservationTrajectories(result.Control, result.Candidate!);
            searches.Add(result);
            PartyChoice Selected(TowerProposalRacingReport report) => report.Evaluation.Panels[^1].Freeze.Parties
                .Single(p => p.Id == report.Evaluation.RawSelectedId);
            var parties = new[] { ("control", Selected(result.Control)), ("candidate", Selected(result.Candidate!)),
                ("benchmark", context.Scope.Starts.Single(s => s.ReferenceId == context.BenchmarkReferenceId).Party) };
            var members = parties.Select(p => {
                var scenario = TowerBossDiscovery.Scenario(context.Scope, context.Scope.Contexts.Single().Id, p.Item2, pair.HeldoutSeeds);
                return new ProposalStudyMember(TowerBossDiscovery.RecipeHash(scenario.Party), p.Item2, scenario, [p.Item1]);
            }).GroupBy(m => m.RecipeHash).Select(g => g.First() with { Roles = g.SelectMany(m => m.Roles).Order(StringComparer.Ordinal).ToArray() })
                .OrderBy(m => m.RecipeHash, StringComparer.Ordinal).ToArray();
            families.Add(new(pair.Root, pair.HeldoutSeeds, members));
        }
        // This durable write is the global barrier: no held-out evaluator has run.
        var freeze = new ProposalStudyFreeze(plan.Version, HarnessJson.Hash(plan), HarnessJson.Hash(context), binding.ValuesHash,
            HarnessJson.Hash(searches), attemptPrefix(), families);
        save("freeze.json", freeze);
        var scopeHash = HarnessJson.Hash(context.Scope); var freezeHash = HarnessJson.Hash(freeze);
        var evidence = new List<ProposalStudyEvidence>(); var ordinal = 0; var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var family in families)
        foreach (var member in family.Members)
        {
            var observations = new List<TowerPanelObservation>();
            foreach (var seed in family.Seeds)
            {
                ct.ThrowIfCancellationRequested();
                var request = new TowerPanelTrial(scopeHash, freezeHash,
                    $"heldout-root-{family.Root:D2}", member.Party.Id, ++ordinal, seed, member.Scenario);
                attempt(false);
                var outcome = await measure(freeze, request);
                Require(outcome.RequestHash == HarnessJson.Hash(request) && outcome.Seed == seed && !string.IsNullOrWhiteSpace(outcome.TrialId) && ids.Add(outcome.TrialId)
                    && Enum.IsDefined(outcome.Outcome) && double.IsFinite(outcome.GuardianHealth) && outcome.GuardianHealth is >= 0 and <= 100
                    && double.IsFinite(outcome.Survival) && outcome.Survival is >= 0 and <= 100
                    && double.IsFinite(outcome.DurationSeconds) && outcome.DurationSeconds >= 0, "Invalid held-out observation.");
                observations.Add(new(request, outcome)); attempt(true);
            }
            var cell = new ProposalStudyEvidence(family.Root, member.RecipeHash, observations); evidence.Add(cell);
            save($"heldout-{family.Root:D2}-{member.RecipeHash}.json", cell);
        }
        Require(HarnessJson.Hash(freeze) == freezeHash, "Held-out evaluator changed the frozen family.");
        var resultSummary = Summarize(plan, context, freeze, searches, evidence);
        Require(resultSummary.Fights <= plan.MaximumFights && resultSummary.HeldoutFights == ordinal, "Changed study accounting.");
        save("summary.json", resultSummary); return resultSummary;
    }

    private static bool Complete(TowerProposalRacingReport report, TowerProposalRacingPlan plan) =>
        report.Version == plan.Version && report.PlanHash == HarnessJson.Hash(plan) && report.PolicyHash == HarnessJson.Hash(plan.Policy)
        && report.SelectionPolicyVersion == plan.SelectionPolicyVersion
        && report.Evaluation.Status == "Complete" && report.Evaluation.ChargedEvaluations == 528
        && report.Batches.Count == 2 && report.Evaluation.Panels.Count == (TowerProposalPolicies.UsesBenchmarkValidation(plan.Version) ? 6 : 5)
        && (!TowerProposalPolicies.UsesBenchmarkValidation(plan.Version)
            || report.Evaluation.ValidationFreeze is not null && report.Evaluation.ValidationDecision is not null
                && report.Evaluation.RawSelectedId == report.Evaluation.ValidationDecision.SelectedId)
        && report.Evaluation.RawSelectedId is not null;

    internal static ProposalStudyResult Summarize(TowerProposalComparisonPlan plan, TowerProposalContext context,
        ProposalStudyFreeze freeze, IReadOnlyList<TowerProposalComparisonSearch> searches, IReadOnlyList<ProposalStudyEvidence> evidence)
    {
        ValidateDesign(plan);
        Require(freeze.Version == plan.Version, "Changed study freeze version.");
        Require(searches.Count == 12 && freeze.Families.Select(f => f.Root).SequenceEqual(Enumerable.Range(1, 12)), "Retain all twelve roots.");
        var references = context.Scope.Starts.Select(s => TowerBossDiscovery.RecipeHash(TowerBossDiscovery.Scenario(context.Scope,
            context.Scope.Contexts.Single().Id, s.Party, []).Party)).ToHashSet(StringComparer.Ordinal);
        var roots = new List<ProposalStudyRoot>();
        foreach (var f in freeze.Families)
        {
            ProposalStudyMember Member(string role) => f.Members.Single(m => m.Roles.Contains(role));
            bool[] Wins(ProposalStudyMember member)
            {
                var rows = evidence.Single(e => e.Root == f.Root && e.RecipeHash == member.RecipeHash).Observations;
                Require(rows.Count == 256 && rows.Select(r => r.Request.Seed).SequenceEqual(f.Seeds), "Changed held-out panel.");
                return rows.Select(r => r.Outcome.Outcome == BattleOutcome.Victory).ToArray();
            }
            var a = Member("control"); var b = Member("candidate"); var anchor = Member("benchmark");
            var aw = Wins(a); var bw = Wins(b); var rw = Wins(anchor);
            var d = bw.Zip(aw).Select(p => (p.First ? 1d : 0) - (p.Second ? 1d : 0)).ToArray();
            var g = bw.Zip(rw).Select(p => (p.First ? 1d : 0) - (p.Second ? 1d : 0)).ToArray();
            var dm = d.Average(); var gm = g.Average();
            double Cov(double[] x, double[] y, double xm, double ym) => x.Zip(y).Sum(p => (p.First-xm)*(p.Second-ym)) / (256d*255);
            var search = searches[f.Root - 1];
            roots.Add(new(f.Root, a.Party.Id, b.Party.Id, anchor.Party.Id, a.RecipeHash == b.RecipeHash,
                !references.Contains(b.RecipeHash), search.Control.Batches.Zip(search.Candidate!.Batches)
                    .Select(p => p.First.Candidates.Zip(p.Second.Candidates).Count(c => c.First.Id != c.Second.Id)).ToArray(),
                aw.Count(w => w), bw.Count(w => w), rw.Count(w => w), d.Count(x => x > 0), d.Count(x => x < 0),
                g.Count(x => x > 0), g.Count(x => x < 0), dm, gm, Math.Sqrt(Cov(d, d, dm, dm)), Math.Sqrt(Cov(g, g, gm, gm)), Cov(d, g, dm, gm)));
        }
        var method = Interval(roots.Select(r => r.MethodDifference).ToArray());
        var benchmark = Interval(roots.Select(r => r.BenchmarkDifference).ToArray());
        var differing = roots.Count(r => !r.Identical); var promising = roots.Count(r => r.Novel && r.BenchmarkDifference >= plan.Analysis.PromisingNovelGainAtLeast);
        var decision = Decide(plan.Analysis, method.Mean, benchmark.Mean, differing, promising);
        var heldout = evidence.Sum(e => e.Observations.Count);
        static double Median(IEnumerable<double> values) { var sorted = values.Order().ToArray(); return (sorted[5]+sorted[6])/2; }
        var validation = plan.Version is ValidationVersion or PreservationVersion or AlliedActionVersion or LoadoutPlacementVersion or NominationVersion ? searches.Select(s => s.Candidate!.Evaluation.ValidationDecision
            ?? throw new InvalidDataException("Missing completed validation decision.")).ToArray() : null;
        var controlValidation = plan.Version is PreservationVersion or AlliedActionVersion or LoadoutPlacementVersion or NominationVersion ? searches.Select(s => s.Control.Evaluation.ValidationDecision
            ?? throw new InvalidDataException("Missing completed control validation decision.")).ToArray() : null;
        return new(plan.Version, "Verified", decision, HarnessJson.Hash(plan), HarnessJson.Hash(freeze), SearchFights+heldout, SearchFights,
            heldout, differing, roots.Count(r => r.Novel), promising, roots, method, benchmark,
            Median(roots.Select(r => r.MethodDifference)), roots.Min(r => r.MethodDifference),
            Median(roots.Select(r => r.BenchmarkDifference)), roots.Min(r => r.BenchmarkDifference), plan.Interpretation,
            validation is null ? null : new(validation.Count(d => d.Passed), validation.Count(d => !d.Passed), validation),
            controlValidation is null ? null : new(controlValidation.Count(d => d.Passed), controlValidation.Count(d => !d.Passed), controlValidation));
    }

    internal static string Decide(TowerProposalComparisonAnalysis a, double method, double benchmark, int differing, int promising) =>
        a.DecisionOrder == TowerProposalComparison.LoadoutPlacementDecisionOrder
            ? method <= a.AbandonMethodAtMost || benchmark <= a.AbandonBenchmarkAtMost ? "AbandonThisConfiguration"
                : differing == 0 ? "NoObservedOutputDifferentiation"
                : method >= a.GoMethodAtLeast && benchmark >= a.GoBenchmarkAtLeast && differing >= a.GoDifferingRootsAtLeast
                    && promising >= a.GoPromisingNovelRootsAtLeast ? "LargerFreshEvaluationWarranted" : "Inconclusive"
            : a.DecisionOrder == TowerProposalComparison.SelectorDecisionOrder
            ? method <= a.AbandonMethodAtMost || benchmark <= a.AbandonBenchmarkAtMost ? "AbandonThisConfiguration"
                : differing == 0 ? "NoObservedOutputDifferentiation"
                : method >= a.GoMethodAtLeast && benchmark >= a.GoBenchmarkAtLeast && differing >= a.GoDifferingRootsAtLeast
                    ? "LargerFreshEvaluationWarranted" : "Inconclusive"
            :
        method <= a.AbandonMethodAtMost || benchmark <= a.AbandonBenchmarkAtMost && promising < a.AbandonPromisingNovelRootsBelow
            ? "AbandonThisConfiguration" : differing == 0 ? "NoObservedOutputDifferentiation"
            : method >= a.GoMethodAtLeast && benchmark >= a.GoBenchmarkAtLeast && differing >= a.GoDifferingRootsAtLeast
                && promising >= a.GoPromisingNovelRootsAtLeast ? "LargerFreshEvaluationWarranted" : "Inconclusive";

    private static AdaptiveRootInterval Interval(double[] values)
    {
        var mean = values.Average(); var sd = Math.Sqrt(values.Sum(x => (x-mean)*(x-mean))/11);
        var margin = 2.200985160082949 * sd / Math.Sqrt(12);
        return new(mean, sd, mean-margin, mean+margin);
    }
}
