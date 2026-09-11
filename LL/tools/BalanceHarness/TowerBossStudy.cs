using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record BossStudyAccounting(IReadOnlyDictionary<string, int> Attempted, IReadOnlyDictionary<string, int> Completed,
    int Reserved, int Maximum, int UnusedReservation);
public sealed record BossStudyReplay(string TrialId, BattleOutcome Outcome, string ResultHash);
public sealed record BossStudyReport(string Status, int ExitCode, BossGenerationResult? Discovery,
    IReadOnlyList<BossDiscoveryMeasurement> Selection, BossConfirmationFreeze? Confirmation,
    IReadOnlyList<TowerBalanceEvidence> Evidence, TowerBalanceReport? Balance, BossStudyConclusion? Conclusion,
    IReadOnlyList<BossStudyComparison> Comparisons, IReadOnlyList<BossStudyReplay> Replays, BossStudyAccounting Accounting, string? Error);

/// <summary>One frozen experiment. The same state machine runs against combat or recorded trial evidence.</summary>
public static partial class TowerBossStudy
{
    public const string Algorithm = TowerBossDiscovery.Version + "/" + TowerBossGeneration.Version + "/" + TowerBossStudyPolicy.Version;
    internal delegate Task<TowerBattleReport> Replay(LoadoutTrial trial, TowerScenario scenario, CancellationToken token);

