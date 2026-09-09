using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessLocalBloodGroveTests
{
    private static string ApiRoot => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(ApiRoot, "..", "..", "..", "tools", "BalanceHarness", "Fixtures"));

    [Fact]
    public void Fixed_plan_reserves_new_seeds_and_records_current_policy_and_budget()
    {
        var plan = BloodGroveLocalValidation.CreatePlan(ApiRoot, Fixtures, 100);
        var smoke = BloodGroveLocalValidation.CreatePlan(ApiRoot, Fixtures, 1);
        Assert.Equal(21600, plan.PlannedBattles);
        Assert.Equal(3000, plan.ConfirmationSamples);
        Assert.Equal(518091, plan.MasterSeed);
        Assert.Equal(518092, smoke.MasterSeed);
        Assert.Contains(smoke.MasterSeed, plan.ExcludedMasterSeeds);
        Assert.Contains(plan.MasterSeed, smoke.ExcludedMasterSeeds);
        Assert.Equal(2.421, plan.OffenseMultiplier);
        Assert.Equal(HarnessJson.Hash(BalanceGoals.Read(Path.Combine(Fixtures, BloodGroveLocalValidation.Goals))), plan.GoalsHash);
        foreach (var count in new[] { 0, 101 })
            Assert.Throws<ArgumentOutOfRangeException>(() => BloodGroveLocalValidation.CreatePlan(ApiRoot, Fixtures, count));
    }

    [Fact]
    public async Task Complete_workflow_preserves_other_areas_and_records_replays_without_auto_promotion()
    {
        using var workspace = new Workspace();
        var output = Path.Combine(workspace.Path, "complete");
        var report = await BloodGroveLocalValidation.RunAsync(ApiRoot, Fixtures, output, 1, CancellationToken.None);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(216, report.ValidBattles);
        Assert.Equal(32, report.UnchangedControlCells);
        Assert.Equal(48, report.Controls.Values.Sum(c => c.Cells.Count));
        Assert.Equal(GoalOutcome.Inconclusive, report.Evaluation.Assessment);
        Assert.Equal(4, report.Replays.Count);
        Assert.All(report.Replays, path => Assert.True(File.Exists(Path.Combine(output, path))));
        Assert.Single(report.AreaEffects, a => a.BeforeOffense != a.AfterOffense);
        Assert.False(File.Exists(Path.Combine(output, "baseline.json")));
        var hash = HarnessJson.FileHash(Path.Combine(output, "results.json"));
        await Assert.ThrowsAsync<IOException>(() => BloodGroveLocalValidation.RunAsync(ApiRoot, Fixtures, output, 1, CancellationToken.None));
        Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(output, "results.json")));
    }

    [Fact]
    public async Task Cancellation_keeps_the_plan_and_never_claims_completion()
    {
        using var workspace = new Workspace();
        using var cancellation = new CancellationTokenSource();
        var output = Path.Combine(workspace.Path, "cancelled");
        await Assert.ThrowsAsync<OperationCanceledException>(() => BloodGroveLocalValidation.RunAsync(ApiRoot, Fixtures, output, 1,
            cancellation.Token, _ => cancellation.Cancel()));
        Assert.True(File.Exists(Path.Combine(output, "plan.json")));
        Assert.True(File.Exists(Path.Combine(output, "failure.json")));
        Assert.False(File.Exists(Path.Combine(output, "results.json")));
    }

    private sealed class Workspace : IDisposable
    {
        private readonly string _parent = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-local-tests"));
        public string Path { get; } = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ll-balance-local-tests", Guid.NewGuid().ToString("N")));
        public Workspace() => Directory.CreateDirectory(Path);
        public void Dispose()
        {
            if (!Path.StartsWith(_parent + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test cleanup escaped its temporary directory.");
            Directory.Delete(Path, recursive: true);
        }
    }
}
