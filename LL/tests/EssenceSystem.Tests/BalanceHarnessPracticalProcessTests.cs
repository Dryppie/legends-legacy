using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPracticalProcessTests : IAsyncLifetime
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-practical-process-fixture-" + Guid.NewGuid().ToString("N"));
    private readonly List<Process> processes = [];
    private readonly List<Task<string>> readers = [];
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Process test entered combat.")).Activate();
    private static readonly ConcurrentDictionary<string, Task<BossStudyReport>> Reports = new();
    private sealed record Input(string Path, TowerPracticalRequest Request, TowerBossDiscoveryDefinition Definition, BossStudyReport Report);
    private sealed record Running(Process Process, Task<string> Output, Task<string> Error);
    public Task InitializeAsync() { Directory.CreateDirectory(root); return Task.CompletedTask; }
    public async Task DisposeAsync()
    {
        // Only processes started here or matched to their recorded PID/start time
        // are owned. Kill descendants before deleting this isolated fixture root.
        foreach (var process in processes.AsEnumerable().Reverse())
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            }
            finally { process.Dispose(); }
        }
        await Task.WhenAll(readers).WaitAsync(TimeSpan.FromSeconds(10));
        guard.Dispose(); Directory.Delete(root, true);
    }

    private async Task<Input> Fixture(string mode = "complete", string outcome = "pilot", int seconds = 60, bool allocated = false)
    {
        var d = I.Definition() with { ExcludedCombatSeeds = [-987] };
        var report = await Reports.GetOrAdd(outcome, _ => BalanceHarnessPracticalSearchTests.Study(d, outcome));
        Assert.Equal("Complete", report.Status);
        var prior = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(prior, new { historical = d.ExcludedCombatSeeds });
        var definition = Path.Combine(root, "input.json"); HarnessJson.WriteNew(definition, allocated ? BalanceHarnessPracticalAllocationTests.Template(d) : d);
        var content = Path.Combine(root, "content"); Directory.CreateDirectory(content);
        var q = new TowerPracticalRequest(TowerPracticalSearch.Version, content, definition, HarnessJson.FileHash(definition), root,
            Path.Combine(root, "result"), new Dictionary<string, string> { [prior] = HarnessJson.FileHash(prior) }, seconds, 32 * 1048576, 2, 128);
        if (allocated) q = q with { Version = TowerPracticalSearch.AllocationVersion, Allocation = new(19, "literal-allocation-fixture", 8, 32, 256) };
        var inventory = new TowerBossInventoryReport(1, new Dictionary<string, string>(), [],
            F.Mechanics(TowerBossImprovement.Inputs(d)).Essences, [], [], [], [], ["Synthetic process fixture; no gameplay runtime"]);
        var path = Path.Combine(root, "process-fixture.json");
        HarnessJson.WriteNew(path, new ProcessFixture(FixtureHost.Version, mode, q, report, inventory));
        return new(path, q, d, report);
    }

    private Running Start(string command, Input input, bool production = false)
    {
        var start = production ? new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true } : FixtureHost.Start(command, input.Path);
        if (production)
        {
            start.ArgumentList.Add(typeof(TowerPracticalSearch).Assembly.Location);
            start.ArgumentList.Add("tower-practical-search-verify"); start.ArgumentList.Add(input.Request.OutputRoot);
        }
        start.RedirectStandardOutput = true; start.RedirectStandardError = true;
        var process = Process.Start(start)!; processes.Add(process);
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync(); readers.Add(stdout); readers.Add(stderr);
        return new(process, stdout, stderr);
    }

    private static async Task Exit(Running running, int expected, int seconds = 25)
    {
        await running.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(seconds));
        var output = await running.Output.WaitAsync(TimeSpan.FromSeconds(5)); var error = await running.Error.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(running.Process.ExitCode == expected, $"Expected {expected}, got {running.Process.ExitCode}.\n{output}\n{error}");
    }

    private async Task<Process> Blocked(Running owner, Input input, string stage)
    {
        var marker = Path.Combine(root, "fixture-blocked.json"); var timer = Stopwatch.StartNew();
        while (!File.Exists(marker))
        {
            if (owner.Process.HasExited)
                Assert.Fail("Fixture owner exited before the required boundary: " + await owner.Error.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(15), "Worker never reached the required boundary.");
            await Task.Delay(25);
        }
        Assert.Equal(stage, HarnessJson.Read<JsonElement>(marker).GetProperty("stage").GetString());
        var worker = HarnessJson.Read<TowerPracticalWorkerStart>(Path.Combine(input.Request.OutputRoot, "worker-start.json"));
        var process = Process.GetProcessById(worker.ProcessId); processes.Add(process);
        _ = process.SafeHandle; // Retain the Windows handle so ExitCode remains available after this non-child exits.
        Assert.Equal(worker.ProcessStartedUtcTicks, process.StartTime.ToUniversalTime().Ticks);
        Assert.NotEqual(Environment.ProcessId, worker.ProcessId); Assert.NotEqual(owner.Process.Id, worker.ProcessId);
        if (stage == "storage") HarnessJson.WriteNew(marker + ".continue", new { attached = true });
        return process;
    }

    private void Reserved(Input input, string state)
    {
        var pending = HarnessJson.Read<JsonElement>(Path.Combine(input.Request.OutputRoot, "history-input.json"));
        Assert.Equal(state, pending.GetProperty("reservationState").GetString());
        Assert.Equal(TowerPracticalSearch.Reserved(input.Definition), pending.GetProperty("reserved").EnumerateArray().Select(v => v.GetInt32()));
        Assert.False(File.Exists(Path.Combine(input.Request.OutputRoot, "completion.json")));
        Assert.False(File.Exists(Path.Combine(input.Request.OutputRoot, "teams.json")));
    }

    [Theory]
    [InlineData("pilot", "ImprovementNotDemonstrated", 2, false)]
    [InlineData("improved", "DemonstratedImprovement", 1, false)]
    [InlineData("incumbent", "IncumbentRetained", 2, false)]
    [InlineData("pilot", "ImprovementNotDemonstrated", 2, true)]
    [InlineData("improved", "DemonstratedImprovement", 1, true)]
    [InlineData("incumbent", "IncumbentRetained", 2, true)]
    public async Task Separate_parent_and_worker_publish_and_audit_all_synthetic_decisions(string outcome, string decision, int recommendations, bool allocated)
    {
        var input = await Fixture(outcome: outcome, allocated: allocated); var owner = Start("parent", input); await Exit(owner, 0);
        var output = input.Request.OutputRoot; var result = HarnessJson.Read<TowerPracticalResult>(Path.Combine(output, "result.json"));
        Assert.Equal("Verified", result.IntegrityStatus); Assert.Equal(decision, result.StrengthDecision);
        Assert.Equal(recommendations, result.RecommendedPartyIds.Count); Assert.Equal(GoalOutcome.Fail, result.BalanceAssessment);
        TowerBulkCampaign.VerifyFiles(output, "files.json", true, default);
        TowerPracticalSearch.VerifyAttempts(Path.Combine(output, "attempts.jsonl"), input.Report);
        var launch = HarnessJson.Read<TowerPracticalLaunch>(Path.Combine(output, "launch.json"));
        var worker = HarnessJson.Read<TowerPracticalWorkerStart>(Path.Combine(output, "worker-start.json"));
        Assert.Equal(owner.Process.Id, launch.ParentProcessId); Assert.NotEqual(launch.ParentProcessId, worker.ProcessId);
        Assert.NotEqual(Environment.ProcessId, worker.ProcessId);
        var completion = HarnessJson.Read<JsonElement>(Path.Combine(output, "completion.json"));
        Assert.InRange(completion.GetProperty("chargedSeconds").GetDouble(), input.Request.PriorSeconds, input.Request.MaximumSeconds);
        Assert.Equal(input.Request.PriorBytes, completion.GetProperty("priorBytes").GetInt64());
        var teams = HarnessJson.Read<TowerPracticalTeams>(Path.Combine(output, "teams.json"));
        Assert.All(teams.Teams, t => Assert.Empty(t.Scenario.Seeds));
        await Exit(Start("verify", input), 0);
        // The production native verifier must reject this fabricated archive.
        // A successful fixture audit is not a production authentication bypass.
        await Exit(Start("verify", input, production: true), 2);
    }

    [Theory]
    [InlineData("allocation-pending", 0, false)]
    [InlineData("allocation-start", 1, false)]
    [InlineData("allocation-candidate", 2, false)]
    [InlineData("allocation-complete", 594, false)]
    [InlineData("before-complete", 594, false)]
    [InlineData("allocation-handoff", 594, true)]
    public async Task Parent_death_at_allocation_boundaries_leaves_visible_exclusions_or_a_blocker(string stage, int rows, bool complete)
    {
        var input = await Fixture(stage + "-hang", allocated: true); var owner = Start("parent", input);
        var worker = await Blocked(owner, input, stage);
        Assert.Throws<IOException>(() => TowerCompactBundle.AcquireWriter(Path.Combine(root, "complete-family-allocation")));
        owner.Process.Kill(entireProcessTree: false);
        await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(8)); Assert.Equal(130, worker.ExitCode);
        await owner.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        var journal = Path.Combine(input.Request.OutputRoot, "allocation-journal.jsonl");
        Assert.Equal(rows, File.Exists(journal) ? File.ReadAllLines(journal).Length : 0);
        Assert.False(File.Exists(Path.Combine(input.Request.OutputRoot, "attempts.jsonl")));
        var expected = input.Definition.ExcludedCombatSeeds.Concat(TowerPracticalSearch.Reserved(input.Definition)).Order().ToArray();
        TowerRefinementLiveHistory Scan() => TowerRefinementComparisonLaunch.Refresh(root, Path.Combine(root, "next-run"),
            input.Request.RequiredHistory, expected, default);
        if (complete) Assert.Equal(expected, Scan().Values);
        else Assert.Throws<InvalidDataException>(() => Scan());
        var before = Directory.EnumerateFiles(input.Request.OutputRoot).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash);
        var manifest = Path.Combine(root, "recovery-manifest.json"); HarnessJson.WriteNew(manifest, before);
        var recovery = new TowerPracticalRecoveryRequest(TowerPracticalReservationRecovery.AllocationVersion, input.Request.OutputRoot,
            manifest, HarnessJson.FileHash(manifest), Path.Combine(root, "recovery.json"), 30);
        if (complete || stage == "allocation-start")
        {
            Assert.Throws<InvalidDataException>(() => TowerPracticalReservationRecovery.RecoverCore(recovery, candidate: FixtureHost.AllocationCandidate));
            Assert.False(File.Exists(recovery.ReceiptPath));
        }
        else
        {
            if (stage == "allocation-pending")
            {
                var requestPath = Path.Combine(root, "recovery-request.json"); HarnessJson.WriteNew(requestPath, recovery);
                Assert.Equal(0, await TowerPracticalSearch.Command(["tower-practical-search-recover", requestPath], default));
                Assert.Equal(0, await TowerPracticalSearch.Command(["tower-practical-search-recovery-verify", recovery.ReceiptPath], default));
            }
            else TowerPracticalReservationRecovery.RecoverCore(recovery, candidate: FixtureHost.AllocationCandidate);
            var recovered = TowerPracticalReservationRecovery.VerifyCore(recovery.ReceiptPath, candidate: FixtureHost.AllocationCandidate);
            Assert.Equal(rows / 2, recovered.Reserved.Length); Assert.Equal(input.Request.MaximumSeconds, recovered.ForfeitedMaximumSeconds);
            var pending = Path.Combine(input.Request.OutputRoot, "history-input.json");
            var pins = input.Request.RequiredHistory.ToDictionary(p => p.Key, p => p.Value); pins.Add(pending, HarnessJson.FileHash(pending));
            var union = recovered.Historical.Concat(recovered.Reserved).Order().ToArray();
            Assert.Equal(union, TowerRefinementComparisonLaunch.Refresh(root, Path.Combine(root, "next-run"), pins, union, default,
                new Dictionary<string, string> { [pending] = recovery.ReceiptPath }, FixtureHost.AllocationCandidate).Values);
        }
        Assert.All(before, p => Assert.Equal(p.Value, HarnessJson.FileHash(Path.Combine(input.Request.OutputRoot, p.Key))));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.AllocateAndRun(input.Request));
    }

    [Theory] [InlineData("pending-hang", "pending")] [InlineData("attempt-hang", "attempt")]
    public async Task Parent_death_stops_a_blocked_worker_and_preserves_reservations_and_attempts(string mode, string stage)
    {
        var input = await Fixture(mode); var owner = Start("parent", input); var worker = await Blocked(owner, input, stage);
        var launch = HarnessJson.Read<TowerPracticalLaunch>(Path.Combine(input.Request.OutputRoot, "launch.json"));
        Assert.True(launch.Deadline - DateTimeOffset.UtcNow > TimeSpan.FromSeconds(20), "Parent-death case must not be confused with deadline expiry.");
        owner.Process.Kill(entireProcessTree: false);
        await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(8)); Assert.Equal(130, worker.ExitCode);
        await owner.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Reserved(input, stage == "pending" ? "Pending" : "Complete");
        var attempts = Path.Combine(input.Request.OutputRoot, "attempts.jsonl");
        if (stage == "attempt")
        {
            var row = JsonSerializer.Deserialize<JsonElement>(Assert.Single(File.ReadAllLines(attempts)));
            Assert.Equal("Started", row.GetProperty("kind").GetString()); Assert.Equal(1, row.GetProperty("ordinal").GetInt32());
        }
        else
        {
            Assert.False(File.Exists(attempts));
            var before = Directory.EnumerateFiles(input.Request.OutputRoot).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash);
            var manifest = Path.Combine(root, "recovery-manifest.json"); HarnessJson.WriteNew(manifest, before);
            var request = new TowerPracticalRecoveryRequest(TowerPracticalReservationRecovery.Version, input.Request.OutputRoot,
                manifest, HarnessJson.FileHash(manifest), Path.Combine(root, "recovery.json"), 30);
            var recovered = TowerPracticalReservationRecovery.Recover(request);
            Assert.Equal(TowerPracticalSearch.Reserved(input.Definition), recovered.Reserved);
            TowerPracticalReservationRecovery.Verify(request.ReceiptPath);
            Assert.All(before, p => Assert.Equal(p.Value, HarnessJson.FileHash(Path.Combine(input.Request.OutputRoot, p.Key))));
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.Run(input.Request));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Parent_deadline_kills_a_worker_that_ignores_cooperative_cancellation(bool allocated)
    {
        var stage = allocated ? "allocation-pending" : "pending";
        var input = await Fixture(stage + "-hang", seconds: 8, allocated: allocated); var owner = Start("parent", input); var worker = await Blocked(owner, input, stage);
        await Exit(owner, 130, 20); await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        if (allocated) Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(root, Path.Combine(root, "next-run"), input.Request.RequiredHistory, [-987], default));
        else Reserved(input, "Pending");
        Assert.False(File.Exists(Path.Combine(input.Request.OutputRoot, "attempts.jsonl")));
        Assert.Equal("Cancelled", HarnessJson.Read<TowerPracticalResult>(Path.Combine(input.Request.OutputRoot, "failure.json")).ExecutionStatus);
    }

    [Fact]
    public async Task Worker_absolute_deadline_terminates_without_parent_monitoring()
    {
        var input = await Fixture("pending-hang", seconds: 8); var owner = Start("owner", input); var worker = await Blocked(owner, input, "pending");
        await Exit(owner, 130, 20); Assert.True(worker.HasExited); Assert.Equal(130, worker.ExitCode);
        var launch = HarnessJson.Read<TowerPracticalLaunch>(Path.Combine(input.Request.OutputRoot, "launch.json"));
        Assert.True(DateTimeOffset.UtcNow >= launch.Deadline);
        Reserved(input, "Pending"); Assert.False(File.Exists(Path.Combine(input.Request.OutputRoot, "attempts.jsonl")));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Parent_storage_monitor_terminates_a_blocked_worker_and_keeps_the_overrun(bool allocated)
    {
        var input = await Fixture("storage-hang", allocated: allocated); var owner = Start("parent", input); var worker = await Blocked(owner, input, "storage");
        await Exit(owner, 2, 20); await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        var failed = HarnessJson.Read<TowerPracticalResult>(Path.Combine(input.Request.OutputRoot, "failure.json"));
        Assert.Equal("Invalid", failed.ExecutionStatus); Assert.Contains("storage cap", failed.StopReason);
        Assert.Equal(input.Request.MaximumBytes, new FileInfo(Path.Combine(input.Request.OutputRoot, "fixture-overflow.bin")).Length);
        if (allocated) Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(root, Path.Combine(root, "next-run"), input.Request.RequiredHistory, [-987], default));
        else Reserved(input, "Pending");
        Assert.False(File.Exists(Path.Combine(input.Request.OutputRoot, "attempts.jsonl")));
    }

    [Fact]
    public async Task Worker_rejects_a_reused_or_wrong_parent_identity_before_registration()
    {
        var input = await Fixture("wrong-parent"); var owner = Start("owner", input); await Exit(owner, 2);
        Assert.Contains("launcher identity", await owner.Error);
        Assert.False(File.Exists(Path.Combine(input.Request.OutputRoot, "worker-start.json")));
        Assert.False(File.Exists(Path.Combine(input.Request.OutputRoot, "history-input.json")));
        Assert.False(File.Exists(Path.Combine(input.Request.OutputRoot, "attempts.jsonl")));
    }
}
