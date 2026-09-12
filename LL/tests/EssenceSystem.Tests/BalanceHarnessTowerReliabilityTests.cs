using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerReliabilityTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Fact]
    public void Reliability_contract_budgets_both_contexts_and_rejects_historical_seed_reuse()
    {
        var d = TowerLoadoutReliability.Default;
        Assert.Equal(40080, TowerLoadoutPilot.Validate(d));
        Assert.Equal(9360, TowerLoadoutPilot.Validate(TowerLoadoutPilot.Default));
        Assert.Equal(4, d.SearchSeeds.Count); Assert.Equal(6, d.DiscoverySamples); Assert.Equal(40, d.ConfirmationSamples);
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.Validate(d with { MaximumBattles = 40079 }));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.Validate(d with { ConfirmationSeed = 202609102 }));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.Validate(d with { DiscoverySeed = 202609101 }));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.Validate(d with { AllyContexts = ["balanced", "balanced"] }));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.Validate(d with { FixedCandidates = [d.FixedCandidates![0] with { PreviousParty = true }] }));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.Validate(d with { FixedCandidates = [d.FixedCandidates![0] with { Id = "misleading" }] }));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.Validate(TowerLoadoutPilot.Default with { AllyContexts = ["balanced"] }));
    }

    [Fact]
    public void Alternative_allies_preserve_target_equipment_and_runtime_identities_on_every_floor()
    {
        var d = TowerLoadoutReliability.Default;
        var contexts = TowerLoadoutReliability.Contexts(d, Root, Catalogs);
        Assert.Equal(2, contexts.Count);
        var settings = new TowerSettings(new(), 10);
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, settings.Threat));
        var a = contexts["standard-rank-1--balanced"]; var b = contexts["standard-rank-1--previous-05"];
        Assert.Equal(Enumerable.Range(1, 15), a.Select(s => s.FloorNumber));
        foreach (var (original, alternative) in a.Zip(b))
        {
            Assert.Equal(HarnessJson.Hash(original.Party[1]), HarnessJson.Hash(alternative.Party[1]));
            Assert.Contains("essence.hobgoblin", original.Party[0].Build.EssenceIds);
            Assert.Contains("essence.horned_wolf", alternative.Party[0].Build.EssenceIds);
            var before = runner.CreateInput(original, original.Seeds[0], settings.Threat, 10);
            var after = runner.CreateInput(alternative, alternative.Seeds[0], settings.Threat, 10);
            Assert.Equal(before.Party.Select(p => p.Character.Id), after.Party.Select(p => p.Character.Id));
            Assert.Equal(HarnessJson.Hash(before.Party.Select(p => p.Character.Equipment)), HarnessJson.Hash(after.Party.Select(p => p.Character.Equipment)));
        }
        // Even a target that normally receives a candidate-05 substitution remains unchanged.
        var guardianContexts = TowerLoadoutReliability.Contexts(d with { TargetPartySlot = 1 }, Root, Catalogs);
        Assert.Equal(HarnessJson.Hash(guardianContexts.Values.First()[0].Party[0]), HarnessJson.Hash(guardianContexts.Values.Last()[0].Party[0]));
    }

    [Fact]
    public void Selection_transfers_both_context_winners_and_historical_candidates_without_reselection()
    {
        var d = TowerLoadoutReliability.Default with { SearchSeeds = [4111] };
        var contexts = TowerLoadoutReliability.Contexts(d, Root, Catalogs);
        LoadoutMethodResult Arm(string cohort, string method, string[] ids) => new(cohort, new(method, 4111, "candidate-budget",
            [new(HarnessJson.Hash(ids), "test", null, ids, new(1, 1, 1, 0, 0), [])], []));
        var original = contexts.Values.First()[0].Party[1].Build.EssenceIds.ToArray();
        var reversed = original.Reverse().ToArray();
        var arms = new[] { Arm(contexts.Keys.First(), "guided", original), Arm(contexts.Keys.First(), "random", reversed),
            Arm(contexts.Keys.Last(), "guided", d.FixedCandidates![0].Essences.ToArray()), Arm(contexts.Keys.Last(), "random", d.FixedCandidates[1].Essences.ToArray()) };
        var selection = TowerLoadoutReliability.Select(d, contexts, arms);
        Assert.Equal(2, selection.Count); Assert.Equal(4, selection[0].Finalists.Count);
        Assert.Equal(HarnessJson.Hash(selection[0].Finalists), HarnessJson.Hash(selection[1].Finalists));
        Assert.Contains(selection[0].Finalists, f => f.Essences.SequenceEqual(reversed));
        Assert.All(selection.SelectMany(s => s.Finalists), f => Assert.False(f.PreviousParty));
    }

    [Fact]
    public async Task Compressed_study_rebuilds_both_stages_replays_and_rejects_falsified_confirmation()
    {
        using var temp = new Temp();
        var d = TowerLoadoutReliability.Default with { SearchSeeds = [4111], CandidatesPerArm = 2, DiscoverySamples = 1, ConfirmationSamples = 1 };
        var run = Path.Combine(temp.Path, "run");
        var report = await TowerLoadoutPilot.RunAsync(Root, Catalogs, run, d);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(TowerLoadoutPilot.ReadReport(run)));
        Assert.Equal(HarnessJson.Hash(report.Selection[0].Finalists), HarnessJson.Hash(report.Selection[1].Finalists));
        Assert.All(report.Discovery, a => Assert.Equal(30, a.Search.Evaluations.Sum(e => e.Trials.Count)));
        var trials = TowerLoadoutArchive.Verify(run);
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(run, "scope.json"));
        Assert.Equal("gzip-json-v1", scope.ReportStorage);
        Assert.Empty(Directory.GetFiles(Path.Combine(run, "battles"), "*.json"));
        Assert.Equal(trials.Count, Directory.GetFiles(Path.Combine(run, "battles"), "*.json.gz").Length);
        foreach (var trial in new[] { trials.First(), trials.First(t => t.Stage == "confirmation"), trials.Last() })
        {
            var restored = TowerLoadoutArchive.ReadBattle(run, trial.Id, scope.ReportStorage);
            var plain = Path.Combine(temp.Path, trial.Id + ".json"); HarnessJson.WriteNew(plain, restored);
            Assert.True(new FileInfo(Path.Combine(run, "battles", trial.Id + ".json.gz")).Length < new FileInfo(plain).Length / 2);
            var replay = await TowerLoadoutArchive.ReplayAsync(run, trial.Id, true);
            Assert.Equal(HarnessJson.Hash(restored.Battle.Summary), HarnessJson.Hash(replay.Battle.Summary));
        }
        // Recompute only the outer file checksum: semantic verification must still catch invented wins.
        var path = Path.Combine(run, "pilot.json");
        var node = JsonNode.Parse(File.ReadAllText(path))!; node["confirmation"]![0]!["wins"] = 123;
        File.WriteAllText(path, node.ToJsonString());
        var hashes = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(run, "files.json")); hashes["pilot.json"] = HarnessJson.FileHash(path);
        File.WriteAllText(Path.Combine(run, "files.json"), System.Text.Json.JsonSerializer.Serialize(hashes, HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.ReadReport(run));
    }

    [Fact]
    public async Task Cancelled_study_keeps_completed_compressed_trials_and_cannot_be_used_as_complete_evidence()
    {
        using var temp = new Temp(); using var cancel = new CancellationTokenSource();
        var d = TowerLoadoutReliability.Default with { SearchSeeds = [4111], CandidatesPerArm = 2, DiscoverySamples = 1, ConfirmationSamples = 1 };
        var run = Path.Combine(temp.Path, "cancelled");
        await Assert.ThrowsAsync<OperationCanceledException>(() => TowerLoadoutPilot.RunAsync(Root, Catalogs, run, d, cancel.Token,
            message => { if (message.Contains("candidate measured", StringComparison.Ordinal)) cancel.Cancel(); }));
        Assert.Equal(15, TowerLoadoutArchive.Verify(run).Count);
        Assert.Equal("Cancelled", HarnessJson.Read<TowerLoadoutPilotReport>(Path.Combine(run, "pilot.json")).Status);
        Assert.False(File.Exists(Path.Combine(run, "selection.json")));
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.ReadReport(run));
        Assert.NotEmpty((await TowerLoadoutArchive.ReplayAsync(run, "trial-000001", true)).Battle.EventLog!);
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-reliability-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
