using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record IncumbentTieOutput(string Selector, BossFinalist Finalist, string RecipeHash, TowerScenario Scenario);
public sealed record IncumbentTieSearch(int Restart, BossGenerationResult Discovery,
    IReadOnlyList<BossDiscoveryMeasurement> Selection, IncumbentTieOutput Baseline, IncumbentTieOutput Candidate);
public sealed record IncumbentTieFreeze(string Version, string BindingHash, string SearchHash, string AttemptsHash,
    int CompletedAttempts, IReadOnlyList<IncumbentTieSearch> Searches, IReadOnlyList<int> ActiveRestarts);
public sealed record IncumbentTieEvidence(int Restart, IReadOnlyList<TowerBalanceTrial> Baseline, IReadOnlyList<TowerBalanceTrial> Candidate);
public sealed record IncumbentTieStudy(string Version, IncumbentTieFreeze Freeze, IReadOnlyList<IncumbentTieEvidence> Evidence);
public sealed record IncumbentTiePair(int Restart, string BaselineParty, string CandidateParty, bool Identical,
    int? BaselineWins, int? CandidateWins, int Gains, int Losses, double Difference);
public sealed record IncumbentTieResult(string Version, string Status, string Decision, string BoundVersion,
    IReadOnlyList<IncumbentTiePair> Pairs, int ActiveRestarts, int NetWins, int PositiveRestarts,
    int Denominator, double MeanDifference, double Depletion, double Margin, double LowerBound, int Fights);
internal sealed class IncumbentTieIncompleteException(string message) : Exception(message);

