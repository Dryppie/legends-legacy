using BalanceHarness;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerSearchTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static TowerEssenceSearchDefinition Config => HarnessJson.Read<TowerEssenceSearchDefinition>(Path.Combine(Catalogs, TowerEssenceSearch.ConfigFile));
    private static TowerBenchmarkDefinition Baseline => HarnessJson.Read<TowerBenchmarkDefinition>(Path.Combine(Catalogs, Config.BaseCatalog));

    [Fact]
    public void Factorial_candidates_change_only_essences_and_are_legal_at_every_floor()
    {
        var definition = TowerEssenceSearch.Generate(Config, Baseline);
        Assert.Equal(16, definition.Parties.Count); Assert.Equal(Baseline.Floors, definition.Floors);
        var original = TowerBenchmark.Expand(Baseline with { Parties = [Baseline.Parties.Single(p => p.Id == Config.BaseParty)] }, Root, 17, 1);
        var candidates = TowerBenchmark.Expand(definition, Root, 17, 1);
        Assert.Equal(240, candidates.Count);
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, new ThreatAndTankingOptions()));
        foreach (var candidate in candidates)
        {
            var parent = original.Single(s => s.FloorNumber == candidate.FloorNumber);
            var input = runner.CreateInput(candidate, candidate.Seeds[0], new(), 10);
            Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
            foreach (var (a, b) in parent.Party.Zip(candidate.Party))
            {
                Assert.Equal(HarnessJson.Hash(a.Build), HarnessJson.Hash(b.Build with { Id = a.Build.Id, EssenceIds = a.Build.EssenceIds }));
                Assert.Equal(a.Build.EssenceIds.Count, b.Build.EssenceIds.Count);
                Assert.InRange(a.Build.EssenceIds.Except(b.Build.EssenceIds).Count(), 0, 1);
            }
            if (candidate.Id.EndsWith(".candidate-00", StringComparison.Ordinal))
                Assert.All(parent.Party.Zip(candidate.Party), p => Assert.Equal(p.First.Build.EssenceIds, p.Second.Build.EssenceIds));
        }
        Assert.Throws<InvalidDataException>(() => TowerEssenceSearch.Generate(Config with { ConfirmationSeed = Config.DiscoverySeed }, Baseline));
        Assert.Throws<InvalidDataException>(() => TowerEssenceSearch.Generate(Config with { Swaps = [Config.Swaps[0] with { Add = "essence.brown_slime" }] }, Baseline));
        Assert.Throws<InvalidDataException>(() => TowerEssenceSearch.Generate(Config with { Swaps = [Config.Swaps[0] with { Remove = "missing" }] }, Baseline));
        Assert.Throws<InvalidDataException>(() => TowerEssenceSearch.Generate(Config with { BaseCatalog = "../outside.json" }, Baseline));
    }

    [Fact]
    public async Task Search_freezes_selection_confirms_separate_seeds_and_detects_tampering()
    {
        using var temp = new Temp();
        HarnessJson.WriteNew(Path.Combine(temp.Path, Config.BaseCatalog), Baseline with { Floors = [1, 4, 10] });
        var config = Config with { DiscoverySamples = 1, ConfirmationSamples = 2 };
        var output = Path.Combine(temp.Path, "search");
        var report = await TowerEssenceSearch.RunAsync(Root, temp.Path, output, config);
        Assert.Equal("Complete", report.Status); Assert.InRange(report.Selection.Parties.Count, 3, 6);
        Assert.Equal(TowerEssenceSearch.Control, report.Selection.Parties[0]);
        Assert.NotEmpty(report.RoleEvidence);
        var discovery = TowerBenchmark.ReadSaved(Path.Combine(output, "discovery"));
        Assert.Equal(HarnessJson.Hash(report.Selection), HarnessJson.Hash(TowerEssenceSearch.Select(discovery.Report with { Cells = discovery.Report.Cells.Reverse().ToArray() })));
        Assert.Throws<InvalidDataException>(() => TowerEssenceSearch.Select(discovery.Report with { Status = "Cancelled" }));
        var confirmation = TowerBenchmark.ReadSaved(Path.Combine(output, "confirmation"));
        Assert.Empty(discovery.Input.Scenarios.SelectMany(s => s.Seeds).Intersect(confirmation.Input.Scenarios.SelectMany(s => s.Seeds)));
        Assert.All(report.Confirmation.Where(c => c.Party == TowerEssenceSearch.Control), c => { Assert.Equal(0, c.GainedWins); Assert.Equal(0, c.LostWins); });
        foreach (var party in report.Selection.Parties)
            Assert.NotEmpty((await TowerBenchmark.ReplayAsync(Path.Combine(output, "confirmation"), $"floor-1.{party}/tower.0001", true, default)).Battle.EventLog!);
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(TowerEssenceSearch.ReadReport(output)));
        await Assert.ThrowsAsync<IOException>(() => TowerEssenceSearch.RunAsync(Root, temp.Path, output, config));
        File.WriteAllText(Path.Combine(output, "selection.json"), "{}");
        Assert.Throws<InvalidDataException>(() => TowerEssenceSearch.ReadReport(output));
    }

    [Fact]
    public async Task Cancelled_search_preserves_trials_without_selecting_or_confirming()
    {
        using var temp = new Temp();
        HarnessJson.WriteNew(Path.Combine(temp.Path, Config.BaseCatalog), Baseline with { Floors = [1] });
        using var cancellation = new CancellationTokenSource();
        var output = Path.Combine(temp.Path, "cancelled");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerEssenceSearch.RunAsync(Root, temp.Path, output,
            Config with { DiscoverySamples = 1, ConfirmationSamples = 1 }, cancellation.Token, _ => cancellation.Cancel()));
        Assert.Equal(1, TowerBenchmark.ReadSaved(Path.Combine(output, "discovery")).Report.ValidBattles);
        Assert.False(File.Exists(Path.Combine(output, "selection.json")));
        Assert.False(Directory.Exists(Path.Combine(output, "confirmation")));
        Assert.Contains("Cancelled", File.ReadAllText(Path.Combine(output, "search-status.json")));
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-search-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
