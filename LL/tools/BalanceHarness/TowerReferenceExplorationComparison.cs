using Domain.Models.Combat;
using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record ExplorationOutput(string Generator, string Selector, BossFinalist Finalist, string RecipeHash, TowerScenario Scenario,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Pipeline = null);
public sealed record ExplorationArm(BossGenerationResult? Discovery, IReadOnlyList<BossDiscoveryMeasurement> Selection, ExplorationOutput Output,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] PracticalScreeningResult? Screening = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerAdaptiveRacingReport? Adaptive = null);
public sealed record ExplorationSearch(int Restart, ExplorationArm Baseline, ExplorationArm Candidate);
public sealed record ExplorationMember(string RecipeHash, string PartyId, IReadOnlyList<string> ReferenceIds, bool Baseline, bool Candidate, TowerScenario Scenario);
public sealed record ExplorationFamily(int Restart, IReadOnlyList<ExplorationMember> Members, IReadOnlyList<int> Seeds);
public sealed record ExplorationFreeze(string Version, string BindingHash, string SearchHash, string AttemptsHash,
    int CompletedAttempts, IReadOnlyList<ExplorationSearch> Searches, IReadOnlyList<ExplorationFamily> Families);
public sealed record ExplorationEvidence(int Restart, string RecipeHash, IReadOnlyList<TowerBalanceTrial> Trials);
public sealed record ExplorationStudy(string Version, ExplorationFreeze Freeze, IReadOnlyList<ExplorationEvidence> Evidence);
public sealed record ExplorationPair(int Restart, string BaselineParty, string CandidateParty, bool Identical,
    int BaselineWins, int CandidateWins, int Gains, int Losses, double Difference, int RecipeCount);
public sealed record ExplorationView(int Restart, string Generator, string SelectedPartyId, string Interpretation,
    int IntervalFamily, IReadOnlyList<TowerDiagnosticRate> Rates, IReadOnlyList<TowerPracticalContrast> Contrasts,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Pipeline = null);
public sealed record ExplorationResult(string Version, string Status, string Decision, string BoundVersion,
    IReadOnlyList<ExplorationPair> Pairs, int ActiveRestarts, int NetWins, int PositiveRestarts,
    int Denominator, double MeanDifference, double Depletion, double Margin, double LowerBound, int Fights,
    IReadOnlyList<ExplorationView> DescriptiveViews,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] AdaptivePilotMetrics? Pilot = null);
internal sealed class ExplorationIncompleteException(string message) : Exception(message);

