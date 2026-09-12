using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerPerformanceTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static TowerPerformanceDefinition Definition => TowerContractJson.Read<TowerPerformanceDefinition>(Path.Combine(Fixtures, TowerPerformanceBenchmark.Fixture));

    [Fact]
    public void Default_reservation_includes_repetitions_replays_and_fixed_budget()
    {
        var d = Definition;
        Assert.Equal(300, TowerPerformanceBenchmark.Validate(d));
        Assert.Equal(new[] { "weak", "strong", "summon-status", "long-duration" }, d.Cases.Select(c => c.Id));
        var settings = TowerBundle.ReadSettings(Root);
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, settings.Threat));
        foreach (var item in d.Cases)
        {
            Assert.All(item.Scenario.Party, p => { Assert.Equal(40, p.Build.CharacterLevel); Assert.Equal(5, p.Build.EssenceIds.Count); });
            Assert.Equal(10, runner.CreateInput(item.Scenario, item.Scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks).Party.Count);
        }
    }

    [Theory]
    [InlineData("budget")]
    [InlineData("workers")]
    [InlineData("duplicate-workers")]
    [InlineData("repetitions")]
    [InlineData("case-path")]
    [InlineData("assumptions")]
    [InlineData("duplicate-seeds")]
    [InlineData("storage")]
    public void Invalid_definitions_fail_without_combat(string defect)
    {
        var d = Definition; var item = d.Cases[0];
        d = defect switch
        {
            "budget" => d with { MaxBattles = 299 },
            "workers" => d with { WorkerCounts = [9] },
            "duplicate-workers" => d with { WorkerCounts = [1, 1] },
            "repetitions" => d with { Repetitions = 1 },
            "case-path" => d with { Cases = [item with { Id = "../escape" }] },
            "assumptions" => d with { Cases = [item with { Scenario = item.Scenario with { Assumptions = [] } }] },
            "duplicate-seeds" => d with { Cases = [item with { Scenario = item.Scenario with { Seeds = [1, 1] } }] },
            _ => d with { MaxOutputBytes = long.MaxValue }
        };
        Assert.Throws<InvalidDataException>(() => TowerPerformanceBenchmark.Validate(d));
    }

    [Fact]
    public void Json_profiling_preserves_bytes_and_canonical_hashes()
    {
        using var temp = new Temp();
        var value = Definition;
        var original = Path.Combine(temp.Path, "original.json"); var measured = Path.Combine(temp.Path, "measured.json");
        HarnessJson.WriteNew(original, value); var hash = HarnessJson.Hash(value);
        var trace = new TowerPerformanceTrace();
        using (trace.Activate())
        using (TowerPerformanceTrace.Measure("outer"))
        {
            HarnessJson.WriteNew(measured, value);
            Assert.Equal(hash, HarnessJson.Hash(HarnessJson.Read<TowerPerformanceDefinition>(measured)));
            Assert.Equal(HarnessJson.FileHash(original), HarnessJson.FileHash(measured));
        }
        Assert.False(TowerPerformanceTrace.Enabled);
        var timings = trace.Snapshot();
        Assert.Equal(new FileInfo(measured).Length, timings.Where(t => t.Path.EndsWith("/io.write", StringComparison.Ordinal)).Sum(t => t.Bytes));
        Assert.All(timings, t => Assert.InRange(t.ExclusiveMilliseconds, 0, t.InclusiveMilliseconds));
        Assert.True(timings.Single(t => t.Path == "outer").InclusiveMilliseconds >= timings.Where(t => t.Path != "outer").Sum(t => t.ExclusiveMilliseconds));
    }

    [Fact]
    public async Task Profiling_contexts_remain_isolated_across_parallel_awaits()
    {
        async Task<IReadOnlyList<TowerStageTiming>> Run(string name)
        {
            var trace = new TowerPerformanceTrace();
            using (trace.Activate())
            using (TowerPerformanceTrace.Measure(name))
            {
                await Task.Yield();
                using var child = TowerPerformanceTrace.Measure("child");
                await Task.Yield();
            }
            return trace.Snapshot();
        }
        var all = await Task.WhenAll(Task.Run(() => Run("first")), Task.Run(() => Run("second")));
        Assert.Equal(new[] { "first", "first/child" }, all[0].Select(t => t.Path));
        Assert.Equal(new[] { "second", "second/child" }, all[1].Select(t => t.Path));
        Assert.False(TowerPerformanceTrace.Enabled);
    }

    [Fact]
    public async Task Instrumented_and_uninstrumented_tower_archives_match()
    {
        using var temp = new Temp();
        var scenario = Definition.Cases[1].Scenario;
        scenario = scenario with { Seeds = [scenario.Seeds[0]] };
        var file = Path.Combine(temp.Path, "scenario.json"); HarnessJson.WriteNew(file, scenario);
        var before = Path.Combine(temp.Path, "before"); var after = Path.Combine(temp.Path, "after");
        var original = await TowerBundle.CreateAsync(Root, file, before);
        var starts = 0; var finishes = 0;
        var trace = new TowerPerformanceTrace(done => { if (done) finishes++; else starts++; });
        TowerScorecard measured;
        using (trace.Activate())
        {
            measured = await TowerBundle.CreateAsync(Root, file, after);
            Assert.Equal("Complete", TowerBalanceRuns.Read("case", after).Status);
        }
        Assert.Equal(1, starts); Assert.Equal(1, finishes);
        Assert.Equal(HarnessJson.Hash(original), HarnessJson.Hash(measured));
        Assert.Equal(HarnessJson.FileHash(Path.Combine(before, "battles/tower.0001.json")), HarnessJson.FileHash(Path.Combine(after, "battles/tower.0001.json")));
        var timings = trace.Snapshot();
        Assert.Equal(3, timings.Where(t => t.Path.EndsWith("input.materialize", StringComparison.Ordinal)).Sum(t => t.Calls));
        Assert.Single(timings, t => t.Path.EndsWith("engine.playback-including-checkpoints", StringComparison.Ordinal));
        Assert.Contains(timings, t => t.Path.StartsWith("archive.balance-verify", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Late_invalid_recipe_is_rejected_before_any_battle()
    {
        using var temp = new Temp();
        var d = Small();
        d = d with { Cases = [d.Cases[0], d.Cases[1] with { Scenario = d.Cases[1].Scenario with { FloorNumber = 999 } }] };
        var path = Path.Combine(temp.Path, "definition.json"); HarnessJson.WriteNew(path, d);
        var report = await TowerPerformanceBenchmark.RunAsync(Root, path, Path.Combine(temp.Path, "run"));
        Assert.Equal("Invalid", report.Status); Assert.Equal(0, report.StartedBattles); Assert.Empty(report.Workers);
    }

    [Fact]
    public async Task Storage_limit_stops_before_launch_and_preserves_receipt()
    {
        using var temp = new Temp();
        var path = Path.Combine(temp.Path, "definition.json"); HarnessJson.WriteNew(path, Small() with { MaxOutputBytes = 1_048_576 });
        var output = Path.Combine(temp.Path, "run");
        var report = await TowerPerformanceBenchmark.RunAsync(Root, path, output);
        Assert.Equal("BudgetExceeded", report.Status); Assert.Equal(0, report.StartedBattles);
        Assert.True(File.Exists(Path.Combine(output, "performance.json")));
        await Assert.ThrowsAsync<IOException>(() => TowerPerformanceBenchmark.RunAsync(Root, path, output));
    }

    [Fact]
    public async Task Cancelled_request_does_not_create_output()
    {
        using var temp = new Temp(); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        var output = Path.Combine(temp.Path, "run");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerPerformanceBenchmark.RunAsync(Root,
            Path.Combine(Fixtures, TowerPerformanceBenchmark.Fixture), output, cancel.Token));
        Assert.False(Directory.Exists(output));
    }

    [Fact]
    public async Task Small_command_accounts_for_repeats_checks_parity_and_retains_legacy_validation()
    {
        using var temp = new Temp();
        var d = Small();
        var path = Path.Combine(temp.Path, "definition.json"); HarnessJson.WriteNew(path, d);
        var output = Path.Combine(temp.Path, "run");
        var report = await TowerPerformanceBenchmark.RunAsync(Root, path, output);
        Assert.True(report.Status == "Complete", report.Error);
        Assert.Equal(12, report.PlannedBattles); Assert.Equal(12, report.StartedBattles); Assert.Equal(12, report.CompletedBattles);
        Assert.Equal(2, report.Workers.Count);
        Assert.All(report.Workers, w => Assert.Equal(2, w.CompletedReplays));
        var first = report.Workers[0];
        var pass = first.Passes[0]; var row = pass.Cases[0];
        var changed = first with { Passes = [pass with { Cases = [row with { ResultDigest = "changed" }, pass.Cases[1]] }, first.Passes[1]] };
        Assert.Throws<InvalidDataException>(() => TowerPerformanceBenchmark.VerifyParity(d, [changed, report.Workers[1]]));
        var run = Path.Combine(output, "workers-1/pass-00/weak");
        Assert.Equal("Complete", TowerBalanceRuns.Read("case", run).Status);
        File.AppendAllText(Path.Combine(run, "battles/tower.0001.json"), " ");
        Assert.Equal("Invalid", TowerBalanceRuns.Read("case", run).Status);
        var frozen = HarnessJson.Read<TowerPerformanceDefinition>(Path.Combine(output, "definition.json"));
        File.WriteAllText(Path.Combine(output, "definition.json"), JsonSerializer.Serialize(frozen with { Id = "tampered" }, HarnessJson.Options));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPerformanceBenchmark.RunWorkerAsync(output, 1));
    }

    private static TowerPerformanceDefinition Small()
    {
        var d = Definition;
        return d with { Cases = d.Cases.Take(2).Select(c => c with { Scenario = c.Scenario with { Seeds = [c.Scenario.Seeds[0]] } }).ToArray(),
            Repetitions = 2, WorkerCounts = [1, 2], MaxBattles = 12 };
    }

    [Fact]
    public async Task Cancellation_after_first_pass_waits_for_owned_child_and_keeps_counts()
    {
        using var temp = new Temp(); using var cancel = new CancellationTokenSource();
        var path = Path.Combine(temp.Path, "definition.json"); HarnessJson.WriteNew(path, Small());
        var output = Path.Combine(temp.Path, "run");
        var report = await TowerPerformanceBenchmark.RunAsync(Root, path, output, cancel.Token, _ => cancel.Cancel());
        Assert.Equal("Cancelled", report.Status);
        Assert.InRange(report.StartedBattles, 2, 6);
        Assert.InRange(report.CompletedBattles, 2, report.StartedBattles);
        Assert.Single(report.Workers);
        Assert.Equal(report.Workers[0].StartedBattles, report.StartedBattles);
        Assert.Equal(report.Workers[0].CompletedBattles, report.CompletedBattles);
        Assert.False(Directory.Exists(Path.Combine(output, "workers-2")));
        Assert.True(File.Exists(Path.Combine(output, "stop.requested")));
    }
    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-performance-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
