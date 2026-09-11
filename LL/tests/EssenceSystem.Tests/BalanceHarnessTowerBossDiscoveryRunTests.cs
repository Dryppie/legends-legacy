using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBossDiscoveryRunTests
{
    private static string Root => TestContentPaths.FindApiRoot();

    private static TowerBossDiscoveryDefinition Small(int candidates = 2, int samples = 2)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition();
        var context = d.Contexts[0].Id;
        return d with { Generation = d.Generation with { CandidatesPerArm = candidates, Seeds = [613719], MaximumAttemptsPerArm = 128 },
            Stages = d.Stages with { Shortlist = 2, GeneratedFinalists = 1, DiagnosticCandidates = 0, ReplayReserve = 0,
                Schedules = new Dictionary<string, BossDiscoverySchedule> { [context] = new(new[] { 81091, 81092 }.Take(samples).ToArray(), [82091], [83091], []) } } };
    }

    [Fact]
    public async Task Real_combat_is_reference_invariant_reconstructs_and_exports_normal_Tower_recipes()
    {
        using var temp = new DiscoveryTemp(); var d = Small(candidates: 16, samples: 1);
        var reference = BalanceHarnessTowerBossDiscoveryContractTests.Reference();
        var alternate = reference with { Id = "ordered-alternative", Scenario = reference.Scenario with { Party = reference.Scenario.Party.Select(p =>
            p with { Build = p.Build with { EssenceIds = p.Build.EssenceIds.Reverse().ToArray() } }).ToArray() } };
        var definitions = new[] { d, d with { References = [reference, alternate] }, d with { References = [alternate, reference] } };
        BossDiscoveryRunReport? original = null; IReadOnlyList<LoadoutTrial>? originalTrials = null;
        for (var i = 0; i < definitions.Length; i++)
        {
            var path = Path.Combine(temp.Path, "run-" + i);
            BossDiscoveryRunReport result;
            if (i == 0)
            {
                var definitionPath = Path.Combine(temp.Path, "definition.json"); HarnessJson.WriteNew(definitionPath, definitions[i]);
                Assert.Equal(0, await BalanceHarness.Program.Main(["tower-boss-discover", "--definition", definitionPath, "--output", path, "--content-root", Root]));
                result = HarnessJson.Read<BossDiscoveryRunReport>(Path.Combine(path, "discovery.json"));
            }
            else result = await TowerBossDiscoveryRun.RunAsync(Root, path, definitions[i]);
            Assert.Equal("Complete", result.Status); Assert.Null(result.Error);
            Assert.Equal(32, result.ActualBattles); Assert.Equal(0, result.CacheHits);
            Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await TowerBossDiscoveryRun.VerifyAsync(path)));
            var trials = TowerLoadoutArchive.Verify(path);
            Assert.All(trials, t => { Assert.Equal("discovery", t.Stage); Assert.Contains(t.Seed, new[] { 81091, 81092 }); });
            Assert.False(File.Exists(Path.Combine(path, "assessment.json")));
            Assert.Contains("not confirmation or Tower balance acceptance", File.ReadAllText(Path.Combine(path, "discovery.md")));
            original ??= result; originalTrials ??= trials;
            Assert.Equal(HarnessJson.Hash(original.Generation), HarnessJson.Hash(result.Generation));
            Assert.Equal(originalTrials.Select(t => t.InputHash), trials.Select(t => t.InputHash));
        }
        var first = Path.Combine(temp.Path, "run-0");
        Assert.All(TowerBossGeneration.Operators, op => Assert.Contains(original!.Generation!.Arms[1].Proposals, p => p.Provenance.Operator == op));
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-boss-discovery-verify", "--run", first]));
        var trial = originalTrials![0];
        await TowerLoadoutArchive.ReplayAsync(first, trial.Id, detailed: true);
        var recipe = HarnessJson.Read<TowerScenario>(Path.Combine(first, "recipes", trial.Recipe + ".json"));
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, new()));
        var input = runner.CreateInput(recipe, trial.Seed, new(), 10);
        var actual = await runner.RunAsync(input);
        var expected = TowerLoadoutArchive.ReadBattle(first, trial.Id, "gzip-json-v1");
        Assert.Equal(HarnessJson.Hash(expected), HarnessJson.Hash(actual));
        await Assert.ThrowsAsync<IOException>(() => TowerBossDiscoveryRun.RunAsync(Root, first, d));
        var reportPath = Path.Combine(first, "discovery.json");
        var tampered = original! with { Generation = original.Generation! with { DiscoveryShortlist = original.Generation.DiscoveryShortlist.Reverse().ToArray() } };
        File.WriteAllText(reportPath, System.Text.Json.JsonSerializer.Serialize(tampered, HarnessJson.Options));
        var filesPath = Path.Combine(first, "files.json");
        var files = HarnessJson.Read<Dictionary<string, string>>(filesPath); files["discovery.json"] = HarnessJson.FileHash(reportPath);
        File.WriteAllText(filesPath, System.Text.Json.JsonSerializer.Serialize(files, HarnessJson.Options));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossDiscoveryRun.VerifyAsync(first));
    }

    [Fact]
    public async Task Cancellation_preserves_partial_trials_without_claiming_completion_and_rejects_improve_mode_before_combat()
    {
        using var temp = new DiscoveryTemp(); using var cancellation = new CancellationTokenSource(); var d = Small();
        var path = Path.Combine(temp.Path, "cancelled");
        var result = await TowerBossDiscoveryRun.RunAsync(Root, path, d, cancellation.Token, message => {
            if (message.Contains("1 evaluated parties", StringComparison.Ordinal)) cancellation.Cancel();
        });
        Assert.Equal("Cancelled", result.Status); Assert.Equal(2, result.ActualBattles);
        Assert.Single(result.Generation!.Arms[0].Evaluations);
        Assert.Equal(2, TowerLoadoutArchive.Verify(path).Count);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossDiscoveryRun.VerifyAsync(path));
        var reference = BalanceHarnessTowerBossDiscoveryContractTests.Reference();
        var party = TowerPartySelection.Choice("supplied", reference.Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
        var improve = d with { Mode = TowerBossDiscovery.Improve, References = [reference], Starts = [new("supplied", reference.Id, party)] };
        var forbidden = Path.Combine(temp.Path, "improve");
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossDiscoveryRun.RunAsync(Root, forbidden, improve));
        Assert.False(Directory.Exists(forbidden));
    }
}
