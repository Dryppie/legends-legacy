using System.Globalization;
using System.Text;

namespace BalanceHarness;

public static partial class TowerBossStudy
{
    public static string Markdown(BossStudyReport report)
    {
        string Rate(RateEstimate? rate) => rate is null ? "unavailable" : string.Create(CultureInfo.InvariantCulture,
            $"{rate.Lower:P2}–{rate.Upper:P2}");
        var text = new StringBuilder($"# Tower team study: {report.Status}\n\n");
        text.AppendLine($"Overall assessment: **{report.Conclusion?.OverallAssessment.ToString() ?? "Unavailable"}**. " +
            $"Frozen confirmation family: **{report.Balance?.Assessment.ToString() ?? "Unavailable"}**. " +
            $"Independent generated viability (10% lower threshold): **{report.Conclusion?.GeneratedViability.ToString() ?? "Unavailable"}**.\n");
        text.AppendLine("Execution completion, generated viability, Tower balance and archive verification are separate findings. Verification must be run explicitly. An above-50% team remains a balance breach even if it demonstrates successful search. No global optimum or unsearched-floor acceptance is claimed.\n");
        if (report.Error is not null) text.AppendLine(report.Error + "\n");
        text.AppendLine("## Combat accounting\n\n| Stage | Attempted | Completed |\n| --- | ---: | ---: |");
        foreach (var stage in report.Accounting.Attempted.Keys)
            text.AppendLine($"| {stage} | {report.Accounting.Attempted[stage]} | {report.Accounting.Completed[stage]} |");
        text.AppendLine($"\nReserved: {report.Accounting.Reserved}; hard cap: {report.Accounting.Maximum}; unused reservation: {report.Accounting.UnusedReservation}. " +
            "Interrupted attempts are charged. Diagnostics are reserved but unused in this version. Replay policy audits the first observed confirmation example of each outcome, limited by the reserve.\n");
        text.AppendLine("## Search and selection\n\nReferences never enter generation, discovery scoring or finalist selection. The primary maximizes the worst-context win rate, with boss progress, survival, winning duration and stable ID as tie-breakers. Alternatives require distinct capability and coarse observed behavior patterns within ten percentage points of the primary; these labels do not establish causal mechanisms.\n");
        text.AppendLine("| Method | Restart | Evaluated | Attempts | Stop reason |\n| --- | ---: | ---: | ---: | --- |");
        foreach (var arm in report.Discovery?.Arms ?? []) text.AppendLine($"| {arm.Method} | {arm.Seed} | {arm.Evaluations.Count} | {arm.Proposals.Count} | {arm.StopReason} |");
        text.AppendLine("\n| Selection recipe | Worst-context wins | Guardian health | Party survival | Confirmed |\n| --- | ---: | ---: | ---: | --- |");
        foreach (var row in TowerBossGeneration.Rank(report.Selection))
            text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"| {row.Id} | {row.Fitness.WorstContextWinRate:P2} | {row.Fitness.GuardianHealth:F2}% | {row.Fitness.Survival:F2}% | {report.Confirmation?.Members.Any(m => m.GeneratedIds.Contains(row.Id)) == true} |"));
        text.AppendLine("\nFull ordered recipes, parent/operator lineage and all discovery measurements are retained in discovery.json. No reused discovery or selection samples enter the following confirmation intervals.\n");
        text.AppendLine("## Frozen confirmation\n\n| Context | Recipe export | Sources | Wins / samples | Draws | Pointwise 95% interval | Family-adjusted interval | Above 50% | Cell outcome |\n| --- | --- | --- | ---: | ---: | --- | --- | --- | --- |");
        foreach (var cell in report.Balance?.Cells ?? [])
        {
            var member = report.Confirmation!.Members.Single(m => m.CellId == cell.Id);
            var sources = string.Join(", ", member.GeneratedIds.Select(id => (member.Primary ? "primary " : "generated ") + id)
                .Concat(member.ReferenceIds.Select(id => "reference " + id)));
            text.AppendLine($"| {member.Context} | [{cell.Id}](exports/{cell.Id}.json) | {sources} | {cell.Wins}/{cell.Valid} (planned {cell.Planned}) | {cell.Draws} | {Rate(cell.PointwiseInterval)} | {Rate(cell.AdjustedInterval)} | {cell.ObservedAboveCeiling} | {cell.Outcome} |");
        }
        text.AppendLine($"\nFamily size: {report.Balance?.FamilySize ?? 0}. Approximate Bonferroni-adjusted Wilson intervals use the full frozen family, including missing cells. " +
            "Every included party must support the 50% ceiling; at least one party per declared context must support 10% viability. Draws are non-wins. Exact prepared duplicates share one cell while retaining all source labels.\n");
        foreach (var cohort in report.Balance?.Cohorts ?? [])
            text.AppendLine($"- Floor {cohort.Floor}, {cohort.Context}, {cohort.Purpose}: **{cohort.Outcome}**. {string.Join(" ", cohort.Reasons)}");
        text.AppendLine("\n## Paired generated/reference comparisons\n\n| Generated cell | Reference | Context | Gained / lost wins | Win-rate difference | Paired interval |\n| --- | --- | --- | ---: | ---: | --- |");
        foreach (var pair in report.Comparisons)
            text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"| {pair.GeneratedCell} | {pair.ReferenceId} | {pair.Context} | {pair.GainedWins}/{pair.LostWins} | {pair.Difference.MeanChange:F2} pp | {pair.Difference.Lower:F2}–{pair.Difference.Upper:F2} pp |"));
        text.AppendLine("\nDifferences use equal paired seeds and count draws as non-wins. They describe this comparison and do not replace absolute acceptance thresholds or prove strategy mechanisms.\n");
        var breaches = report.Conclusion?.EarlierBreaches ?? [];
        text.AppendLine($"## Earlier above-ceiling findings\n\n{breaches.Count} discovery/selection findings; {breaches.Count(b => !b.Confirmed)} remain outside the confirmation family. All are retained in study.json. " +
            "An unconfirmed earlier breach prevents an overall Pass even when the frozen confirmation family passes. Do not expand the family or resample after inspecting these results.\n");
        foreach (var note in report.Conclusion?.Notes ?? []) text.AppendLine("- " + note);
        text.AppendLine("\n## Reproduction\n\nThe archive retains content, settings, seed exclusions, stage schedules, producing assemblies/dependencies, recipes, compressed trials, frozen shortlist/finalists/family, comparisons and detailed replay audits. " +
            "Use the same .NET runtime/platform with `dotnet executable/BalanceHarness.dll tower-boss-study-verify --run <archive>`. " +
            "Use `tower-loadout-replay --run <archive> --battle <trial-id> --detailed` for an additional explicit replay. Additional user-requested replays occur outside this study's recorded budget.\n");
        return report.Discovery?.Version == TowerBossImprovement.Version
            ? text.ToString().Replace("Independent generated viability", "Retained-build search viability")
                .Replace("References never enter generation, discovery scoring or finalist selection.", "Explicit supplied references enter discovery as scored starts; their ancestry propagates through every descendant. This is reference-derived improvement. Historical fitness and fresh confirmation outcomes never enter search or finalist selection.")
            : text.ToString();
    }
}