    internal static async Task<BossStudyReport> ExecuteAsync(TowerBossDiscoveryDefinition d, BossGenerationMechanics mechanics,
        TowerBossDiscoveryRun.Battle battle, Replay replay, Action<string, object> freeze, CancellationToken token, Action<string>? progress = null)
    {
        var cost = TowerBossDiscovery.Validate(d); var inputs = TowerBossDiscovery.GenerationInputs(d);
        var caps = new Dictionary<string, int>(StringComparer.Ordinal) { ["discovery"] = cost.Discovery, ["selection"] = cost.Selection,
            ["confirmation"] = cost.GeneratedConfirmation + cost.ReferenceConfirmation, ["diagnostics"] = cost.Diagnostics, ["replay"] = cost.ReplayReserve };
        var attempted = caps.Keys.ToDictionary(k => k, _ => 0); var completed = caps.Keys.ToDictionary(k => k, _ => 0);
        var selection = new List<BossDiscoveryMeasurement>(); var evidence = new List<TowerBalanceEvidence>();
        var replays = new List<BossStudyReplay>();
        var replayChoices = new List<(LoadoutTrial Trial, TowerScenario Scenario, TowerBattleReport Report)>();
        BossGenerationResult? discovery = null; BossConfirmationFreeze? family = null;
        var status = "Invalid"; string? error = null;
        void Charge(string stage)
        {
            token.ThrowIfCancellationRequested();
            if (attempted[stage] >= caps[stage] || attempted.Values.Sum() >= d.MaximumBattles)
                throw new InvalidDataException("Frozen study combat reservation exhausted: " + stage);
            attempted[stage]++;
        }
        async Task<(LoadoutTrial Trial, TowerBattleReport Report)> Fight(string arm, string stage, TowerScenario scenario, int seed, CancellationToken ct)
        {
            Charge(stage);
            var result = await battle(arm, stage, scenario, seed, ct);
            completed[stage]++;
            if (result.Trial.Stage != stage || result.Trial.Seed != seed || result.Trial.Recipe != HarnessJson.Hash(scenario)
                || result.Report.Battle.Seed != seed || result.Report.Battle.ScenarioId != scenario.Id
                || !Enum.IsDefined(result.Report.Battle.Summary.ContentOutcome)
                || result.Report.Succeeded != (result.Report.Battle.Summary.ContentOutcome == BattleOutcome.Victory))
                throw new InvalidDataException("Study combat evidence has inconsistent outcome, stage, seed or recipe.");
            return result;
        }
        try
        {
            var lastCount = -1;
            discovery = await TowerBossGeneration.RunAsync(inputs, mechanics,
                (party, arm, ct) => TowerBossDiscoveryRun.Measure(d, inputs, party, arm, Fight, ct), token, partial => {
                    discovery = partial;
                    var count = partial.Arms.Sum(a => a.Evaluations.Count);
                    if (count != lastCount) { lastCount = count; progress?.Invoke($"Discovery: {count} parties, {completed["discovery"]}/{cost.Discovery} combats."); }
                });
            status = discovery.Status; error = discovery.Error;
            TowerBossDiscovery.ValidateProvenance(d, discovery.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
            freeze("discovery.json", discovery);
            if (status == "Complete")
            {
                // No selection or reference outcome exists when this list is written.
                freeze("discovery-shortlist.json", discovery.DiscoveryShortlist);
                var selectionSeeds = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection);
                var selectionInputs = inputs with { DiscoverySeeds = selectionSeeds };
                foreach (var party in discovery.DiscoveryShortlist)
                {
                    selection.Add(await TowerBossDiscoveryRun.Measure(d, selectionInputs, party, "selection", Fight, token, "selection"));
                    progress?.Invoke($"Selection: {selection.Count}/{discovery.DiscoveryShortlist.Count} generated parties validated.");
                }
                freeze("selection-results.json", selection);
                var finalists = TowerBossStudyPolicy.Select(inputs, mechanics, discovery.DiscoveryShortlist, selection, selectionSeeds, d.Stages.GeneratedFinalists);
                freeze("finalists.json", finalists);
                family = TowerBossStudyPolicy.Freeze(d, discovery.DiscoveryShortlist, selection, finalists, completed.Values.Sum());
                freeze("confirmation-freeze.json", family);
                foreach (var cell in family.Definition.Cells) freeze("exports/" + cell.Id + ".json", cell.Scenario);
                progress?.Invoke($"Confirmation frozen: {finalists.Count} generated finalists, {family.Definition.Cells.Count} distinct party/context cells.");
                foreach (var cell in family.Definition.Cells)
                {
                    var outcomes = new List<TowerBalanceTrial>(); var artifacts = new List<object>();
                    var evidenceIndex = evidence.Count;
                    TowerBalanceEvidence Evidence() => new(cell.Id, outcomes.Count == cell.Scenario.Seeds.Count ? "Complete" : "Incomplete",
                        HarnessJson.Hash(cell.Scenario), HarnessJson.Hash(d.ContentHashes), d.SettingsHash, d.ExecutionHash,
                        d.RequiredPartySize, outcomes.ToArray(), HarnessJson.Hash(artifacts));
                    evidence.Add(Evidence());
                    try
                    {
                        foreach (var seed in cell.Scenario.Seeds)
                        {
                            var result = await Fight("confirmation/" + cell.Id, "confirmation", cell.Scenario, seed, token);
                            var outcome = result.Report.Battle.Summary.ContentOutcome;
                            outcomes.Add(new(seed, outcome)); artifacts.Add(new { result.Trial, ReportHash = HarnessJson.Hash(result.Report) });
                            if (replayChoices.Count < d.Stages.ReplayReserve && replayChoices.All(p => p.Report.Battle.Summary.ContentOutcome != outcome))
                                replayChoices.Add((result.Trial, cell.Scenario, result.Report));
                            if (outcomes.Count == 1 || outcomes.Count % 64 == 0 || outcomes.Count == cell.Scenario.Seeds.Count)
                                progress?.Invoke($"Confirmation: {completed["confirmation"]}/{family.Definition.MaximumBattles} combats; family remains frozen.");
                        }
                    }
                    finally { evidence[evidenceIndex] = Evidence(); }
                }
                // First observed example of each outcome, in fixed trial order, capped by the predeclared reserve.
                freeze("replay-plan.json", replayChoices.Select(p => p.Trial.Id).ToArray());
                foreach (var choice in replayChoices)
                {
                    Charge("replay");
                    var actual = await replay(choice.Trial, choice.Scenario, token);
                    completed["replay"]++;
                    VerifyReplay(choice.Report, actual);
                    replays.Add(new(choice.Trial.Id, actual.Battle.Summary.ContentOutcome, ReplayHash(actual)));
                }
                token.ThrowIfCancellationRequested();
                status = "Complete";
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { status = "Cancelled"; error = "Study interrupted; partial evidence cannot establish acceptance."; }
        catch (Exception exception) { status = "Invalid"; error = exception.GetType().Name + ": " + exception.Message; }
        var balance = family is null ? null : TowerBalanceEvaluator.Evaluate(family.Definition, evidence);
        var conclusion = family is null || balance is null || discovery is null ? null : TowerBossStudyPolicy.Conclude(d, discovery, selection, family, balance);
        // A completed confirmation matrix cannot mask a replay or execution failure.
        if (status != "Complete" && conclusion is not null) conclusion = conclusion with { OverallAssessment = GoalOutcome.Invalid };
        var exit = status == "Cancelled" ? 130 : status == "Invalid" ? 2 : status == "Incomplete" ? 3
            : conclusion?.OverallAssessment switch { GoalOutcome.Pass => 0, GoalOutcome.Fail => 1, GoalOutcome.Inconclusive => 3, _ => 2 };
        return new(status, exit, discovery, selection, family, evidence, balance, conclusion,
            family is null ? [] : TowerBossStudyPolicy.Compare(family, evidence), replays,
            new(attempted, completed, cost.Total, d.MaximumBattles, cost.Total - attempted.Values.Sum()), error);
    }

    internal static string ReplayHash(TowerBattleReport report) => HarnessJson.Hash(new { report.Battle.PreparedParticipants,
        report.Battle.Summary, report.Succeeded, report.GuardianHealthRemainingPercent, report.DisplayDurationSeconds });

    internal static void VerifyReplay(TowerBattleReport expected, TowerBattleReport actual)
    {
        RunBundle.VerifyResult(expected.Battle, actual.Battle);
        if (ReplayHash(expected) != ReplayHash(actual)) throw new InvalidDataException("Study replay differs from the recorded Tower outcome.");
    }
}
