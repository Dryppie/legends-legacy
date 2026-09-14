using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerDiscoveryParityTests
{
    [Fact]
    public void Nested_real_leases_journals_and_completion_have_constant_candidate_visits()
    {
        using var temp = new Temp();
        var empty = TowerDiscoveryParity.Fixture(temp.P("empty"), 0, 2, CancellationToken.None);
        var retained = TowerDiscoveryParity.Fixture(temp.P("retained"), 8, 2, CancellationToken.None);
        Assert.True(empty.SampleFileVisits > 0);
        Assert.True(empty.SampleDirectoryVisits > 0);
        Assert.Equal(empty.SampleFileVisits, retained.SampleFileVisits);
        Assert.Equal(empty.SampleDirectoryVisits, retained.SampleDirectoryVisits);
        Assert.Equal(8 * TowerDiscoveryPerformance.FixtureArchiveBytes + 1, retained.Bytes - empty.Bytes); // 2 versus 10 in final JSON.
        Assert.Throws<IOException>(() => TowerDiscoveryParity.Fixture(temp.P("retained"), 8, 2, CancellationToken.None));
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("fights")]
    [InlineData("retry")]
    [InlineData("time")]
    [InlineData("storage")]
    [InlineData("scales")]
    [InlineData("samples")]
    public void Closure_budget_is_explicit_and_cannot_expand(string change)
    {
        var path = Path.GetFullPath(Path.Combine(TestContentPaths.FindApiRoot(), "../../../tools/BalanceHarness/Fixtures", TowerPerformanceBenchmark.Fixture));
        var combat = HarnessJson.Read<TowerPerformanceDefinition>(path);
        combat = combat with { Cases = combat.Cases.Take(2).Select(c => c with { Scenario = c.Scenario with { Seeds = c.Scenario.Seeds.Take(8).ToArray() } }).ToArray(),
            WorkerCounts = [1], Repetitions = 2, MaxBattles = 34, MaxSeconds = 600, MaxOutputBytes = 268435456 };
        var d = new TowerDiscoveryParityDefinition(1, 34, 0, 900, 268435456, [0, 9216], 16, "unused", "unused", combat, new Dictionary<string, string>());
        d = change switch {
            "fights" => d with { MaximumFights = 35 }, "retry" => d with { CombatRetries = 1 },
            "time" => d with { MaximumSeconds = 901 }, "storage" => d with { MaximumBytes = 268435457 },
            "scales" => d with { FixtureScales = [0, 1024] }, "samples" => d with { SamplesPerScale = 17 }, _ => d
        };
        if (change == "valid") TowerDiscoveryParity.ValidateBudget(d);
        else Assert.Throws<InvalidDataException>(() => TowerDiscoveryParity.ValidateBudget(d));
    }

    [Fact]
    public async Task Pre_cancelled_closure_creates_no_output_or_attempt_journal()
    {
        using var temp = new Temp(); using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerDiscoveryParity.RunAsync("unused", temp.P("output"), stop.Token));
        Assert.False(Path.Exists(temp.P("output")));
    }

    private sealed class Temp : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "tower-parity-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(root);
        public string P(string name) => Path.Combine(root, name);
        public void Dispose() => Directory.Delete(root, true);
    }
}
