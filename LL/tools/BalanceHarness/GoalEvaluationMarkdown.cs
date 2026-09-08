using System.Globalization;
using System.Text;

namespace BalanceHarness;

public static class GoalEvaluationMarkdown
{
    public static string Render(BalanceGoals goals, GoalEvaluationReport report)
    {
        var text = new StringBuilder();
        text.AppendLine($"# Balance goal evaluation: {Escape(goals.Id)}\n");
        text.AppendLine($"**Assessment: {report.Assessment}. Enforcement: {report.GateStatus}. Exit code: {report.ExitCode}.**\n");
        text.AppendLine(Escape(goals.Description) + "\n");
        text.AppendLine($"Checks: {report.Checks.Count(c => c.Outcome == GoalOutcome.Pass)} pass, {report.Checks.Count(c => c.Outcome == GoalOutcome.Fail)} fail, {report.Checks.Count(c => c.Outcome == GoalOutcome.Inconclusive)} inconclusive, {report.Checks.Count(c => c.Outcome == GoalOutcome.Invalid)} invalid. Enforced: {report.Checks.Count(c => c.Enforcement == GoalEnforcement.Enforced)}; draft: {report.Checks.Count(c => c.Enforcement == GoalEnforcement.Draft)}. Run status: {report.RunStatus}.\n");
        text.AppendLine("Draft and diagnostic balance findings are advisory. Enforcement applies only to explicitly reviewed primary/guardrail goals; any invalid configuration, coverage or evidence blocks evaluation. An enforced inconclusive result needs review and returns its own nonzero exit code.\n");
        text.AppendLine("The exact policy is frozen in [goals.json](goals.json); [evaluation.json](evaluation.json) records its hash, fixture contract, evaluator version and run fingerprints.");
        if (report.BaselineManifest is not null) text.AppendLine("The verified baseline comparison is saved in [comparison.md](comparison.md).\n");
        else text.AppendLine();
        text.AppendLine("## Evidence and coverage issues\n");
        if (report.Issues.Count == 0) text.AppendLine("None.\n");
        else foreach (var issue in report.Issues) text.AppendLine("- " + Escape(issue));
        text.AppendLine("\n## Interpretation\n");
        text.AppendLine("Bounds are inclusive. Pass requires the interval wholly inside the bounds; fail requires it wholly outside on a disallowed side. Boundary overlap, too few eligible samples or unavailable uncertainty is inconclusive. Missing cells, incompatible baselines or invalid simulations are invalid, never combat losses.\n");
        text.AppendLine("Clear rate uses a 95% Wilson interval; paired clear-rate change uses the comparison's conservative approximate 95% interval. Mean duration/health intervals use a normal approximation with at least 30 nonconstant samples. Intervals are evaluated per check and are not corrected across goals/cells. Numerical proposals require gameplay review before enforcement.\n");
        text.AppendLine("Winning duration includes only victories; shared-win duration change includes only seeds won in both runs. Always read pacing beside clear rate. Eligible samples can therefore be fewer than valid battles. Health includes defeats. Changes are candidate minus baseline, with rate/health differences in percentage points.\n");
        foreach (var goal in goals.Goals)
        {
            text.AppendLine($"## {Escape(goal.Id)}\n");
            text.AppendLine($"**{goal.Enforcement} / {goal.Role}.** {goal.Metric}: {Bounds(goal)} {goal.Unit}; minimum {goal.MinimumSamples} eligible samples.\n");
            text.AppendLine(Escape(goal.Rationale) + "\n");
            if (goal.ReviewReason is { } review) text.AppendLine("Review reason: " + Escape(review) + "\n");
            text.AppendLine("| Cell | Result | Eligible / valid / planned | Estimate [interval] | Reason |");
            text.AppendLine("| --- | --- | --- | --- | --- |");
            foreach (var check in report.Checks.Where(c => c.GoalId == goal.Id))
                text.AppendLine($"| {Escape(check.CellId)} | {check.Outcome} | {check.Measurement?.Samples.ToString(CultureInfo.InvariantCulture) ?? "—"} / {check.ValidBattles} / {check.PlannedBattles} | {Number(check.Measurement?.Value)} [{Number(check.Measurement?.Lower)}, {Number(check.Measurement?.Upper)}] | {Escape(check.Reason)} |");
            text.AppendLine();
        }
        return text.ToString();
    }

    private static string Bounds(BalanceGoal goal) => goal.Minimum.HasValue && goal.Maximum.HasValue
        ? $"[{Number(goal.Minimum)}, {Number(goal.Maximum)}]"
        : goal.Minimum.HasValue ? "≥ " + Number(goal.Minimum) : "≤ " + Number(goal.Maximum);
    private static string Number(double? value) => value?.ToString("0.##", CultureInfo.InvariantCulture) ?? "—";
    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
}
