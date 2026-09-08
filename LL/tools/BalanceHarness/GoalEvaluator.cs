using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record GoalMeasurement(int Samples, double? Value, double? Lower, double? Upper,
    string IntervalMethod, string? Note = null);
public sealed record GoalCheck(string GoalId, string CellId, GoalMetric Metric, string Unit, GoalRole Role,
    GoalEnforcement Enforcement, double? Minimum, double? Maximum, int MinimumSamples,
    GoalOutcome Outcome, string Reason, GoalMeasurement? Measurement, int ValidBattles, int PlannedBattles);
public sealed record GoalEvaluationReport(int SchemaVersion, string EvaluatorVersion, string GoalsId,
    string GoalsHash, string FixtureHash, string RunDirectory, string RunArtifactHash, string RunStatus,
    string? BaselineManifest, string? BaselineArtifactHash, GoalOutcome Assessment, string GateStatus,
    int ExitCode, IReadOnlyList<string> Issues, IReadOnlyList<GoalCheck> Checks);

public static class GoalEvaluator
{
    public const string Version = "idle-goals-v1";

    public static GoalEvaluationReport Evaluate(BalanceGoals goals, SavedSuite run,
        ComparisonReport? comparison = null, CancellationToken cancellationToken = default)
    {
        goals.Validate();
        var issues = new List<string>();
        var fixtureHash = BalanceGoals.FixtureContractHash(run.Input.Definition);
        var wrongCohort = goals.SuiteId != run.Input.Definition.Id || goals.FixtureHash != fixtureHash;
        if (wrongCohort) issues.Add($"Suite or fixture contract differs from the goals. Run fixture hash: {fixtureHash}. Review the cohort before updating goals.");
        var required = goals.RequiredCells.ToHashSet(StringComparer.Ordinal);
        var actual = run.Input.Cells.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var id in required.Except(actual).Order(StringComparer.Ordinal)) issues.Add($"Required cell missing: {id}.");
        foreach (var id in actual.Except(required).Order(StringComparer.Ordinal)) issues.Add($"Cell has no declared goal coverage: {id}.");
        if (run.Scorecard.Status != "Complete") issues.Add($"Run status is {run.Scorecard.Status}; incomplete evidence cannot pass evaluation.");
        var wrongComparison = comparison is not null && (comparison.SchemaVersion != 1
            || comparison.ComparisonVersion != SuiteComparison.Version || comparison.CandidateArtifactHash != run.ArtifactHash);
        if (wrongComparison) issues.Add("Comparison schema/version or candidate evidence does not match this run.");
        if (goals.RequiresBaseline && comparison is null) issues.Add("Baseline-change goals require --baseline with an accepted, compatible run.");
        if (comparison is not null && comparison.Status != "Complete") issues.Add("The requested baseline comparison is incomplete or incompatible.");
        var scores = run.Scorecard.Cells.ToDictionary(c => c.CellId, StringComparer.Ordinal);
        var compared = comparison?.Cells.ToDictionary(c => c.CellId, StringComparer.Ordinal);
        var observations = run.Observations.Values.ToLookup(o => o.CellId, StringComparer.Ordinal);
        var checks = new List<GoalCheck>();
        foreach (var goal in goals.Goals)
        foreach (var id in goal.Cells)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var score = scores.GetValueOrDefault(id);
            string? invalid = wrongCohort ? "Goal cohort does not match the run." : score is null ? "Required cell is missing."
                : score.Valid != score.Planned ? "Cell contains invalid, cancelled or unexecuted battles." : null;
            GoalMeasurement? measurement = null;
            if (invalid is null && BalanceGoals.IsChange(goal.Metric))
            {
                var cell = compared?.GetValueOrDefault(id);
                if (comparison is null || wrongComparison || cell is null || cell.Status != "Compared")
                    invalid = cell is null ? "A verified baseline comparison is required for this metric."
                        : "Baseline comparison is not compatible: " + string.Join(" ", cell.Reasons);
                else
                {
                    var estimate = goal.Metric switch
                    {
                        GoalMetric.ClearRateChange => cell.ClearRateChange,
                        GoalMetric.SharedWinDurationChange => cell.SharedWinDurationChange,
                        GoalMetric.RemainingHealthChange => cell.RemainingHealthChange,
                        _ => null
                    };
                    if (estimate is null || estimate.Unit != goal.Unit) invalid = "Required paired metric or its units are unavailable.";
                    else measurement = new(estimate.Pairs, estimate.MeanChange, estimate.Lower, estimate.Upper, estimate.IntervalMethod, estimate.Note);
                }
            }
            else if (invalid is null)
            {
                if (goal.Metric == GoalMetric.ClearRate)
                {
                    var rate = score!.ClearRate;
                    measurement = new(score.Valid, rate?.Rate * 100, rate?.Lower * 100, rate?.Upper * 100, "Wilson (95%)");
                }
                else
                {
                    var values = goal.Metric == GoalMetric.WinDurationMean
                        ? observations[id].Where(o => o.Status == "Completed" && o.Outcome == BattleOutcome.Victory).Select(o => o.DurationSeconds!.Value)
                        : observations[id].Where(o => o.Status == "Completed").Select(o => 100 * o.RemainingHealthFraction!.Value);
                    var mean = SampleStatistics.Mean(values);
                    measurement = new(mean.Count, mean.Mean, mean.Lower, mean.Upper, "Mean normal approximation (95%, minimum 30 samples)", mean.Note);
                }
            }
            var (outcome, reason) = invalid is not null ? (GoalOutcome.Invalid, invalid)
                : Assess(measurement!, goal.Minimum, goal.Maximum, goal.MinimumSamples);
            checks.Add(new(goal.Id, id, goal.Metric, goal.Unit, goal.Role, goal.Enforcement,
                goal.Minimum, goal.Maximum, goal.MinimumSamples, outcome, reason, measurement,
                score?.Valid ?? 0, score?.Planned ?? 0));
        }
        var assessment = issues.Count > 0 ? GoalOutcome.Invalid : Aggregate(checks.Select(c => c.Outcome));
        var enforced = checks.Where(c => c.Enforcement == GoalEnforcement.Enforced && c.Role != GoalRole.Diagnostic).ToArray();
        // Bad configuration/evidence is always an error. Draft balance findings never gate a valid run.
        var gate = assessment == GoalOutcome.Invalid ? "Invalid" : enforced.Length == 0 ? "Advisory"
            : Aggregate(enforced.Select(c => c.Outcome)).ToString();
        var exitCode = gate switch { "Fail" => 1, "Invalid" => 2, "Inconclusive" => 3, _ => 0 };
        return new(1, Version, goals.Id, HarnessJson.Hash(goals), fixtureHash, run.Directory, run.ArtifactHash,
            run.Scorecard.Status, comparison?.BaselineManifest, comparison?.BaselineArtifactHash,
            assessment, gate, exitCode, issues, checks);
    }

    public static (GoalOutcome Outcome, string Reason) Assess(GoalMeasurement measurement,
        double? minimum, double? maximum, int minimumSamples)
    {
        var tolerance = 1e-12 * Math.Max(1, Math.Abs(measurement.Value ?? 0));
        if (measurement.Samples < 0 || new[] { measurement.Value, measurement.Lower, measurement.Upper }.OfType<double>().Any(x => !double.IsFinite(x))
            || (measurement.Lower.HasValue != measurement.Upper.HasValue)
            || measurement.Lower > measurement.Upper
            || (measurement.Value is { } value && (measurement.Lower > value + tolerance || measurement.Upper < value - tolerance)))
            return (GoalOutcome.Invalid, "Non-finite or inconsistent metric evidence.");
        if (measurement.Samples < minimumSamples)
            return (GoalOutcome.Inconclusive, $"Only {measurement.Samples} eligible samples; the declared minimum is {minimumSamples}.");
        if (measurement.Value is null || measurement.Lower is null || measurement.Upper is null)
            return (GoalOutcome.Inconclusive, measurement.Note ?? "Sampling uncertainty is unavailable.");
        // Archived Wilson endpoints can miss 0/100 by floating-point roundoff. Expand
        // only to include the estimate after the consistency check; do not relax goal bounds.
        var lower = Math.Min(measurement.Lower.Value, measurement.Value.Value);
        var upper = Math.Max(measurement.Upper.Value, measurement.Value.Value);
        if ((!minimum.HasValue || lower >= minimum) && (!maximum.HasValue || upper <= maximum))
            return (GoalOutcome.Pass, "The interval lies entirely within the inclusive goal bounds.");
        if ((minimum.HasValue && upper < minimum) || (maximum.HasValue && lower > maximum))
            return (GoalOutcome.Fail, "The interval lies entirely outside the goal on a disallowed side.");
        return (GoalOutcome.Inconclusive, "The interval overlaps a goal boundary.");
    }

    private static GoalOutcome Aggregate(IEnumerable<GoalOutcome> source)
    {
        var values = source.ToArray();
        return values.Contains(GoalOutcome.Invalid) ? GoalOutcome.Invalid : values.Contains(GoalOutcome.Fail) ? GoalOutcome.Fail
            : values.Contains(GoalOutcome.Inconclusive) ? GoalOutcome.Inconclusive : GoalOutcome.Pass;
    }
}