/// <summary>Two independent searches per pair; all outputs freeze before shared-control confirmation.</summary>
public static partial class TowerReferenceExplorationComparison
{
    public const string Version = "tower-reference-exploration-comparison-v1";
    public const string OffsetVersion = "tower-reference-exploration-offset-comparison-v1";
    public const string ScreeningVersion = "tower-practical-fresh-screening-comparison-v1";
    internal sealed record Protocol(string Generator, string PlanHash, string SupportDecision, string NegativeDecision, bool Screening = false, bool Adaptive = false)
    { internal int ValuesPerRestart => Adaptive ? 113 : Screening ? 49 : 41; }
    internal static Protocol Policy(string version) => version switch {
        Version => new(TowerReferenceExploration.Version, PlanHash, "SupportsReferenceExplorationForFrozenOutputs", "DoNotPromoteReferenceExploration"),
        OffsetVersion => new(TowerReferenceExploration.OffsetVersion, "7e6203052cd8a1984b80bbe34dbb1e4b21fe3f882c8ab314e21987d289a25c90",
            "SupportsReferenceExplorationOffsetForFrozenOutputs", "DoNotPromoteReferenceExplorationOffset"),
        ScreeningVersion => new(TowerSuppliedCompositionSearch.ThreeReferenceVersion, "13fc98e45afb7a96403918ffe0233fd1dcb375a52fb9446b63053911cc94c263",
            "SupportsFreshScreeningForFrozenOutputs", "DoNotPromoteFreshScreening", true),
        TowerAdaptiveRacingComparison.Version => new(TowerAdaptiveRacing.Version, TowerAdaptiveRacingComparison.PlanHash,
            "LargerFreshEvaluationWarranted", "RetainBaselineAndBenchmark", Adaptive: true),
        _ => throw new InvalidDataException("Unknown reference exploration comparison protocol.") };
    public const string BoundVersion = "conditional-range-hoeffding-depletion-v1";
    internal const int Restarts = 12, Samples = 1000, SearchValues = 492, AssignedValues = 12492,
        EntropyWords = 16384, MaximumHistory = 983616, SearchFights = 12672, MaximumFights = 72672;
    internal const int MaximumSeconds = 10800, PriorSeconds = 600, ExecutionSeconds = 10200, NativeSeconds = 10080;
    internal const long MaximumBytes = 6442450944, PriorBytes = 536870912, ExecutionBytes = 5905580032, NativeBytes = 5637144576;
    internal const string PrimaryReference = "confirmed-399bc7760fb0cf790a5d8ac4";
    internal static readonly string[] ReferenceParties = [
        "399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b",
        "8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50",
        "96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c"];
    internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool ok, string reason)
    { if (!ok) throw new InvalidDataException(reason); }
    internal static int SearchCount(string version) => Restarts * Policy(version).ValuesPerRestart;
    internal static int AssignedCount(string version) => SearchCount(version) + Restarts * SampleCount(version);
    internal static int SampleCount(string version) => Policy(version).Adaptive ? 256 : Samples;
    internal static int FightLimit(string version) => SearchFights + Restarts * 5 * SampleCount(version);
    internal static int ArmLimit(string version) => 528 + 4 * SampleCount(version);
    internal static int ExecutionSecondsFor(string version) => Policy(version).Adaptive ? MaximumSeconds : ExecutionSeconds;
    internal static int NativeSecondsFor(string version) => ExecutionSecondsFor(version) - 120;
    internal static long ExecutionBytesFor(string version) => Policy(version).Adaptive ? MaximumBytes : ExecutionBytes;
    internal static long NativeBytesFor(string version) => ExecutionBytesFor(version) - 268435456;
    internal static int PriorSecondsFor(string version) => Policy(version).Adaptive ? 0 : PriorSeconds;
    internal static long PriorBytesFor(string version) => Policy(version).Adaptive ? 0 : PriorBytes;
    internal static string? Pipeline(string version, bool candidate) => Policy(version).Screening
        ? candidate ? TowerPracticalScreening.Version : TowerPracticalScreening.BaselineVersion : null;
    internal static int[] ScreeningPanel(IReadOnlyList<int> values, int restart, string version)
    {
        Require(Policy(version).Screening && values.Count == AssignedCount(version) && restart is >= 0 and < Restarts,
            "Screening panels require their explicit comparison version.");
        return values.Skip(restart * Policy(version).ValuesPerRestart + 9).Take(8).ToArray();
    }

    internal static TowerBossDiscoveryDefinition Bind(TowerBossDiscoveryDefinition template, IReadOnlyList<int> values, int restart, bool candidate, string version = Version)
    {
        var policy = Policy(version);
        Require(values.Count == AssignedCount(version) && values.Distinct().Count() == AssignedCount(version) && restart is >= 0 and < Restarts
            && !values.Intersect(template.ExcludedCombatSeeds).Any(), "Invalid paired exploration allocation.");
        var search = values.Skip(restart * policy.ValuesPerRestart).Take(policy.ValuesPerRestart).ToArray();
        var result = template with { Id = "reference-exploration-pair-" + (restart + 1),
            Generation = template.Generation with { Seeds = [search[0]], PolicyVersion = candidate && !policy.Adaptive
                ? Policy(version).Generator : TowerSuppliedCompositionSearch.ThreeReferenceVersion },
            Stages = template.Stages with { Schedules = template.Stages.Schedules.ToDictionary(p => p.Key,
                _ => new BossDiscoverySchedule(search.Skip(1).Take(candidate && policy.Screening ? 4 : 8).ToArray(),
                    search.Skip(policy.Screening ? 17 : 9).Take(32).ToArray(), Panel(values, restart, version), [])) } };
        var cost = TowerBossDiscovery.Validate(result);
        var screening = candidate && policy.Screening ? TowerPracticalScreening.ScreeningFights : 0;
        Require(cost.Discovery + screening == 368 && cost.Selection == 160 && cost.Total + screening == ArmLimit(version)
            && result.MaximumBattles == ArmLimit(version), "Changed logical arm ceiling.");
        if (screening > 0) TowerPracticalScreening.Validate(result, ScreeningPanel(values, restart, version));
        return result;
    }
    internal static int[] Panel(IReadOnlyList<int> values, int restart, string version = Version)
        => values.Skip(SearchCount(version) + restart * SampleCount(version)).Take(SampleCount(version)).ToArray();

    internal static ExplorationFamily Family(TowerBossDiscoveryDefinition d, ExplorationSearch search, IReadOnlyList<int> panel, string version = Version)
    {
        var outputs = new[] { search.Baseline.Output, search.Candidate.Output };
        Require(outputs[0].Generator == TowerSuppliedCompositionSearch.ThreeReferenceVersion
            && outputs[1].Generator == Policy(version).Generator
            && outputs[0].Pipeline == Pipeline(version, false) && outputs[1].Pipeline == Pipeline(version, true)
            && search.Baseline.Screening is null && search.Baseline.Adaptive is null
            && search.Baseline.Discovery is not null
            && (search.Candidate.Discovery is null) == Policy(version).Adaptive
            && (search.Candidate.Adaptive is not null) == Policy(version).Adaptive
            && (search.Candidate.Adaptive is null || search.Candidate.Adaptive.Evaluation is { Status: "Complete", ChargedEvaluations: 528 }
                && search.Candidate.Adaptive.Evaluation.RawSelectedId == outputs[1].Finalist.Party.Id)
            && (search.Candidate.Screening is not null) == Policy(version).Screening
            && outputs.All(o => o.Selector == TowerBossStudyPolicy.IncumbentTieVersion && o.Finalist.Primary
                && o.RecipeHash == TowerBossDiscovery.RecipeHash(o.Scenario.Party)
                && HarnessJson.Hash(o.Scenario) == HarnessJson.Hash(TowerBossDiscovery.Scenario(d, d.Contexts.Single().Id, o.Finalist.Party, [])))
            && panel.Count == SampleCount(version) && panel.Distinct().Count() == SampleCount(version), "Changed selected output or confirmation panel.");
        var rows = d.Starts.Select(s => (Party: s.Party.Id, Reference: (string?)s.ReferenceId,
            Scenario: TowerBossDiscovery.Scenario(d, d.Contexts.Single().Id, s.Party, [])))
            .Concat(outputs.Select(o => (Party: o.Finalist.Party.Id, Reference: (string?)null, Scenario: o.Scenario)));
        var members = rows.GroupBy(r => TowerBossDiscovery.RecipeHash(r.Scenario.Party), StringComparer.Ordinal).Select(g => {
            Require(g.Select(r => HarnessJson.Hash(r.Scenario)).Distinct().Count() == 1
                && g.Select(r => r.Party).Distinct().Count() == 1, "Recipe identity collision or changed encounter scope.");
            return new ExplorationMember(g.Key, g.First().Party, g.Where(r => r.Reference is not null).Select(r => r.Reference!).Order(StringComparer.Ordinal).ToArray(),
                outputs[0].RecipeHash == g.Key, outputs[1].RecipeHash == g.Key, g.First().Scenario);
        }).OrderBy(m => m.RecipeHash, StringComparer.Ordinal).ToArray();
        Require(members.Length is >= 3 and <= 5 && members.Count(m => m.Baseline) == 1 && members.Count(m => m.Candidate) == 1
            && members.SelectMany(m => m.ReferenceIds).Order().SequenceEqual(d.References.Select(r => r.Id).Order()), "Incomplete physical confirmation union.");
        return new(search.Restart, members, panel);
    }

    internal static async Task<ExplorationStudy> Execute(TowerBossDiscoveryDefinition template, ExplorationReservation allocation,
        BossGenerationMechanics mechanics, TowerBossDiscoveryRun.Battle battle, Action<string, object> save,
        Action<bool> attempt, Func<string> attemptsHash, CancellationToken ct)
    {
        var version = allocation.Version; ValidateTemplate(template, version);
        var searches = new List<ExplorationSearch>(); var completed = 0; var started = 0; var frozen = false;
        async Task<(LoadoutTrial Trial, TowerBattleReport Report)> Fight(string arm, string stage, TowerScenario scenario, int seed, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Require(started == completed && started < FightLimit(version)
                && (stage != "confirmation" || frozen && completed >= SearchFights), "Attempt ceiling or global confirmation barrier.");
            attempt(false); started++; var value = await battle(arm, stage, scenario, seed, token);
            Require(value.Trial.Stage == stage && value.Trial.Seed == seed && value.Trial.Recipe == HarnessJson.Hash(scenario)
                && value.Report.Battle.Seed == seed && value.Report.Battle.ScenarioId == scenario.Id
                && Enum.IsDefined(value.Report.Battle.Summary.ContentOutcome)
                && value.Report.Succeeded == (value.Report.Battle.Summary.ContentOutcome == BattleOutcome.Victory), "Inconsistent trial evidence.");
            attempt(true); completed++; return value;
        }
        for (var restart = 0; restart < Restarts; restart++)
        {
            var arms = new List<ExplorationArm>();
            foreach (var candidate in new[] { false, true })
            {
                var d = Bind(template, allocation.Selected, restart, candidate, version);
                if (candidate && Policy(version).Adaptive)
                {
                    var adaptive = await TowerAdaptiveRacingComparison.Search(d, allocation, restart, mechanics, Fight, save, ct);
                    Require(completed == (restart + 1) * 1056, "Incomplete adaptive search accounting.");
                    arms.Add(adaptive); continue;
                }
                var screened = candidate && Policy(version).Screening;
                var input = TowerBossImprovement.Inputs(d); var arm = $"pair-{restart + 1:D2}-" + (candidate ? "candidate" : "baseline");
                // Distinct namespaces prevent shared initial recipes from reusing the other search's measurements.
                Task<(LoadoutTrial, TowerBattleReport)> Measure(string _, string stage, TowerScenario scenario, int seed, CancellationToken token)
                    => Fight(arm + "/" + stage, stage, scenario, seed, token);
                var discovery = await TowerBossImprovement.ExecuteBattlesAsync(d, input, mechanics, Measure, ct);
                save(arm + "-discovery.json", discovery);
                if (discovery.Status != "Complete") throw new ExplorationIncompleteException("Incomplete discovery; no replacement restart.");
                Require(discovery.Arms.Count == 1 && discovery.Arms[0].Evaluations.Count == 46 && discovery.DiscoveryShortlist.Count == 5
                    && d.Starts.All(s => discovery.DiscoveryShortlist.Any(p => p.Id == s.Party.Id))
                    && completed == restart * 1056 + (candidate ? 528 : 0) + (screened ? 184 : 368), "Incomplete search or changed discovery accounting.");
                PracticalScreeningResult? screening = null;
                IReadOnlyList<PartyChoice> nominees = discovery.DiscoveryShortlist;
                if (screened)
                {
                    var seeds = ScreeningPanel(allocation.Selected, restart, version);
                    var membership = TowerPracticalScreening.Freeze(d, discovery, seeds);
                    save(arm + "-screening-freeze.json", membership);
                    var screeningInput = input with { DiscoverySeeds = d.Stages.Schedules.ToDictionary(p => p.Key, _ => (IReadOnlyList<int>)seeds) };
                    var measured = new List<BossDiscoveryMeasurement>();
                    foreach (var party in membership.Candidates)
                        measured.Add(await TowerBossDiscoveryRun.Measure(d, screeningInput, party, "screening", Measure, ct, "screening"));
                    Require(completed == restart * 1056 + 528 + 368, "Incomplete screening accounting.");
                    screening = TowerPracticalScreening.Nominate(d, discovery, seeds, membership, measured);
                    save(arm + "-screening.json", screening); // Final membership is durable before selection.
                    nominees = screening.Nominees;
                }
                var selectionInput = input with { DiscoverySeeds = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection) };
                var selection = new List<BossDiscoveryMeasurement>();
                foreach (var party in nominees)
                    selection.Add(await TowerBossDiscoveryRun.Measure(d, selectionInput, party, "selection", Measure, ct, "selection"));
                var selected = TowerBossStudyPolicy.Select(d, mechanics, nominees, selection).Single();
                var scenario = TowerBossDiscovery.Scenario(d, d.Contexts.Single().Id, selected.Party, []);
                var output = new ExplorationOutput(d.Generation.PolicyVersion, d.Stages.SelectionPolicyVersion, selected,
                    TowerBossDiscovery.RecipeHash(scenario.Party), scenario, Pipeline(version, candidate));
                var row = new ExplorationArm(discovery, selection, output, screening); save(arm + ".json", row); arms.Add(row);
            }
            searches.Add(new(restart + 1, arms[0], arms[1]));
        }
        Require(completed == SearchFights, "Incomplete global search.");
        var families = searches.Select(s => Family(Bind(template, allocation.Selected, s.Restart - 1, false, version), s, Panel(allocation.Selected, s.Restart - 1, version), version)).ToArray();
        var freeze = new ExplorationFreeze(version, HarnessJson.Hash(allocation), HarnessJson.Hash(searches), attemptsHash(), completed, searches, families);
        save("outputs-freeze.json", freeze); frozen = true;
        var evidence = new List<ExplorationEvidence>();
        foreach (var family in families)
        foreach (var member in family.Members)
        {
            var scenario = member.Scenario with { Seeds = family.Seeds }; var trials = new List<TowerBalanceTrial>();
            foreach (var seed in family.Seeds)
            {
                var value = await Fight($"pair-{family.Restart:D2}-confirmation", "confirmation", scenario, seed, ct);
                trials.Add(new(seed, value.Report.Battle.Summary.ContentOutcome));
            }
            evidence.Add(new(family.Restart, member.RecipeHash, trials));
        }
        Require(started == completed && completed == SearchFights + SampleCount(version) * families.Sum(f => f.Members.Count), "Incomplete physical fight accounting.");
        var study = new ExplorationStudy(version, freeze, evidence); save("study.json", study); return study;
    }

    internal static ExplorationResult Assess(TowerBossDiscoveryDefinition template, ExplorationStudy study, ExplorationReservation allocation)
    {
        var version = allocation.Version; ValidateTemplate(template, version); var f = study.Freeze;
        var samples = SampleCount(version);
        Require(study.Version == version && f.Version == version && f.CompletedAttempts == SearchFights
            && f.BindingHash == HarnessJson.Hash(allocation) && f.SearchHash == HarnessJson.Hash(f.Searches)
            && TowerContractJson.Hash(f.AttemptsHash) && f.Searches.Select(s => s.Restart).SequenceEqual(Enumerable.Range(1, Restarts)), "Changed global freeze.");
        var families = f.Searches.Select(s => Family(Bind(template, allocation.Selected, s.Restart - 1, false, version), s, Panel(allocation.Selected, s.Restart - 1, version), version)).ToArray();
        Require(HarnessJson.Hash(f.Families) == HarnessJson.Hash(families)
            && study.Evidence.Select(e => (e.Restart, e.RecipeHash)).SequenceEqual(families.SelectMany(g => g.Members.Select(m => (g.Restart, m.RecipeHash)))), "Changed physical confirmation family.");
        var pairs = new List<ExplorationPair>(); var views = new List<ExplorationView>();
        foreach (var family in families)
        {
            var wins = family.Members.ToDictionary(m => m.RecipeHash, m => {
                var row = study.Evidence.Single(e => e.Restart == family.Restart && e.RecipeHash == m.RecipeHash);
                Require(row.Trials.Select(t => t.Seed).SequenceEqual(family.Seeds) && row.Trials.All(t => Enum.IsDefined(t.Outcome)), "Unpaired confirmation evidence.");
                return row.Trials.Select(t => t.Outcome == BattleOutcome.Victory).ToArray();
            });
            var a = family.Members.Single(m => m.Baseline); var b = family.Members.Single(m => m.Candidate);
            var gained = wins[a.RecipeHash].Zip(wins[b.RecipeHash]).Count(p => !p.First && p.Second);
            var lost = wins[a.RecipeHash].Zip(wins[b.RecipeHash]).Count(p => p.First && !p.Second);
            pairs.Add(new(family.Restart, a.PartyId, b.PartyId, a.RecipeHash == b.RecipeHash, wins[a.RecipeHash].Count(w => w),
                wins[b.RecipeHash].Count(w => w), gained, lost, (gained - lost) / (double)samples, family.Members.Count));
            foreach (var candidate in new[] { false, true })
            {
                var selected = candidate ? b : a; var selectedWins = wins[selected.RecipeHash];
                var members = family.Members.Where(m => m.ReferenceIds.Count > 0 || m.RecipeHash == selected.RecipeHash).ToArray();
                var rates = members.Select(m => new TowerDiagnosticRate(m.PartyId, wins[m.RecipeHash].Count(w => w),
                    TowerBalanceEvaluator.Wilson(wins[m.RecipeHash].Count(w => w), samples, 10)!)).ToArray();
                var contrasts = template.References.Select(reference => {
                    var anchor = wins[family.Members.Single(m => m.ReferenceIds.Contains(reference.Id)).RecipeHash];
                    var g = selectedWins.Zip(anchor).Count(p => p.First && !p.Second); var l = selectedWins.Zip(anchor).Count(p => !p.First && p.Second);
                    var gi = TowerBalanceEvaluator.Wilson(g, samples, 10)!; var li = TowerBalanceEvaluator.Wilson(l, samples, 10)!;
                    return new TowerPracticalContrast(reference.Id, g, l, (g-l)/(double)samples, gi.Lower-li.Upper, gi.Upper-li.Lower);
                }).ToArray();
                views.Add(new(family.Restart, candidate ? Policy(version).Generator : TowerSuppliedCompositionSearch.ThreeReferenceVersion,
                    selected.PartyId, "DescriptiveOnlyNoTeamAdoption", 10, rates, contrasts, Pipeline(version, candidate)));
            }
        }
        return (Policy(version).Adaptive ? TowerAdaptiveRacingComparison.Summarize(template, study, pairs)
            : Summarize(pairs, template.ExcludedCombatSeeds.Count, version)) with { DescriptiveViews = views };
    }

    internal static ExplorationResult Summarize(IReadOnlyList<ExplorationPair> pairs, int historicalCount, string version = Version)
    {
        Require(!Policy(version).Adaptive, "Adaptive pilots require the absolute benchmark endpoint.");
        Require(pairs.Count == Restarts && pairs.Select(p => p.Restart).SequenceEqual(Enumerable.Range(1, Restarts))
            && historicalCount is >= 0 and <= MaximumHistory && pairs.All(p => p.RecipeCount is >= 3 and <= 5
                && p.Identical == (p.BaselineParty == p.CandidateParty) && (!p.Identical || p.RecipeCount <= 4 && p.Gains == 0 && p.Losses == 0)
                && p.BaselineWins is >= 0 and <= Samples && p.CandidateWins is >= 0 and <= Samples
                && p.Gains >= 0 && p.Losses >= 0 && p.Gains + p.Losses <= Samples && p.Gains <= p.CandidateWins && p.Gains <= Samples - p.BaselineWins
                && p.Losses <= p.BaselineWins && p.Losses <= Samples - p.CandidateWins && p.CandidateWins - p.BaselineWins == p.Gains - p.Losses
                && p.Difference == (p.Gains - p.Losses)/(double)Samples), "Invalid comparison counts.");
        var k = pairs.Count(p => !p.Identical); var net = pairs.Sum(p => p.Gains-p.Losses); var positive = pairs.Count(p => p.Gains > p.Losses);
        var n = k * Samples; const int denominator = Restarts * Samples;
        var mean = net / (double)denominator; var population = 4294967296d - historicalCount - SearchCount(version);
        var depletion = n * (n-1d) / (population * denominator);
        var margin = Math.Sqrt(2d * n * Math.Log(20)) / denominator + depletion; var lower = Math.Max(-k/(double)Restarts, mean-margin);
        var decision = k == 0 ? "NoSelectedOutputDifferences" : net >= 600 && lower > 0 && positive >= 7
            ? Policy(version).SupportDecision : Policy(version).NegativeDecision;
        _ = Policy(version);
        return new(version, "Verified", decision, BoundVersion, pairs, k, net, positive, denominator, mean, depletion, margin, lower,
            SearchFights + Samples * pairs.Sum(p => p.RecipeCount), []);
    }
}
