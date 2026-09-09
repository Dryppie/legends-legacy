using System.Text;

namespace BalanceHarness;

public sealed record TowerCellComparison(string Cell, string Status, string? Reason, int Pairs = 0,
    int GameplayChanges = 0, int GainedWins = 0, int LostWins = 0,
    PairedEstimate? ClearRateChange = null, PairedEstimate? SurvivalChange = null,
    PairedEstimate? DurationChange = null, PairedEstimate? SharedWinDurationChange = null,
    PairedEstimate? GuardianHealthChange = null, IReadOnlyList<string>? ChangedBattles = null);
public sealed record TowerBenchmarkComparisonReport(int SchemaVersion, string Status, string ReferenceRun,
    string CandidateRun, string ReferenceInputHash, string CandidateInputHash,
    IReadOnlyList<EvidenceChange> EvidenceChanges, IReadOnlyList<TowerCellComparison> Cells);

public static class TowerBenchmarkComparison
{
    public static TowerBenchmarkComparisonReport Create(string reference, string candidate, string output,
        CancellationToken token = default)
    {
        if (Path.Exists(output)) throw new IOException("Comparison output already exists.");
        token.ThrowIfCancellationRequested();
        var before = TowerBenchmark.ReadSaved(reference, token);
        var after = TowerBenchmark.ReadSaved(candidate, token);
        var report = Compare(before, after);
        Directory.CreateDirectory(output);
        HarnessJson.WriteNew(Path.Combine(output, "comparison.json"), report);
        File.WriteAllText(Path.Combine(output, "comparison.md"), Markdown(report));
        return report;
    }

    public static TowerBenchmarkComparisonReport Compare(SavedTowerBenchmark before, SavedTowerBenchmark after)
    {
        var changes = new List<EvidenceChange>();
        void Changed(string kind, string name, string a, string b)
        {
            if (a != b) changes.Add(new(kind, name, a, b));
        }
        foreach (var key in before.Manifest.ContentHashes.Keys.Union(after.Manifest.ContentHashes.Keys).Order())
            Changed("Content", key, before.Manifest.ContentHashes.GetValueOrDefault(key) ?? "missing", after.Manifest.ContentHashes.GetValueOrDefault(key) ?? "missing");
        foreach (var key in before.Manifest.Execution.AssemblyHashes.Keys.Union(after.Manifest.Execution.AssemblyHashes.Keys).Order())
            Changed("Assembly", key, before.Manifest.Execution.AssemblyHashes.GetValueOrDefault(key) ?? "missing", after.Manifest.Execution.AssemblyHashes.GetValueOrDefault(key) ?? "missing");
        Changed("Environment", "Runtime", before.Manifest.Execution.Runtime, after.Manifest.Execution.Runtime);
        Changed("Environment", "Platform", before.Manifest.Execution.OperatingSystem + before.Manifest.Execution.Architecture,
            after.Manifest.Execution.OperatingSystem + after.Manifest.Execution.Architecture);
        Changed("Settings", "Tower", HarnessJson.Hash(before.Input.Settings), HarnessJson.Hash(after.Input.Settings));
        var cells = new List<TowerCellComparison>();
        var beforeScenarios = before.Input.Scenarios.ToDictionary(s => s.Id);
        var afterScenarios = after.Input.Scenarios.ToDictionary(s => s.Id);
        foreach (var id in beforeScenarios.Keys.Union(afterScenarios.Keys).Order())
        {
            if (!beforeScenarios.TryGetValue(id, out var bs) || !afterScenarios.TryGetValue(id, out var cs))
            {
                cells.Add(new(id, "Incompatible", "Cell is missing from one catalog."));
                continue;
            }
            if (HarnessJson.Hash(bs) != HarnessJson.Hash(cs))
            {
                cells.Add(new(id, "Incompatible", "Party recipe, slots, assumptions, starting state or seed schedule differs."));
                continue;
            }
            if (!before.Cells.TryGetValue(id, out var b) || !after.Cells.TryGetValue(id, out var c)
                || b.Scorecard.Status != "Complete" || c.Scorecard.Status != "Complete")
            {
                cells.Add(new(id, "Incomplete", "Both cells must contain their complete declared schedules."));
                continue;
            }
            if (b.Inputs.Zip(c.Inputs).Any(p => HarnessJson.Hash(p.First.Rules) != HarnessJson.Hash(p.Second.Rules)
                    || p.First.Floor.GuardianCreatureId != p.Second.Floor.GuardianCreatureId
                    || p.First.Floor.ProgressionPosition != p.Second.Floor.ProgressionPosition))
            {
                cells.Add(new(id, "Incompatible", "Execution rules or encounter identity differs."));
                continue;
            }
            Changed("ResolvedInput", id, HarnessJson.Hash(b.Inputs), HarnessJson.Hash(c.Inputs));
            var pairs = b.Scorecard.Trials.Zip(c.Scorecard.Trials).ToArray();
            var gained = pairs.Count(p => !p.First.Report.Succeeded && p.Second.Report.Succeeded);
            var lost = pairs.Count(p => p.First.Report.Succeeded && !p.Second.Report.Succeeded);
            var changed = pairs.Where(p => GameplayHash(p.First.Report) != GameplayHash(p.Second.Report)).ToArray();
            cells.Add(new(id, "Compared", null, pairs.Length, changed.Length, gained, lost,
                PairedStatistics.ClearRate(gained, lost, pairs.Length),
                PairedStatistics.Mean(pairs.Select(p => TowerBenchmark.Survival(p.Second.Report) - TowerBenchmark.Survival(p.First.Report)), "percentage points"),
                PairedStatistics.Mean(pairs.Select(p => p.Second.Report.Battle.Summary.DurationSeconds - p.First.Report.Battle.Summary.DurationSeconds), "seconds"),
                PairedStatistics.Mean(pairs.Where(p => p.First.Report.Succeeded && p.Second.Report.Succeeded)
                    .Select(p => p.Second.Report.Battle.Summary.DurationSeconds - p.First.Report.Battle.Summary.DurationSeconds), "seconds"),
                PairedStatistics.Mean(pairs.Select(p => (double)(p.Second.Report.GuardianHealthRemainingPercent - p.First.Report.GuardianHealthRemainingPercent)), "percentage points"),
                changed.Take(5).Select(p => $"{id}/{p.Second.Id}").ToArray()));
        }
        var status = before.Report.Status != "Complete" || after.Report.Status != "Complete" ? "Incomplete"
            : cells.Any(c => c.Status != "Compared") ? "Incompatible" : "Compared";
        return new(1, status, before.Directory, after.Directory, before.Manifest.InputHash, after.Manifest.InputHash, changes, cells);
    }

