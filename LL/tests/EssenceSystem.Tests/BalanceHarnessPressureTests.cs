using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessPressureTests
{
    private static string ApiRoot => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(ApiRoot, "..", "..", "..", "tools", "BalanceHarness", "Fixtures"));

    [Fact]
    public void Plan_keeps_production_validation_and_freezes_budget_and_selection_rules()
    {
        var plan = BloodGrovePressureExperiment.CreatePlan(ApiRoot, Fixtures, 100);
        Assert.Equal(23, plan.Candidates.Count);
        Assert.Equal(2.3, plan.Candidates[0].Bonus);
        Assert.Equal(0.1, plan.Candidates[^1].Bonus);
        Assert.Contains("bonus-000", plan.RejectedCandidates.Keys);
        Assert.Equal(18200, plan.PlannedBattles);
        Assert.Equal(1000, plan.ConfirmationSamples);
        Assert.Equal(1337, plan.DiscoverySeed);
        Assert.Equal(318091, plan.ConfirmationSeed);
        Assert.Equal(14, plan.ContentHashes.Count);
        Assert.Equal(4, plan.FixtureFileHashes.Count);
        foreach (var count in new[] { 0, 101 })
            Assert.Throws<ArgumentOutOfRangeException>(() => BloodGrovePressureExperiment.CreatePlan(ApiRoot, Fixtures, count));
        PressureFinding Finding(double bonus, double worst, double mean) => new(new($"test-{bonus}", bonus), [], worst, mean, "test");
        var winner = Finding(1.5, 10, 5);
        Assert.Equal(winner, BloodGrovePressureExperiment.SelectCandidate(
            [Finding(2, 11, 1), Finding(2, 10, 6), Finding(1, 10, 5), winner]));
    }

    [Fact]
    public void Content_copy_changes_only_the_declared_value_and_never_copies_other_settings()
    {
        using var workspace = new Workspace();
        var source = Path.Combine(workspace.Path, "source");
        BloodGrovePressureExperiment.CreateContentCopy(ApiRoot, source, 2.3, CancellationToken.None);
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

    [Fact]
    public async Task Full_workflow_retains_selection_verified_comparisons_and_replays_without_promotion()
    {
        using var workspace = new Workspace();
        var output = Path.Combine(workspace.Path, "complete");
        var report = await BloodGrovePressureExperiment.RunAsync(ApiRoot, Fixtures, output, 1, CancellationToken.None);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(182, report.ValidBattles);
        Assert.Equal(23, report.Discovery.Count);
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
        await Assert.ThrowsAsync<IOException>(() => BloodGrovePressureExperiment.RunAsync(ApiRoot, Fixtures, output, 1, CancellationToken.None));
        Assert.Equal(summary, HarnessJson.FileHash(Path.Combine(output, "summary.md")));
        var plan = HarnessJson.Read<PressurePlan>(Path.Combine(output, "plan.json"));
        foreach (var pair in plan.ContentHashes)
            Assert.Equal(pair.Value, HarnessJson.FileHash(Path.Combine(ApiRoot, "Data", pair.Key)));
    }

    [Fact]
    public async Task Cancelled_search_keeps_its_plan_without_a_complete_result()
    {
        using var workspace = new Workspace();
        using var cancellation = new CancellationTokenSource();
        var output = Path.Combine(workspace.Path, "cancelled");
        await Assert.ThrowsAsync<OperationCanceledException>(() => BloodGrovePressureExperiment.RunAsync(ApiRoot, Fixtures, output, 1,
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
        public void Dispose()
        {
            if (!Path.StartsWith(_parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(Path, recursive: true);
        }
    }
}
