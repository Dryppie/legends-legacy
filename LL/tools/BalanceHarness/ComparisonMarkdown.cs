using System.Globalization;
using System.Text;

namespace BalanceHarness;

public static class ComparisonMarkdown
{
    public static string Render(ComparisonReport report)
    {
        var text = new StringBuilder();
        text.AppendLine("# Idle balance comparison\n");
        text.AppendLine($"**{report.Status} — advisory only.** No balance targets, regression thresholds or acceptance gates are applied.\n");
        text.AppendLine($"Baseline reason: {Escape(report.BaselineReason)}\n");
        text.AppendLine($"Baseline: {Link(report.BaselineRun, "scorecard.md", "scorecard")}. Candidate: {Link(report.CandidateRun, "scorecard.md", "scorecard")} ({report.CandidateStatus}).\n");
        text.AppendLine($"Compared {report.Cells.Count(c => c.Status == "Compared")} of {report.Cells.Count} cells. Gameplay records changed in {report.Cells.Sum(c => c.GameplayChanges)} paired battles; outcomes changed in {report.Cells.Sum(c => c.OutcomeChanges)}.\n");
        text.AppendLine("All deltas are candidate minus baseline. Clear-rate and health changes use percentage points (pp). A positive clear-rate change means more victories; whether that is desirable depends on the intended difficulty.\n");
        text.AppendLine("| Cell | Status | Valid n baseline → candidate | Clear rate baseline → candidate | Clear change pp [95% interval] | Gained/lost wins | Win median s baseline → candidate | Shared-win mean change s (pairs) | Health mean change pp | Changed gameplay/outcomes |");
        text.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var cell in report.Cells)
        {
            var compared = cell.Status == "Compared";
            text.AppendLine($"| {Escape(cell.CellId)} | {cell.Status} | {Count(cell.Baseline?.Valid)} → {Count(cell.Candidate?.Valid)} | {Percent(cell.Baseline?.ClearRate?.Rate)} → {Percent(cell.Candidate?.ClearRate?.Rate)} | {Estimate(cell.ClearRateChange)} | {(compared ? $"{cell.GainedWins}/{cell.LostWins}" : "—")} | {Number(cell.Baseline?.WinDurationSeconds.Median)} → {Number(cell.Candidate?.WinDurationSeconds.Median)} | {Number(cell.SharedWinDurationChange?.MeanChange)} ({Count(cell.SharedWinDurationChange?.Pairs)}) | {Number(cell.RemainingHealthChange?.MeanChange)} | {(compared ? $"{cell.GameplayChanges}/{cell.OutcomeChanges}" : "—")} |");
        }
        text.AppendLine("\nClear-rate intervals combine 97.5% Wilson intervals for gained/lost win probabilities with a Bonferroni adjustment (conservative approximate 95% coverage). Identical observed outcomes still have sampling uncertainty. Fixed-suite equality and population estimates are different claims.\n");
        text.AppendLine("Winning-duration change uses only seeds won in both runs; its pair count is shown and it excludes newly won/lost fights. The full winning medians can describe different subsets. Health change uses all matched attempts, including defeats. Per-run distributions and paired mean intervals are retained in JSON.\n");
        text.AppendLine("Duration/health mean intervals use a normal approximation with at least 30 eligible pairs and nonzero observed variance. Otherwise the interval is unavailable; this is not evidence of zero population uncertainty. These are exploratory intervals without correction across cells/metrics. Shared seeds do not guarantee identical random trajectories after a gameplay change.\n");
        text.AppendLine("## Non-comparable or incomplete cells\n");
        var excluded = report.Cells.Where(c => c.Status != "Compared").ToArray();
        if (excluded.Length == 0) text.AppendLine("None.\n");
        foreach (var cell in excluded)
            text.AppendLine($"- **{Escape(cell.CellId)} ({cell.Status}):** {Escape(string.Join(" ", cell.Reasons))} Invalid/cancelled/not run: {Count(cell.Candidate?.Invalid)}/{Count(cell.Candidate?.Cancelled)}/{Count(cell.Candidate?.NotRun)}.");
        text.AppendLine("\n## Evidence changes\n");
        text.AppendLine("Code and content changes are allowed. Changed fixture selections, rules, units or schedules are excluded. Derived character stats/equipment and encounter content may change under the same recipe; those changes are listed below. Full hashes and run fingerprints are in JSON.\n");
        if (report.EvidenceChanges.Count == 0) text.AppendLine("No code, content, environment or resolved-input changes.\n");
        else
        {
            text.AppendLine("| Kind | Name | Baseline | Candidate |");
            text.AppendLine("| --- | --- | --- | --- |");
            foreach (var change in report.EvidenceChanges)
                text.AppendLine($"| {Escape(change.Kind)} | {Escape(change.Name)} | {Short(change.Baseline)} | {Short(change.Candidate)} |");
        }
        text.AppendLine("\n## Changed battle examples\n");
        text.AppendLine("Up to three examples per compared cell: outcome changes first, then largest absolute duration change, then health change, then battle ID. These explain selected changes; they are not a representative sample. Saved records open below. Detailed replay requires each run's original assemblies/runtime/platform.\n");
        text.AppendLine("| Battle ID / seed | Outcome baseline → candidate | Duration s baseline → candidate | Saved records |");
        text.AppendLine("| --- | --- | --- | --- |");
        foreach (var example in report.Cells.SelectMany(c => c.Examples ?? []))
            text.AppendLine($"| {Escape(example.BattleId)} / {example.Seed} | {example.BaselineOutcome} → {example.CandidateOutcome} | {Number(example.BaselineSeconds)} → {Number(example.CandidateSeconds)} | {Link(report.BaselineRun, $"battles/{example.BattleId}.json", "baseline")} / {Link(report.CandidateRun, $"battles/{example.BattleId}.json", "candidate")} |");
        text.AppendLine("\n```powershell");
        text.AppendLine($"dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run {Quote(report.BaselineRun)} --battle <battle-id> --detailed");
        text.AppendLine($"dotnet run --project LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-build -- replay --run {Quote(report.CandidateRun)} --battle <battle-id> --detailed");
        text.AppendLine("```\n");
        return text.ToString();
    }

    private static string Number(double? value) => value?.ToString("0.##", CultureInfo.InvariantCulture) ?? "—";
    private static string Count(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "—";
    private static string Percent(double? value) => value.HasValue ? Number(100 * value) + "%" : "—";
    private static string Estimate(PairedEstimate? value) => value is null ? "—"
        : $"{Number(value.MeanChange)} [{Number(value.Lower)}, {Number(value.Upper)}]";
    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    private static string Short(string? value) => value is null ? "—" : Escape(value.Length == 64 ? value[..12] : value);
    private static string Quote(string value) => "'" + value.Replace("'", "''") + "'";
    private static string Link(string directory, string file, string label)
    {
        var path = Path.GetFullPath(Path.Combine(directory, file)).Replace('\\', '/');
        if (!path.StartsWith('/')) path = "/" + path;
        return $"[{label}](<{path.Replace(">", "%3E").Replace("<", "%3C")}>)";
    }
}
