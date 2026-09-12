using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPressureTests
{
    private static string ApiRoot => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(ApiRoot, "..", "..", "..", "tools", "BalanceHarness", "Fixtures"));

    [Fact]
    public void Plan_keeps_production_validation_and_freezes_budget_and_selection_rules()
    {
        using var workspace = new Workspace();
        var plan = BloodGrovePressureExperiment.CreatePlan(workspace.OriginalRoot, Fixtures, 100);
        Assert.Throws<InvalidDataException>(() => BloodGrovePressureExperiment.CreatePlan(ApiRoot, Fixtures, 100));
        var historical = BalanceGoals.Read(Path.Combine(Fixtures, BloodGrovePressureExperiment.GoalFixture));
        var current = BalanceGoals.Read(Path.Combine(Fixtures, "idle-blood-grove-starter-goals.json"));
        Assert.Equal("idle-blood-grove-starter-goals-v1", historical.Id);
        Assert.Equal("idle-blood-grove-starter-goals-v2", current.Id);
        Assert.Equal(HarnessJson.Hash(historical), plan.GoalsHash);
        Assert.NotEqual(HarnessJson.Hash(current), plan.GoalsHash);
        Assert.Equal(current.FixtureHash, historical.FixtureHash);
        Assert.Equal(65d, Assert.Single(historical.Goals).Minimum);
        Assert.Equal(75d, Assert.Single(historical.Goals).Maximum);
        Assert.Equal(23, plan.Candidates.Count);
        Assert.Equal(2.3, plan.Candidates[0].Bonus);
        Assert.Equal(0.1, plan.Candidates[^1].Bonus);
        Assert.Contains("bonus-000", plan.RejectedCandidates.Keys);
        Assert.Equal(18200, plan.PlannedBattles);
        Assert.Equal(1000, plan.ConfirmationSamples);
        Assert.Equal(1337, plan.DiscoverySeed);
        Assert.Equal(318091, plan.ConfirmationSeed);
        Assert.Equal(OfflineContent.Files.Count, plan.ContentHashes.Count);
        Assert.Equal(4, plan.FixtureFileHashes.Count);
        foreach (var count in new[] { 0, 101 })
            Assert.Throws<ArgumentOutOfRangeException>(() => BloodGrovePressureExperiment.CreatePlan(workspace.OriginalRoot, Fixtures, count));
        PressureFinding Finding(double bonus, double worst, double mean) => new(new($"test-{bonus}", bonus), [], worst, mean, "test");
        var winner = Finding(1.5, 10, 5);
        Assert.Equal(winner, BloodGrovePressureExperiment.SelectCandidate(
            [Finding(2, 11, 1), Finding(2, 10, 6), Finding(1, 10, 5), winner]));
    }

    [Fact]
    public void Fine_plan_preserves_the_recipe_with_a_fixed_grid_budget_and_fresh_schedules()
    {
        using var workspace = new Workspace();
        var coarse = BloodGrovePressureExperiment.CreatePlan(workspace.OriginalRoot, Fixtures, 100);
        var fine = BloodGrovePressureExperiment.CreatePlan(workspace.OriginalRoot, Fixtures, 100, PressureSweep.Fine);
        var smoke = BloodGrovePressureExperiment.CreatePlan(workspace.OriginalRoot, Fixtures, 1, PressureSweep.Fine);
        Assert.Equal("blood-grove-pressure-fine-v1", fine.ExperimentId);
        Assert.Equal(2, fine.SchemaVersion);
        Assert.Equal(Enumerable.Range(20, 11).Reverse().Select(i => i / 100d), fine.Candidates.Select(c => c.Bonus));
        Assert.Equal(11, fine.Candidates.Select(c => c.Id).Distinct().Count());
        Assert.Empty(fine.RejectedCandidates);
        Assert.Equal(24600, fine.PlannedBattles);
        Assert.Equal(500, fine.DiscoverySamples);
        Assert.Equal(1000, fine.ConfirmationSamples);
        Assert.Equal(418091, fine.DiscoverySeed);
        Assert.Equal(418092, fine.ConfirmationSeed);
        Assert.Equal(418093, smoke.DiscoverySeed);
        Assert.Equal(418094, smoke.ConfirmationSeed);
        Assert.Contains(coarse.DiscoverySeed, fine.ExcludedMasterSeeds);
        Assert.Contains(coarse.ConfirmationSeed, fine.ExcludedMasterSeeds);
        Assert.Contains(smoke.DiscoverySeed, fine.ExcludedMasterSeeds);
        Assert.Contains(smoke.ConfirmationSeed, fine.ExcludedMasterSeeds);
        Assert.Contains(fine.DiscoverySeed, smoke.ExcludedMasterSeeds);
        Assert.Contains(fine.ConfirmationSeed, smoke.ExcludedMasterSeeds);
        Assert.Equal(coarse.FixtureHash, fine.FixtureHash);
        Assert.Equal(coarse.GoalsHash, fine.GoalsHash);
        Assert.Equal(coarse.SettingsHash, fine.SettingsHash);
        Assert.Equal(HarnessJson.Hash(coarse.ContentHashes), HarnessJson.Hash(fine.ContentHashes));
        Assert.Equal(coarse.SelectionRule, fine.SelectionRule);
        Assert.Equal(coarse.StoppingRule, fine.StoppingRule);
        Assert.Throws<ArgumentOutOfRangeException>(() => BloodGrovePressureExperiment.CreatePlan(ApiRoot, Fixtures, 100, (PressureSweep)99));
    }

    [Fact]
    public void Content_copy_changes_only_the_declared_value_and_never_copies_other_settings()
    {
        using var workspace = new Workspace();
        var source = Path.Combine(workspace.Path, "source");
        BloodGrovePressureExperiment.CreateContentCopy(workspace.OriginalRoot, source, 2.3, CancellationToken.None);
        var configPath = Path.Combine(source, "appsettings.json");
        var config = JsonNode.Parse(File.ReadAllText(configPath))!;
        config["ConnectionStrings"] = new JsonObject { ["FakeTestOnly"] = "must-not-be-copied" };
        File.WriteAllText(configPath, config.ToJsonString());
        var output = Path.Combine(workspace.Path, "candidate");
        BloodGrovePressureExperiment.CreateContentCopy(source, output, 0.1, CancellationToken.None);
        Assert.DoesNotContain("must-not-be-copied", File.ReadAllText(Path.Combine(output, "appsettings.json")));
        foreach (var file in OfflineContent.Files.Where(f => f != BloodGrovePressureExperiment.BalancePath))
            Assert.Equal(HarnessJson.FileHash(Path.Combine(source, "Data", file)), HarnessJson.FileHash(Path.Combine(output, "Data", file)));
        var before = HarnessJson.Read<JsonNode>(Path.Combine(source, "Data", BloodGrovePressureExperiment.BalancePath));
        var after = HarnessJson.Read<JsonNode>(Path.Combine(output, "Data", BloodGrovePressureExperiment.BalancePath));
        var profile = after["profiles"]!.AsArray().Single(p => p!["id"]!.GetValue<string>() == BloodGrovePressureExperiment.ProfileId)!;
        Assert.Equal(0.1, profile["offenseCurve"]!["postTutorialBonus"]!.GetValue<double>());
        profile["offenseCurve"]!["postTutorialBonus"] = 2.3;
        Assert.Equal(HarnessJson.Hash(before), HarnessJson.Hash(after));
        Assert.Throws<IOException>(() => BloodGrovePressureExperiment.CreateContentCopy(source, output, 1, CancellationToken.None));
        var invalid = Path.Combine(workspace.Path, "invalid");
        Assert.Throws<InvalidOperationException>(() => BloodGrovePressureExperiment.CreateContentCopy(source, invalid, 0, CancellationToken.None));
        Assert.False(Path.Exists(invalid));
    }

    [Theory]
    [InlineData(PressureSweep.Coarse, 182, 23)]
    [InlineData(PressureSweep.Fine, 246, 11)]
    public async Task Full_workflow_retains_selection_verified_comparisons_and_replays_without_promotion(PressureSweep sweep, int battles, int candidates)
    {
        using var workspace = new Workspace();
        var output = Path.Combine(workspace.Path, "complete");
        string? frozenSelection = null;
        var report = await BloodGrovePressureExperiment.RunAsync(workspace.OriginalRoot, Fixtures, output, 1, CancellationToken.None, message =>
        {
            if (!message.StartsWith("confirmation/original:")) return;
            Assert.False(Directory.Exists(Path.Combine(output, "confirmation", "original")));
            frozenSelection = HarnessJson.FileHash(Path.Combine(output, "selection.json"));
        }, sweep);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(battles, report.ValidBattles);
        Assert.Equal(candidates, report.Discovery.Count);
        Assert.NotNull(frozenSelection);
        Assert.Equal(frozenSelection, HarnessJson.FileHash(Path.Combine(output, "selection.json")));
        Assert.Equal(HarnessJson.Hash(report.Selected), HarnessJson.Hash(HarnessJson.Read<PressureFinding>(Path.Combine(output, "selection.json"))));
        Assert.Equal(GoalOutcome.Inconclusive, report.Confirmation.Assessment);
        Assert.Equal(3, report.Confirmation.ExitCode);
        Assert.Equal("Complete", report.StarterComparison.Status);
        Assert.Equal(16, report.UnchangedOpeningControlCells);
        Assert.Equal(48, report.Controls.Values.Sum(c => c.Cells.Count));
        Assert.All(report.Controls.Values, c => Assert.Equal("Complete", c.Status));
        Assert.Equal(14, report.AreaEffects.Count);
        Assert.Equal(4, report.Replays.Count);
        Assert.All(report.Replays, file => Assert.True(File.Exists(Path.Combine(output, file))));
        Assert.False(File.Exists(Path.Combine(output, "baseline.json")));
        var summary = HarnessJson.FileHash(Path.Combine(output, "summary.md"));
        var markdown = File.ReadAllText(Path.Combine(output, "summary.md"));
        foreach (var finding in report.Discovery)
            Assert.Contains($"| {finding.Candidate.Bonus.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} |", markdown);
        await Assert.ThrowsAsync<IOException>(() => BloodGrovePressureExperiment.RunAsync(ApiRoot, Fixtures, output, 1, CancellationToken.None, sweep: sweep));
        Assert.Equal(summary, HarnessJson.FileHash(Path.Combine(output, "summary.md")));
        var plan = HarnessJson.Read<PressurePlan>(Path.Combine(output, "plan.json"));
        foreach (var pair in plan.ContentHashes)
            Assert.Equal(pair.Value, HarnessJson.FileHash(Path.Combine(workspace.OriginalRoot, "Data", pair.Key)));
    }

    [Fact]
    public async Task Cancelled_search_keeps_its_plan_without_a_complete_result()
    {
        using var workspace = new Workspace();
        using var cancellation = new CancellationTokenSource();
        var output = Path.Combine(workspace.Path, "cancelled");
        await Assert.ThrowsAsync<OperationCanceledException>(() => BloodGrovePressureExperiment.RunAsync(workspace.OriginalRoot, Fixtures, output, 1,
            cancellation.Token, _ => cancellation.Cancel()));
        Assert.True(File.Exists(Path.Combine(output, "plan.json")));
        Assert.True(File.Exists(Path.Combine(output, "failure.json")));
        Assert.False(File.Exists(Path.Combine(output, "results.json")));
        Assert.False(File.Exists(Path.Combine(output, "selection.json")));
    }

    private sealed class Workspace : IDisposable
    {
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-pressure-tests"));
        public string Path { get; } = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-pressure-tests", Guid.NewGuid().ToString("N")));
        public Workspace() => Directory.CreateDirectory(Path);
        public string OriginalRoot
        {
            get
            {
                var root = System.IO.Path.Combine(Path, "original");
                if (!Directory.Exists(root)) BloodGroveLocalValidation.CreateOriginalContentCopy(ApiRoot, root);
                return root;
            }
        }
        public void Dispose()
        {
            if (!Path.StartsWith(_parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(Path, recursive: true);
        }
    }
}