/// <summary>One common search per restart, two real selectors, then a global confirmation barrier.</summary>
public static partial class TowerIncumbentTieComparison
{
    public const string Version = "tower-incumbent-tie-comparison-v1";
    public const string BoundVersion = "conditional-range-hoeffding-depletion-v1";
    internal const int Restarts = 24, Samples = 1000, SearchValues = 984, AssignedValues = 24984,
        EntropyWords = 32768, MaximumHistory = 967232, SearchFights = 11904, MaximumFights = 59904;
    internal const int MaximumSeconds = 4500, NativeSeconds = 4440;
    internal const long MaximumBytes = 4294967296, NativeBytes = 4160749568;
    internal const string PrimaryReference = "confirmed-399bc7760fb0cf790a5d8ac4";
    internal const string PrimaryParty = "399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b";
    internal const string SecondParty = "8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50";
    internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool ok, string reason)
    { if (!ok) throw new InvalidDataException(reason); }

    internal static TowerBossDiscoveryDefinition Bind(TowerBossDiscoveryDefinition template, IReadOnlyList<int> values, int restart, bool candidate)
    {
        Require(values.Count == AssignedValues && values.Distinct().Count() == AssignedValues && restart is >= 0 and < Restarts
            && !values.Intersect(template.ExcludedCombatSeeds).Any(), "Invalid comparison allocation.");
        var search = values.Skip(restart * 41).Take(41).ToArray();
        var result = template with { Id = "incumbent-tie-" + (restart + 1),
            Generation = template.Generation with { Seeds = [search[0]] },
            Stages = template.Stages with { SelectionPolicyVersion = candidate ? TowerBossStudyPolicy.IncumbentTieVersion : TowerBossStudyPolicy.ZeroWinVersion,
                SelectionPrimaryReferenceId = candidate ? template.Starts[0].ReferenceId : null,
                Schedules = template.Stages.Schedules.ToDictionary(p => p.Key, _ => new BossDiscoverySchedule(
                    search.Skip(1).Take(8).ToArray(), search.Skip(9).ToArray(), Panel(values, restart), [])) } };
        TowerBossDiscovery.Validate(result); return result;
    }

    internal static int[] Panel(IReadOnlyList<int> values, int restart) => values.Skip(SearchValues + restart * Samples).Take(Samples).ToArray();

    internal static IncumbentTieSearch Select(int restart, TowerBossDiscoveryDefinition baseline, TowerBossDiscoveryDefinition candidate,
        BossGenerationMechanics mechanics, BossGenerationResult discovery, IReadOnlyList<BossDiscoveryMeasurement> selection)
    {
        Require(discovery.Status == "Complete" && discovery.Arms.Count == 1 && discovery.Arms[0].Evaluations.Count == 46
            && discovery.DiscoveryShortlist.Count == 4 && selection.Count == 4, "Incomplete search; no replacement restart.");
        Require(HarnessJson.Hash(candidate with { Stages = baseline.Stages }) == HarnessJson.Hash(baseline)
            && baseline.Stages.SelectionPolicyVersion == TowerBossStudyPolicy.ZeroWinVersion && baseline.Stages.SelectionPrimaryReferenceId is null
            && candidate.Stages.SelectionPolicyVersion == TowerBossStudyPolicy.IncumbentTieVersion
            && candidate.Stages.SelectionPrimaryReferenceId == baseline.Starts[0].ReferenceId
            && HarnessJson.Hash(candidate.Stages with { SelectionPolicyVersion = baseline.Stages.SelectionPolicyVersion, SelectionPrimaryReferenceId = null })
                == HarnessJson.Hash(baseline.Stages), "Selectors must receive the same search and designated primary.");
        IncumbentTieOutput Output(TowerBossDiscoveryDefinition d)
        {
            var selected = TowerBossStudyPolicy.Select(d, mechanics, discovery.DiscoveryShortlist, selection).Single();
            var scenario = TowerBossDiscovery.Scenario(d, d.Contexts.Single().Id, selected.Party, []);
            return new(d.Stages.SelectionPolicyVersion, selected, TowerBossDiscovery.RecipeHash(scenario.Party), scenario);
        }
        var a = Output(baseline); var b = Output(candidate);
        if (a.RecipeHash != b.RecipeHash)
        {
            var wins = selection.ToDictionary(r => r.Id, r => r.Cells.Sum(c => c.Clears.Count(w => w)));
            var maximum = wins.Values.Max();
            Require(maximum > 0 && wins.Values.Count(w => w == maximum) > 1 && wins[b.Finalist.Party.Id] == maximum
                && wins[a.Finalist.Party.Id] == maximum && b.Finalist.Party.Id == baseline.Starts[0].Party.Id,
                "Selector difference is not the designated positive maximum tie.");
        }
        return new(restart + 1, discovery, selection, a, b);
    }

    internal static async Task<IncumbentTieStudy> Execute(TowerBossDiscoveryDefinition template, IncumbentTieReservation allocation,
        BossGenerationMechanics mechanics, TowerBossDiscoveryRun.Battle battle, Action<string, object> save,
        Action<bool> attempt, Func<string> attemptsHash, CancellationToken ct)
    {
        ValidateTemplate(template);
        Require(allocation.Version == Version, "Unknown comparison allocation.");
        var searches = new List<IncumbentTieSearch>(); var completed = 0; var started = 0; var frozen = false;
        async Task<(LoadoutTrial Trial, TowerBattleReport Report)> Fight(string arm, string stage, TowerScenario scenario, int seed, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Require(started == completed && started < MaximumFights
                && (stage != "confirmation" || frozen && completed >= SearchFights), "Attempt ceiling or confirmation barrier.");
            attempt(false); started++; var value = await battle(arm, stage, scenario, seed, token);
            Require(value.Trial.Stage == stage && value.Trial.Seed == seed && value.Trial.Recipe == HarnessJson.Hash(scenario)
                && value.Report.Battle.Seed == seed && value.Report.Battle.ScenarioId == scenario.Id
                && Enum.IsDefined(value.Report.Battle.Summary.ContentOutcome)
                && value.Report.Succeeded == (value.Report.Battle.Summary.ContentOutcome == BattleOutcome.Victory), "Inconsistent trial evidence.");
            attempt(true); completed++; return value;
        }
        for (var restart = 0; restart < Restarts; restart++)
        {
            var d = Bind(template, allocation.Selected, restart, false); var input = TowerBossImprovement.Inputs(d);
            var discovery = await TowerBossImprovement.ExecuteBattlesAsync(d, input, mechanics, Fight, ct);
            save($"search-{restart + 1:D2}-discovery.json", discovery);
            if (discovery.Status != "Complete") throw new IncumbentTieIncompleteException("Incomplete discovery; no restart replacement.");
            Require(completed == restart * 496 + 368, "Changed discovery accounting.");
            var selectionInput = input with { DiscoverySeeds = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection) };
            var selection = new List<BossDiscoveryMeasurement>();
            foreach (var party in discovery.DiscoveryShortlist)
                selection.Add(await TowerBossDiscoveryRun.Measure(d, selectionInput, party, "selection", Fight, ct, "selection"));
            var search = Select(restart, d, Bind(template, allocation.Selected, restart, true), mechanics, discovery, selection);
            save($"search-{restart + 1:D2}.json", search); searches.Add(search);
        }
        Require(completed == SearchFights, "Incomplete global search.");
        var active = searches.Where(s => s.Baseline.RecipeHash != s.Candidate.RecipeHash).Select(s => s.Restart).ToArray();
        var freeze = new IncumbentTieFreeze(Version, HarnessJson.Hash(allocation), HarnessJson.Hash(searches), attemptsHash(),
            completed, searches, active);
        save("outputs-freeze.json", freeze); // Durable global barrier before the first confirmation callback.
        frozen = true;
        var evidence = new List<IncumbentTieEvidence>();
        foreach (var search in searches.Where(s => active.Contains(s.Restart)))
        {
            var panel = Panel(allocation.Selected, search.Restart - 1);
            async Task<IReadOnlyList<TowerBalanceTrial>> Confirm(IncumbentTieOutput output, string arm)
            {
                var scenario = output.Scenario with { Seeds = panel }; var rows = new List<TowerBalanceTrial>();
                foreach (var seed in panel)
                {
                    var result = await Fight(arm, "confirmation", scenario, seed, ct);
                    rows.Add(new(seed, result.Report.Battle.Summary.ContentOutcome));
                }
                return rows;
            }
            evidence.Add(new(search.Restart, await Confirm(search.Baseline, "baseline"), await Confirm(search.Candidate, "candidate")));
        }
        Require(started == completed && completed == SearchFights + 2 * Samples * active.Length, "Incomplete paired evidence.");
        var study = new IncumbentTieStudy(Version, freeze, evidence); save("study.json", study); return study;
    }

    internal static IncumbentTieResult Assess(IncumbentTieStudy study, IncumbentTieReservation allocation, int historicalCount)
    {
        var f = study.Freeze;
        Require(study.Version == Version && f.Version == Version && f.CompletedAttempts == SearchFights
            && f.BindingHash == HarnessJson.Hash(allocation) && f.SearchHash == HarnessJson.Hash(f.Searches)
            && TowerContractJson.Hash(f.AttemptsHash) && f.Searches.Select(s => s.Restart).SequenceEqual(Enumerable.Range(1, Restarts))
            && f.ActiveRestarts.SequenceEqual(f.Searches.Where(s => s.Baseline.RecipeHash != s.Candidate.RecipeHash).Select(s => s.Restart))
            && study.Evidence.Select(e => e.Restart).SequenceEqual(f.ActiveRestarts), "Missing, reordered or changed comparison family.");
        var pairs = new List<IncumbentTiePair>();
        foreach (var s in f.Searches)
        {
            var same = s.Baseline.RecipeHash == s.Candidate.RecipeHash;
            var row = study.Evidence.SingleOrDefault(e => e.Restart == s.Restart);
            if (same) { pairs.Add(new(s.Restart, s.Baseline.Finalist.Party.Id, s.Candidate.Finalist.Party.Id, true, null, null, 0, 0, 0)); continue; }
            var panel = Panel(allocation.Selected, s.Restart - 1);
            Require(row is not null && row.Baseline.Select(t => t.Seed).SequenceEqual(panel) && row.Candidate.Select(t => t.Seed).SequenceEqual(panel)
                && panel.Length == Samples && row.Baseline.Concat(row.Candidate).All(t => Enum.IsDefined(t.Outcome)), "Invalid paired confirmation.");
            var zipped = row.Baseline.Zip(row.Candidate).ToArray();
            var gains = zipped.Count(p => p.First.Outcome != BattleOutcome.Victory && p.Second.Outcome == BattleOutcome.Victory);
            var losses = zipped.Count(p => p.First.Outcome == BattleOutcome.Victory && p.Second.Outcome != BattleOutcome.Victory);
            pairs.Add(new(s.Restart, s.Baseline.Finalist.Party.Id, s.Candidate.Finalist.Party.Id, false,
                row.Baseline.Count(t => t.Outcome == BattleOutcome.Victory), row.Candidate.Count(t => t.Outcome == BattleOutcome.Victory), gains, losses, (gains - losses) / (double)Samples));
        }
        return Summarize(pairs, historicalCount);
    }

    internal static IncumbentTieResult Summarize(IReadOnlyList<IncumbentTiePair> pairs, int historicalCount)
    {
        Require(pairs.Count == Restarts && pairs.Select(p => p.Restart).SequenceEqual(Enumerable.Range(1, Restarts))
            && historicalCount is >= 0 and <= MaximumHistory && pairs.All(p => p.Identical
                ? p.BaselineWins is null && p.CandidateWins is null && p.Gains == 0 && p.Losses == 0 && p.Difference == 0
                : p.BaselineWins is >= 0 and <= Samples && p.CandidateWins is >= 0 and <= Samples
                    && p.Gains >= 0 && p.Losses >= 0 && p.Gains + p.Losses <= Samples
                    && p.Gains <= p.CandidateWins && p.Gains <= Samples - p.BaselineWins
                    && p.Losses <= p.BaselineWins && p.Losses <= Samples - p.CandidateWins
                    && p.CandidateWins - p.BaselineWins == p.Gains - p.Losses && p.Difference == (p.Gains - p.Losses) / (double)Samples), "Invalid comparison counts.");
        var k = pairs.Count(p => !p.Identical); var net = pairs.Sum(p => p.Gains - p.Losses); var positive = pairs.Count(p => p.Gains > p.Losses);
        var n = k * Samples; const int denominator = Restarts * Samples;
        var mean = net / (double)denominator;
        var population = 4294967296d - historicalCount - SearchValues;
        var depletion = n * (n - 1d) / (population * denominator);
        var margin = Math.Sqrt(2d * n * Math.Log(20)) / denominator + depletion;
        var lower = Math.Max(-k / (double)Restarts, mean - margin);
        var decision = k == 0 ? "NoSelectorDifferences" : net >= 240 && lower > 0 && positive >= 3
            ? "SupportsIncumbentTieForFrozenOutputs" : "DoNotPromoteIncumbentTie";
        return new(Version, "Verified", decision, BoundVersion, pairs, k, net, positive, denominator, mean, depletion, margin, lower, SearchFights + 2 * n);
    }
}
