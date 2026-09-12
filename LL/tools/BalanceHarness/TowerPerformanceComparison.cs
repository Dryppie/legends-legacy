namespace BalanceHarness;

public sealed record TowerPerformancePair(int Workers, int Repetition, int Battles, double ReferenceSeconds,
    double CandidateSeconds, double SpeedRatio, long ReferenceArchiveBytes, long CandidateArchiveBytes);
public sealed record TowerPerformanceComparisonReport(string Status, string ReferenceFormat, string CandidateFormat,
    int RepeatedTrialPairs, IReadOnlyList<TowerPerformancePair> Passes, string ReferenceReportHash, string CandidateReportHash);

/// <summary>Revalidates saved archives before comparing matched diagnostic passes; executes no combat.</summary>
public static class TowerPerformanceComparison
{
    public static TowerPerformanceComparisonReport Compare(string reference, string candidate, string output, CancellationToken token = default)
    {
        if (Path.Exists(output)) throw new IOException("Choose a new performance comparison output directory.");
        var first = Read(reference, token); var second = Read(candidate, token);
        if (HarnessJson.Hash(first.Definition) != HarnessJson.Hash(second.Definition)
            || HarnessJson.Hash(first.Scope.Settings) != HarnessJson.Hash(second.Scope.Settings)
            || HarnessJson.Hash(first.Scope.ContentHashes) != HarnessJson.Hash(second.Scope.ContentHashes)
            || HarnessJson.Hash(first.Scope.Execution) != HarnessJson.Hash(second.Scope.Execution))
            throw new InvalidDataException("Performance comparison requires identical recipes, budgets, schedules, content, settings and execution.");
        var pairs = new List<TowerPerformancePair>();
        foreach (var worker in first.Report.Workers)
        foreach (var pass in worker.Passes)
        {
            token.ThrowIfCancellationRequested();
            var other = second.Report.Workers.Single(w => w.Workers == worker.Workers).Passes.Single(p => p.Repetition == pass.Repetition);
            foreach (var row in pass.Cases)
            {
                var matching = other.Cases.Single(c => c.CaseId == row.CaseId);
                if (row.ResultDigest != matching.ResultDigest || row.Battles != matching.Battles || row.Wins != matching.Wins
                    || row.TickLimits != matching.TickLimits || row.MeanDurationSeconds != matching.MeanDurationSeconds)
                    throw new InvalidDataException("Full Tower results differ between performance formats.");
            }
            pairs.Add(new(worker.Workers, pass.Repetition, pass.Cases.Sum(c => c.Battles), pass.ElapsedMilliseconds / 1000,
                other.ElapsedMilliseconds / 1000, pass.ElapsedMilliseconds / other.ElapsedMilliseconds,
                pass.Cases.Sum(c => c.ArchiveBytes), other.Cases.Sum(c => c.ArchiveBytes)));
        }
        var report = new TowerPerformanceComparisonReport("Compared", first.Scope.ArchiveFormat ?? "legacy", second.Scope.ArchiveFormat ?? "legacy",
            pairs.Sum(p => p.Battles), pairs, HarnessJson.FileHash(Path.Combine(reference, "performance.json")), HarnessJson.FileHash(Path.Combine(candidate, "performance.json")));
        Directory.CreateDirectory(output);
        HarnessJson.WriteNew(Path.Combine(output, "comparison.json"), report);
        var text = new System.Text.StringBuilder("# Tower archive performance comparison\n\n");
        text.AppendLine($"**{report.Status}**: {report.ReferenceFormat} → {report.CandidateFormat}. {report.RepeatedTrialPairs} matching repeated trial pairs; these are not independent acceptance samples.\n");
        text.AppendLine("| Workers | Pass | Reference seconds | Candidate seconds | Reference / candidate time | Reference archive MiB | Candidate archive MiB |\n| ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var p in pairs) text.AppendLine(FormattableString.Invariant($"| {p.Workers} | {p.Repetition + 1} | {p.ReferenceSeconds:F3} | {p.CandidateSeconds:F3} | {p.SpeedRatio:F2}× | {p.ReferenceArchiveBytes / 1048576d:F2} | {p.CandidateArchiveBytes / 1048576d:F2} |"));
        text.AppendLine("\nEach pass includes combat, archive creation and strict verification. Archive sizes exclude external detailed replays, root executable and profiling metadata. Timing is measured under uncontrolled OS cache/load; repeat in reversed order before drawing a performance conclusion. This comparison separately revalidates stored archives without rerunning combat, and that validation is outside the original pass times. It does not alter balance decisions or claim whole-campaign throughput.\n");
        File.WriteAllText(Path.Combine(output, "comparison.md"), text.ToString());
        return report;
    }

    private static (TowerPerformanceDefinition Definition, TowerPerformanceScope Scope, TowerPerformanceReport Report) Read(string root, CancellationToken token)
    {
        var index = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(root, "performance-index.json"));
        var expected = new[] { "definition.json", "scope.json", "seed-ledger.json", "executable-files.json", "performance.json", "performance.md" };
        if (!index.Keys.Order().SequenceEqual(expected.Order())) throw new InvalidDataException("Incomplete performance metadata inventory.");
        foreach (var name in expected)
            if (HarnessJson.FileHash(Path.Combine(root, name)) != index[name]) throw new InvalidDataException("Changed performance metadata: " + name);
        var d = TowerContractJson.Read<TowerPerformanceDefinition>(Path.Combine(root, "definition.json"));
        var planned = TowerPerformanceBenchmark.Validate(d);
        var scope = HarnessJson.Read<TowerPerformanceScope>(Path.Combine(root, "scope.json"));
        var report = HarnessJson.Read<TowerPerformanceReport>(Path.Combine(root, "performance.json"));
        if (scope.DefinitionHash != HarnessJson.Hash(d) || scope.ArchiveFormat != report.ArchiveFormat
            || scope.ArchiveFormat is not null && scope.ArchiveFormat != TowerCompactBundle.Format
            || report.Status != "Complete" || report.PlannedBattles != planned || report.StartedBattles != planned || report.CompletedBattles != planned)
            throw new InvalidDataException("Incomplete or incompatible performance report.");
        TowerPerformanceBenchmark.VerifyParity(d, report.Workers);
        foreach (var worker in report.Workers)
        foreach (var pass in worker.Passes)
        {
            if (!double.IsFinite(pass.ElapsedMilliseconds) || pass.ElapsedMilliseconds <= 0) throw new InvalidDataException("Invalid measured elapsed time.");
            foreach (var row in pass.Cases)
            {
                token.ThrowIfCancellationRequested();
                var item = d.Cases.Single(c => c.Id == row.CaseId);
                var path = Path.Combine(root, "workers-" + worker.Workers.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "pass-" + pass.Repetition.ToString("D2", System.Globalization.CultureInfo.InvariantCulture), item.Id);
                string digest; IReadOnlyList<TowerTrial> trials; TowerBalanceEvidence evidence;
                if (scope.ArchiveFormat == TowerCompactBundle.Format)
                {
                    var saved = TowerCompactBundle.ReadSaved(path, token);
                    if (saved.Plan.Cases.Count != 1) throw new InvalidDataException("Unexpected extra compact performance cases.");
                    digest = saved.ResultDigests[item.Id]; trials = saved.Cases[item.Id]; evidence = TowerCompactBundle.Evidence(item.Id, saved, item.Id);
                }
                else
                {
                    var saved = TowerBundle.ReadSaved(path, token); trials = saved.Scorecard.Trials;
                    digest = HarnessJson.Hash(HarnessJson.Read<Dictionary<string, string>>(Path.Combine(path, "tower-results.json")));
                    var input = saved.Inputs[0];
                    evidence = new(item.Id, saved.Scorecard.Status, HarnessJson.Hash(input.Scenario), HarnessJson.Hash(saved.Manifest.ContentHashes),
                        HarnessJson.Hash(new TowerSettings(input.ThreatAndTanking, input.CheckpointIntervalTicks)), HarnessJson.Hash(saved.Manifest.Execution),
                        input.Party.Count, trials.Select(t => new TowerBalanceTrial(t.Seed, t.Report.Battle.Summary.ContentOutcome)).ToArray(), digest);
                }
                if (evidence.Status != "Complete" || evidence.ScenarioHash != HarnessJson.Hash(item.Scenario)
                    || evidence.ContentHash != HarnessJson.Hash(scope.ContentHashes) || evidence.SettingsHash != HarnessJson.Hash(scope.Settings)
                    || evidence.ExecutionHash != HarnessJson.Hash(scope.Execution) || digest != row.ResultDigest || trials.Count != row.Battles
                    || trials.Count(t => t.Report.Succeeded) != row.Wins || trials.Count(t => t.Report.Battle.Summary.TerminationReason == "TickLimit") != row.TickLimits
                    || trials.Average(t => t.Report.Battle.Summary.DurationSeconds) != row.MeanDurationSeconds
                    || Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Sum(p => new FileInfo(p).Length) != row.ArchiveBytes)
                    throw new InvalidDataException("Performance measurements differ from their verified archives.");
            }
        }
        return (d, scope, report);
    }
}
