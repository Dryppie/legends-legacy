using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessGoalTests
{
    [Theory]
    [InlineData(90, 100, 90, 100, GoalOutcome.Pass)]
    [InlineData(90, 100, 80, 89, GoalOutcome.Fail)]
    [InlineData(90, 100, 80, 90, GoalOutcome.Inconclusive)]
    [InlineData(40, 60, 40, 60, GoalOutcome.Pass)]
    [InlineData(40, 60, 30, 70, GoalOutcome.Inconclusive)]
    [InlineData(40, 60, 61, 70, GoalOutcome.Fail)]
    public void Interval_evaluation_uses_inclusive_bounds_and_preserves_overlap_uncertainty(
        double minimum, double maximum, double lower, double upper, GoalOutcome expected)
    {
        var measurement = new GoalMeasurement(100, (lower + upper) / 2, lower, upper, "Test interval");
        Assert.Equal(expected, GoalEvaluator.Assess(measurement, minimum, maximum, 100).Outcome);
    }

    [Fact]
    public void One_sided_bounds_insufficient_samples_and_missing_uncertainty_are_distinct()
    {
        var interval = new GoalMeasurement(100, 50, 40, 60, "Test interval");
        Assert.Equal(GoalOutcome.Pass, GoalEvaluator.Assess(interval, null, 60, 100).Outcome);
        Assert.Equal(GoalOutcome.Fail, GoalEvaluator.Assess(interval, null, 39, 100).Outcome);
        Assert.Equal(GoalOutcome.Inconclusive, GoalEvaluator.Assess(interval, null, 40, 100).Outcome);
        Assert.Equal(GoalOutcome.Pass, GoalEvaluator.Assess(interval, 40, null, 100).Outcome);
        Assert.Equal(GoalOutcome.Inconclusive, GoalEvaluator.Assess(interval with { Samples = 3 }, 0, 100, 100).Outcome);
        Assert.Equal(GoalOutcome.Inconclusive, GoalEvaluator.Assess(new(0, null, null, null, "No wins"), null, 60, 30).Outcome);
        Assert.Equal(GoalOutcome.Inconclusive, GoalEvaluator.Assess(interval with { Lower = null, Upper = null }, null, 60, 100).Outcome);
        Assert.Equal(GoalOutcome.Invalid, GoalEvaluator.Assess(interval with { Value = double.NaN }, 0, 100, 100).Outcome);
        Assert.Equal(GoalOutcome.Invalid, GoalEvaluator.Assess(interval with { Lower = 51 }, 0, 100, 100).Outcome);
    }

    [Fact]
    public void Archived_zero_win_roundoff_is_not_invalid_and_does_not_turn_boundary_overlap_into_failure()
    {
        var rate = SuiteScorecard.Wilson(0, 100)!;
        var measurement = new GoalMeasurement(100, rate.Rate * 100, rate.Lower * 100, rate.Upper * 100, "Wilson");
        Assert.Equal(GoalOutcome.Fail, GoalEvaluator.Assess(measurement, 90, null, 100).Outcome);
        Assert.Equal(GoalOutcome.Inconclusive, GoalEvaluator.Assess(measurement, null, 0, 100).Outcome);
        Assert.Equal(GoalOutcome.Pass, GoalEvaluator.Assess(measurement, 0, 100, 100).Outcome);
        var change = PairedStatistics.ClearRate(0, 100, 100);
        var paired = new GoalMeasurement(100, change.MeanChange, change.Lower, change.Upper, change.IntervalMethod);
        Assert.Equal(GoalOutcome.Inconclusive, GoalEvaluator.Assess(paired, null, -100, 100).Outcome);
        Assert.Equal(GoalOutcome.Fail, GoalEvaluator.Assess(paired, -5, null, 100).Outcome);
    }

    [Fact]
    public void Default_goals_are_draft_cover_every_cell_and_pin_the_fixture_independently_of_sampling_order()
    {
        var goals = BalanceGoals.Read(FixturePath("idle-goals.json"));
        var suite = HarnessJson.Read<IdleSuiteDefinition>(FixturePath("idle-reference.json"));
        Assert.Equal(12, goals.RequiredCells.Count);
        Assert.Equal(60, goals.Goals.Sum(g => g.Cells.Count));
        Assert.All(goals.Goals, g => Assert.Equal(GoalEnforcement.Draft, g.Enforcement));
        Assert.Equal(goals.FixtureHash, BalanceGoals.FixtureContractHash(suite));
        var shuffled = suite with { SamplesPerCell = 3, Stages = suite.Stages.Reverse().Select(s => s with
        {
            Builds = s.Builds.Reverse().Select(b => b with { Equipment = b.Equipment.Reverse().ToArray() }).ToArray(),
            Encounters = s.Encounters.Reverse().ToArray()
        }).ToArray() };
        Assert.Equal(goals.FixtureHash, BalanceGoals.FixtureContractHash(shuffled));
        Assert.NotEqual(goals.FixtureHash, BalanceGoals.FixtureContractHash(suite with
            { Stages = suite.Stages.Select(s => s with { Assumptions = ["Changed ownership hypothesis."] }).ToArray() }));
    }

    [Fact]
    public void Blood_grove_starter_policy_pins_its_recipe_and_preserves_boundary_uncertainty()
    {
        var goals = BalanceGoals.Read(FixturePath("idle-blood-grove-starter-goals.json"));
        var suite = HarnessJson.Read<IdleSuiteDefinition>(FixturePath("idle-blood-grove-starter.json"));
        Assert.Equal("idle-blood-grove-starter-goals-v2", goals.Id);
        Assert.Equal(suite.Id, goals.SuiteId);
        Assert.Equal(BalanceGoals.FixtureContractHash(suite), goals.FixtureHash);
        Assert.Equal(2, goals.RequiredCells.Count);
        Assert.Equal(suite.Stages.SelectMany(s => s.Builds.SelectMany(b => s.Encounters
            .Select(e => $"{s.Id}.{b.Id}.{e.Id}"))).Order(), goals.RequiredCells.Order());
        Assert.False(goals.RequiresBaseline);
        var goal = Assert.Single(goals.Goals);
        Assert.Equal(goals.RequiredCells.Order(), goal.Cells.Order());
        Assert.Equal(GoalMetric.ClearRate, goal.Metric);
        Assert.Equal(GoalRole.Primary, goal.Role);
        Assert.Equal(GoalEnforcement.Enforced, goal.Enforcement);
        Assert.False(string.IsNullOrWhiteSpace(goal.ReviewReason));
        Assert.Equal(50d, goal.Minimum);
        Assert.Equal(90d, goal.Maximum);
        Assert.Equal(100, goal.MinimumSamples);
        foreach (var (wins, trials, expected) in new[]
        {
            (0, 100, GoalOutcome.Fail), (70, 100, GoalOutcome.Pass),
            (700, 1000, GoalOutcome.Pass), (100, 100, GoalOutcome.Fail),
            (50, 100, GoalOutcome.Inconclusive), (90, 100, GoalOutcome.Inconclusive),
            (741, 1000, GoalOutcome.Pass), (885, 1000, GoalOutcome.Inconclusive),
            (786, 1000, GoalOutcome.Pass), (884, 1000, GoalOutcome.Inconclusive),
            (950, 1000, GoalOutcome.Fail),
            (7, 10, GoalOutcome.Inconclusive)
        })
        {
            var rate = SuiteScorecard.Wilson(wins, trials)!;
            var measurement = new GoalMeasurement(trials, rate.Rate * 100, rate.Lower * 100,
                rate.Upper * 100, "Wilson (95%)");
            Assert.Equal(expected, GoalEvaluator.Assess(measurement, goal.Minimum, goal.Maximum, goal.MinimumSamples).Outcome);
        }
    }

    [Fact]
    public async Task Declared_diagnostics_preserve_strict_coverage_without_inventing_targets()
    {
        using var workspace = new Workspace();
        var suite = HarnessJson.Read<IdleSuiteDefinition>(FixturePath("idle-reference.json"));
        Workspace.Write(workspace.SuitePath, suite with { Stages = [suite.Stages[0]] });
        var run = await workspace.Run(3);
        var goals = GoalsFor(run);
        Assert.DoesNotContain("diagnosticCells", JsonSerializer.Serialize(goals, HarnessJson.Options));
        var first = goals.RequiredCells[0];
        var scoped = goals with { RequiredCells = [first], Goals = [goals.Goals[0] with { Cells = [first] }],
            DiagnosticCells = goals.RequiredCells.Skip(1).ToArray() };
        Assert.Empty(GoalEvaluator.Evaluate(scoped, run).Issues);
        Assert.Single(GoalEvaluator.Evaluate(scoped, run).Checks);
        Assert.Equal(2, GoalEvaluator.Evaluate(scoped with { DiagnosticCells = null }, run).ExitCode);
        Assert.Equal(2, GoalEvaluator.Evaluate(scoped with { DiagnosticCells = ["missing"] }, run).ExitCode);
        Assert.Throws<InvalidDataException>(() => (scoped with { DiagnosticCells = [first] }).Validate());
        Assert.Throws<InvalidDataException>(() => (scoped with { DiagnosticCells = ["repeat", "repeat"] }).Validate());
    }

    [Fact]
    public void Crystal_Creek_policy_enforces_only_primary_wins_and_rejects_saved_overshoot()
    {
        var goals = BalanceGoals.Read(FixturePath("idle-crystal-creek-starter-goals.json"));
        var suite = HarnessJson.Read<IdleSuiteDefinition>(FixturePath("idle-crystal-creek-starter.json"));
        Assert.Equal(BalanceGoals.FixtureContractHash(suite), goals.FixtureHash);
        Assert.Equal(14, goals.DiagnosticCells!.Count);
        var cells = suite.Stages.SelectMany(s => s.Builds.SelectMany(b => s.Encounters.Select(e => $"{s.Id}.{b.Id}.{e.Id}")));
        Assert.Equal(cells.Order(), goals.RequiredCells.Concat(goals.DiagnosticCells).Order());
        var goal = Assert.Single(goals.Goals);
        Assert.Equal(GoalMetric.ClearRate, goal.Metric);
        Assert.Equal(GoalEnforcement.Enforced, goal.Enforcement);
        Assert.Equal(50, goal.Minimum);
        Assert.Equal(90, goal.Maximum);
        Assert.Equal(100, goal.MinimumSamples);
        Assert.All(goal.Cells, c => Assert.StartsWith("handoff-crystal-creek.quest-rewards.", c));
        foreach (var wins in new[] { 496, 497, 500 })
        {
            var rate = SuiteScorecard.Wilson(wins, 500)!;
            Assert.Equal(GoalOutcome.Fail, GoalEvaluator.Assess(new(500, rate.Rate * 100,
                rate.Lower * 100, rate.Upper * 100, "Wilson"), goal.Minimum, goal.Maximum, goal.MinimumSamples).Outcome);
        }
    }

    [Fact]
    public void Goal_validation_rejects_unknown_fields_bad_units_bounds_coverage_and_unreviewed_enforcement()
    {
        var goals = BalanceGoals.Read(FixturePath("idle-goals.json"));
        var goal = goals.Goals[0];
        foreach (var invalid in new[]
        {
            goal with { Metric = GoalMetric.Unknown }, goal with { Unit = "fraction" },
            goal with { Minimum = 80, Maximum = 60 }, goal with { Minimum = null, Maximum = null },
            goal with { Maximum = 101 }, goal with { MinimumSamples = 0 },
            goal with { Cells = [goal.Cells[0], goal.Cells[0]] }, goal with { Cells = ["unknown"] },
            goal with { Enforcement = GoalEnforcement.Enforced },
            goal with { Enforcement = GoalEnforcement.Enforced, Role = GoalRole.Diagnostic, ReviewReason = "Test" }
        })
            Assert.Throws<InvalidDataException>(() => (goals with { Goals = [invalid, .. goals.Goals.Skip(1)] }).Validate());
        Assert.Throws<InvalidDataException>(() => (goals with { Goals = [.. goals.Goals, goal] }).Validate());
        Assert.Throws<InvalidDataException>(() => (goals with { Goals = goals.Goals.Skip(1).ToArray() }).Validate());
        using var workspace = new Workspace();
        var json = JsonSerializer.Serialize(goals, HarnessJson.Options).Replace("\"minimum\": 90", "\"minimun\": 90");
        var file = Path.Combine(workspace.Path, "typo.json");
        File.WriteAllText(file, json);
        Assert.Throws<JsonException>(() => BalanceGoals.Read(file));
    }

    [Fact]
    public async Task Draft_findings_do_not_gate_but_reviewed_pass_fail_and_inconclusive_have_distinct_exit_codes()
    {
        using var workspace = new Workspace();
        var run = await workspace.Run();
        var goals = GoalsFor(run);
        var primary = goals.Goals.Single();
        foreach (var (minimum, maximum, expected, exitCode) in new (double?, double?, GoalOutcome, int)[]
        {
            (90, null, GoalOutcome.Pass, 0), (null, 50, GoalOutcome.Fail, 1), (99, null, GoalOutcome.Inconclusive, 3)
        })
        {
            var draftGoal = primary with { Minimum = minimum, Maximum = maximum };
            var draft = GoalEvaluator.Evaluate(goals with { Goals = [draftGoal] }, run);
            Assert.Equal(expected, draft.Assessment);
            Assert.Equal("Advisory", draft.GateStatus);
            Assert.Equal(0, draft.ExitCode);
            var enforced = GoalEvaluator.Evaluate(goals with { Goals = [draftGoal with
                { Enforcement = GoalEnforcement.Enforced, ReviewReason = "Temporary test policy only." }] }, run);
            Assert.Equal(expected.ToString(), enforced.GateStatus);
            Assert.Equal(exitCode, enforced.ExitCode);
        }
        var diagnostic = primary with { Id = "diagnostic", Role = GoalRole.Diagnostic, Minimum = null, Maximum = 50 };
        var mixed = GoalEvaluator.Evaluate(goals with { Goals = [primary with
            { Enforcement = GoalEnforcement.Enforced, ReviewReason = "Temporary test policy." }, diagnostic] }, run);
        Assert.Equal(GoalOutcome.Fail, mixed.Assessment);
        Assert.Equal("Pass", mixed.GateStatus);
        Assert.Equal(0, mixed.ExitCode);
    }

    [Fact]
    public async Task Baseline_goals_use_paired_intervals_and_reject_missing_or_incompatible_comparisons()
    {
        using var workspace = new Workspace();
        var run = await workspace.Run();
        var goals = GoalsFor(run);
        var change = goals.Goals[0] with { Id = "clear-change", Metric = GoalMetric.ClearRateChange,
            Unit = "percentage points", Role = GoalRole.Guardrail, Minimum = -5 };
        goals = goals with { Goals = [.. goals.Goals, change] };
        Assert.Equal(2, GoalEvaluator.Evaluate(goals, run).ExitCode);
        var comparison = SuiteComparison.Compare(run, run, "test-baseline.json", "Test.");
        var result = GoalEvaluator.Evaluate(goals, run, comparison);
        Assert.Equal(GoalOutcome.Pass, result.Checks.Single(c => c.GoalId == change.Id).Outcome);
        Assert.Equal(0, result.Checks.Single(c => c.GoalId == change.Id).Measurement!.Value);
        Assert.Equal(2, GoalEvaluator.Evaluate(goals, run, comparison with { CandidateArtifactHash = "other-run" }).ExitCode);
        Assert.Equal(2, GoalEvaluator.Evaluate(goals, run, comparison with
            { Status = "Incomplete", Cells = comparison.Cells.Select(c => c with { Status = "Incompatible" }).ToArray() }).ExitCode);
        var worseCell = comparison.Cells[0] with { ClearRateChange = PairedStatistics.ClearRate(0, 50, 100) };
        var worse = GoalEvaluator.Evaluate(goals, run, comparison with { Cells = [worseCell] });
        Assert.Equal(GoalOutcome.Fail, worse.Checks.Single(c => c.GoalId == change.Id).Outcome);
        Assert.Equal(0, worse.ExitCode); // Draft movement remains advisory.
        var duration = change with { Id = "win-change", Metric = GoalMetric.SharedWinDurationChange,
            Unit = "seconds", Minimum = null, Maximum = 5, MinimumSamples = 30 };
        var durationResult = GoalEvaluator.Evaluate(goals with { Goals = [goals.Goals[0], duration] }, run, comparison);
        Assert.Equal(GoalOutcome.Inconclusive, durationResult.Checks.Single(c => c.GoalId == duration.Id).Outcome);
    }

    [Fact]
    public async Task Absolute_pacing_and_health_keep_eligible_counts_and_small_samples_inconclusive()
    {
        using var workspace = new Workspace();
        var run = await workspace.Run(samples: 3);
        var goals = GoalsFor(run);
        var primary = goals.Goals.Single();
        var pace = primary with { Id = "pace", Metric = GoalMetric.WinDurationMean, Unit = "seconds",
            Role = GoalRole.Guardrail, Minimum = null, Maximum = 60, MinimumSamples = 30 };
        var health = pace with { Id = "health", Metric = GoalMetric.RemainingHealthMean, Unit = "percent", Minimum = 0, Maximum = 100 };
        var result = GoalEvaluator.Evaluate(goals with { Goals = [primary, pace, health] }, run);
        Assert.All(result.Checks, c => Assert.Equal(GoalOutcome.Inconclusive, c.Outcome));
        Assert.Equal(run.Scorecard.Cells[0].WinDurationSeconds.Mean, result.Checks.Single(c => c.GoalId == "pace").Measurement!.Value);
        Assert.Equal(100 * run.Scorecard.Cells[0].RemainingHealthFraction.Mean!.Value,
            result.Checks.Single(c => c.GoalId == "health").Measurement!.Value!.Value, 8);
    }

    [Fact]
    public async Task Missing_cells_changed_cohorts_and_incomplete_evidence_are_invalid_even_for_draft_goals()
    {
        using var workspace = new Workspace();
        var run = await workspace.Run();
        var goals = GoalsFor(run);
        Assert.Equal(2, GoalEvaluator.Evaluate(goals with { FixtureHash = new('a', 64) }, run).ExitCode);
        var missing = goals with { RequiredCells = ["missing"], Goals = [goals.Goals[0] with { Cells = ["missing"] }] };
        var missingResult = GoalEvaluator.Evaluate(missing, run);
        Assert.Equal(GoalOutcome.Invalid, missingResult.Checks[0].Outcome);
        Assert.Contains(missingResult.Issues, x => x.Contains("no declared goal coverage", StringComparison.Ordinal));
        using var cancellation = new CancellationTokenSource();
        var directory = Path.Combine(workspace.Path, "cancelled");
        await SuiteBundle.CreateAsync(TestContentPaths.FindApiRoot(), workspace.SuitePath, directory, 1337, null,
            cancellation.Token, (_, _) => cancellation.Cancel());
        var cancelled = GoalEvaluator.Evaluate(goals, SavedSuite.Read(directory));
        Assert.Equal(2, cancelled.ExitCode);
        Assert.Equal(GoalOutcome.Invalid, cancelled.Checks[0].Outcome);
        Assert.Null(cancelled.Checks[0].Measurement);
    }

    [Fact]
    public async Task Cli_evaluation_freezes_policy_preserves_reports_and_reports_invalid_archives()
    {
        using var workspace = new Workspace();
        var run = await workspace.Run();
        var goals = GoalsFor(run);
        var goalFile = Path.Combine(workspace.Path, "goals.json");
        var output = Path.Combine(workspace.Path, "evaluation");
        Workspace.Write(goalFile, goals);
        Assert.Equal(0, await BalanceHarness.Program.Main(["evaluate", "--run", run.Directory,
            "--goals", goalFile, "--output", output]));
        var report = HarnessJson.Read<GoalEvaluationReport>(Path.Combine(output, "evaluation.json"));
        Assert.Equal(HarnessJson.Hash(goals), report.GoalsHash);
        Assert.Equal(run.ArtifactHash, report.RunArtifactHash);
        Workspace.Write(goalFile, goals with { Description = "Edited after evaluation." });
        Assert.Equal(report.GoalsHash, HarnessJson.Hash(BalanceGoals.Read(Path.Combine(output, "goals.json"))));
        Assert.Throws<IOException>(() => GoalEvaluationBundle.Create(goalFile, run.Directory, null, output));
        var baseline = Path.Combine(workspace.Path, "baseline.json");
        BaselineManifest.Accept(run.Directory, baseline, "Temporary goal integration test.");
        var comparedOutput = Path.Combine(workspace.Path, "with-baseline");
        GoalEvaluationBundle.Create(goalFile, run.Directory, baseline, comparedOutput);
        Assert.True(File.Exists(Path.Combine(comparedOutput, "comparison.md")));
        File.AppendAllText(Path.Combine(run.Directory, "content", "Data", OfflineContent.Files[0]), " ");
        var corruptOutput = Path.Combine(workspace.Path, "corrupt");
        Assert.Equal(2, await BalanceHarness.Program.Main(["evaluate", "--run", run.Directory,
            "--goals", goalFile, "--output", corruptOutput]));
        Assert.True(File.Exists(Path.Combine(corruptOutput, "failure.json")));
        Assert.False(File.Exists(Path.Combine(corruptOutput, "evaluation.json")));
    }

    private static string FixturePath(string file) => Path.GetFullPath(Path.Combine(TestContentPaths.FindApiRoot(),
        "..", "..", "..", "tools", "BalanceHarness", "Fixtures", file));
    private static BalanceGoals GoalsFor(SavedSuite run)
    {
        var cells = run.Input.Cells.Select(c => c.Id).ToArray();
        return new(1, "test-goals", run.Input.Definition.Id, BalanceGoals.FixtureContractHash(run.Input.Definition),
            SavedSuite.MetricsVersion, "Temporary test policy.", cells,
            [new("clear", GoalMetric.ClearRate, "percent", GoalRole.Primary, GoalEnforcement.Draft,
                cells, 100, "Test reliability.", Minimum: 90)]);
    }

    private sealed class Workspace : IDisposable
    {
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-goals-tests"));
        public string Path { get; }
        public string SuitePath => System.IO.Path.Combine(Path, "suite.json");
        public Workspace()
        {
            Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(_parent, Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(Path);
            var suite = HarnessJson.Read<IdleSuiteDefinition>(FixturePath("idle-reference.json"));
            Write(SuitePath, suite with { Stages = [suite.Stages[0] with
                { Builds = [suite.Stages[0].Builds[0]], Encounters = [suite.Stages[0].Encounters[0]] }] });
        }
        public async Task<SavedSuite> Run(int samples = 100)
        {
            var directory = System.IO.Path.Combine(Path, "run");
            await SuiteBundle.CreateAsync(TestContentPaths.FindApiRoot(), SuitePath, directory, 1337, samples, CancellationToken.None);
            return SavedSuite.Read(directory);
        }
        public static void Write<T>(string file, T value) => File.WriteAllText(file, JsonSerializer.Serialize(value, HarnessJson.Options));
        public void Dispose()
        {
            if (!Path.StartsWith(_parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(Path, recursive: true);
        }
    }
}