    private static string GameplayHash(TowerBattleReport report) => HarnessJson.Hash(new
        { report.Battle.PreparedParticipants, report.Battle.Summary, report.Succeeded, report.GuardianHealthRemainingPercent, report.DisplayDurationSeconds });

    private static string Markdown(TowerBenchmarkComparisonReport report)
    {
        var text = new StringBuilder($"# Tower benchmark comparison\n\nStatus: **{report.Status}**.\n\n");
        text.AppendLine("All changes are candidate minus reference. Results are descriptive; easier or harder is not automatically better. A reference is an explicitly selected saved measurement, not an accepted balance target. Starter bands do not apply.\n");
        text.AppendLine("| Cell | Status | Pairs | Changed gameplay | Clear Δ pp | Survival Δ pp | Duration Δ s | Guardian health Δ pp |\n| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var cell in report.Cells)
            text.AppendLine(FormattableString.Invariant($"| {cell.Cell} | {cell.Status} | {cell.Pairs} | {cell.GameplayChanges} | {cell.ClearRateChange?.MeanChange:F2} | {cell.SurvivalChange?.MeanChange:F2} | {cell.DurationChange?.MeanChange:F2} | {cell.GuardianHealthChange?.MeanChange:F2} |"));
        foreach (var cell in report.Cells.Where(c => c.Reason is not null)) text.AppendLine($"\n{cell.Cell}: {cell.Reason}");
        text.AppendLine("\nJSON contains paired clear-rate uncertainty, mean-change uncertainty where at least 30 pairs and nonzero variance permit it, shared-win duration changes, and up to five changed replay IDs per cell. Small or invariant samples have unavailable mean intervals, not zero uncertainty. No cross-cell pooling or multiple-comparison correction is applied.\n");
        text.AppendLine("Content, settings, resolved input, assembly and environment changes:");
        foreach (var change in report.EvidenceChanges) text.AppendLine($"- {change.Kind}: {change.Name}");
        if (report.EvidenceChanges.Count == 0) text.AppendLine("- None.");
        return text.ToString();
    }
}
