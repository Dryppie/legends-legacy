using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerLoadoutSearchTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Fact]
    public async Task Small_legal_space_matches_exhaustive_oracle_and_crosses_a_two_essence_valley()
    {
        var families = new Dictionary<string, string> { ["a"] = "a", ["b"] = "b", ["c"] = "c", ["d"] = "d" };
        int Value(IReadOnlyList<string> ids) => ids.Contains("c") && ids.Contains("d") ? 10
            : ids.Contains("c") || ids.Contains("d") ? -1 : 0;
        // Every individual substitution from a/b is worse; c/d together is better.
        var oracle = (from a in families.Keys from b in families.Keys where a != b select Value(new[] { a, b })).Max();
        async Task<LoadoutSearchResult> Run(string method) => await LoadoutSearch.RunAsync(method, 1701, 12, 4000,
            ["a", "b"], families, [], (ids, _) => Task.FromResult((new LoadoutFitness(Value(ids), Value(ids), 1, 0, 0), (IReadOnlyList<string>)[])));
        foreach (var method in new[] { "guided", "random" })
        {
            var result = await Run(method);
            Assert.Equal(12, result.Evaluations.Count);
            Assert.Equal(oracle, LoadoutSearch.Rank(result.Evaluations).First().Fitness.Wins);
            Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await Run(method)));
            Assert.Equal("candidate-budget", result.StopReason);
        }
        var guided = await Run("guided");
        Assert.Contains(guided.Proposals, p => p.Origin == "double");
        Assert.Contains(guided.Proposals, p => p.Origin == "single");
        Assert.Contains(guided.Proposals, p => p.Origin == "order");
        Assert.Contains(guided.Proposals, p => p.Result == "duplicate-family");
    }

    [Fact]
    public void Double_mutations_can_change_both_essences_without_accepting_a_losing_intermediate()
    {
        var pool = new[] { "c", "d" }; var random = new Random(19);
        var mutated = Enumerable.Range(0, 20).Select(_ => LoadoutSearch.Mutate(["a", "b"], pool, random, "double"));
        Assert.Contains(mutated, ids => ids.Contains("c") && ids.Contains("d"));
        var evaluations = new[] {
            new LoadoutEvaluation("best", "control", null, ["a", "b"], new(1, 1, 1, 0, 0), []),
            new LoadoutEvaluation("near", "single", null, ["a", "c"], new(1, 1, 1, 1, 0), []),
            new LoadoutEvaluation("diverse", "random", null, ["c", "d"], new(0, 0, 1, 100, 0), []) };
        Assert.Equal(new[] { "best", "diverse" }, LoadoutSearch.Beam(evaluations, 2).Select(e => e.Id));
    }

    [Fact]
    public async Task Cancellation_and_exhausted_space_do_not_claim_complete_search()
    {
        var calls = 0; var cts = new CancellationTokenSource();
        await Assert.ThrowsAsync<OperationCanceledException>(() => LoadoutSearch.RunAsync("guided", 17, 5, 100,
            ["a", "b"], new Dictionary<string, string> { ["a"] = "a", ["b"] = "b", ["c"] = "c" }, [],
            (_, _) => { calls++; cts.Cancel(); return Task.FromResult((new LoadoutFitness(0, 0, 1, 0, 0), (IReadOnlyList<string>)[])); }, cts.Token));
        Assert.Equal(1, calls);
        var result = await LoadoutSearch.RunAsync("random", 17, 3, 10, ["a", "b"],
            new Dictionary<string, string> { ["a"] = "a", ["b"] = "b" }, [],
            (_, _) => Task.FromResult((new LoadoutFitness(0, 0, 1, 0, 0), (IReadOnlyList<string>)[])));
        Assert.Equal("proposal-budget", result.StopReason);
        Assert.Equal(2, result.Evaluations.Count);
    }

    [Fact]
    public void All_entry_budgets_cover_every_floor_and_hold_equipment_allies_and_runtime_identities_fixed()
    {
        var definition = TowerLoadoutPilot.Default;
        Assert.Equal(9360, TowerLoadoutPilot.Validate(definition));
        var settings = new TowerSettings(new(), 10);
        var content = new OfflineContent(Root, settings.Threat); var runner = new TowerBattleRunner(Root, content);
        foreach (var gear in definition.Gear)
        {
            var scenarios = TowerLoadoutPilot.Scenarios(Root, Catalogs, gear);
            Assert.Equal(Enumerable.Range(1, 15), scenarios.Select(s => s.FloorNumber));
            foreach (var scenario in scenarios)
            {
                Assert.All(scenario.Party, p => { Assert.Equal(4, p.Build.EssenceIds.Count); Assert.Equal(30, p.Build.CharacterLevel);
                    Assert.Equal(gear.Rank, p.Build.Rank); Assert.Equal(gear.Quality, p.Build.Quality); });
                var original = runner.CreateInput(scenario, scenario.Seeds[0], settings.Threat, 10);
                var ids = scenario.Party[1].Build.EssenceIds.Reverse().ToArray();
                var changed = TowerLoadoutPilot.Apply(scenario, 2, new("reverse", "test", ids), scenario.Seeds);
                var input = runner.CreateInput(changed, changed.Seeds[0], settings.Threat, 10);
                Assert.Equal(original.Party.Select(p => p.Character.Id), input.Party.Select(p => p.Character.Id));
                Assert.Equal(HarnessJson.Hash(original.Party.Select(p => p.Character.Equipment)), HarnessJson.Hash(input.Party.Select(p => p.Character.Equipment)));
                Assert.Equal(HarnessJson.Hash(scenario.Party.Where(p => p.PartySlot != 2)), HarnessJson.Hash(changed.Party.Where(p => p.PartySlot != 2)));
            }
        }
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.Validate(definition with { MaximumBattles = 100 }));
        var schedule = TowerLoadoutPilot.Schedule(17, 2);
        Assert.Throws<InvalidDataException>(() => TowerLoadoutPilot.ValidateSchedules(schedule, schedule));
    }

    [Fact]
    public async Task Pilot_freezes_selection_reconstructs_discovery_and_replays_confirmation_with_tamper_checks()
    {
        using var temp = new Temp();
        var full = TowerLoadoutPilot.Default;
        var definition = full with { Gear = [full.Gear[0]], SearchSeeds = [1701], CandidatesPerArm = 2, DiscoverySamples = 1, ConfirmationSamples = 1 };
        var run = Path.Combine(temp.Path, "run");
        var report = await TowerLoadoutPilot.RunAsync(Root, Catalogs, run, definition);
        Assert.Equal("Complete", report.Status);
        Assert.All(report.Discovery, a => Assert.Equal(30, a.Search.Evaluations.Sum(e => e.Trials.Count)));
        Assert.All(report.Selection[0].Finalists, f => Assert.Equal(Enumerable.Range(1, 15), report.Confirmation.Where(c => c.Candidate == f.Id).Select(c => c.Floor)));
        var trials = TowerLoadoutArchive.Verify(run);
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(TowerLoadoutPilot.ReadReport(run)));
        Assert.Equal(report.ActualBattles, trials.Count);
        Assert.True(trials.TakeWhile(t => t.Stage == "discovery").Count() == trials.Count(t => t.Stage == "discovery"));
        Assert.Empty(trials.Where(t => t.Stage == "discovery").Select(t => t.Seed).Intersect(trials.Where(t => t.Stage == "confirmation").Select(t => t.Seed)));
        var replayId = trials.First(t => t.Stage == "confirmation").Id;
        var replay = await TowerLoadoutArchive.ReplayAsync(run, replayId, true);
        Assert.NotEmpty(replay.Battle.EventLog!);
        await Assert.ThrowsAsync<IOException>(() => TowerLoadoutPilot.RunAsync(Root, Catalogs, run, definition));
        File.AppendAllText(Path.Combine(run, "selection.json"), " ");
        Assert.Throws<InvalidDataException>(() => TowerLoadoutArchive.Verify(run));
    }

    [Fact]
    public async Task Exact_cache_separates_arms_content_rules_and_seeds_and_obeys_hard_limit()
    {
        using var temp = new Temp();
        var pilot = TowerLoadoutPilot.Default with { Gear = [TowerLoadoutPilot.Default.Gear[0]], SearchSeeds = [1701], CandidatesPerArm = 2, DiscoverySamples = 1, ConfirmationSamples = 1 };
        var run = Path.Combine(temp.Path, "run");
        await TowerLoadoutPilot.RunAsync(Root, Catalogs, run, pilot);
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(run, "scope.json"));
        var scenario = TowerLoadoutPilot.Scenarios(Root, Catalogs, pilot.Gear[0])[0] with { Seeds = [1337, 17] };
        var root = Path.Combine(run, "content"); var runner = new TowerBattleRunner(root, new OfflineContent(root, scope.Settings.Threat));
        var input = runner.CreateInput(scenario, 1337, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
        var key = TowerLoadoutArchive.Key(scope, "arm-a", input);
        Assert.NotEqual(key, TowerLoadoutArchive.Key(scope, "arm-b", input));
        Assert.NotEqual(key, TowerLoadoutArchive.Key(scope with { Algorithm = "changed" }, "arm-a", input));
        Assert.NotEqual(key, TowerLoadoutArchive.Key(scope with { ContentHashes = new Dictionary<string, string>() }, "arm-a", input));
        Assert.NotEqual(key, TowerLoadoutArchive.Key(scope, "arm-a", runner.CreateInput(scenario, 17, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks)));
        // Separate cache directory sharing only captured content; no existing trial files are overwritten.
        var cacheRoot = Path.Combine(temp.Path, "cache"); Directory.CreateDirectory(cacheRoot);
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        { var path = Path.Combine(cacheRoot, "content", Path.GetRelativePath(root, file)); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.Copy(file, path); }
        Directory.CreateDirectory(Path.Combine(cacheRoot, "battles")); Directory.CreateDirectory(Path.Combine(cacheRoot, "recipes"));
        var archive = new TowerLoadoutArchive(cacheRoot, scope, 1);
        var first = await archive.EvaluateAsync("arm", "discovery", scenario, 1337, default);
        var repeated = await archive.EvaluateAsync("arm", "discovery", scenario, 1337, default);
        Assert.Equal(first.Trial.Id, repeated.Trial.Id); Assert.Equal(1, archive.CacheHits);
        Assert.Equal(HarnessJson.Hash(first.Report), HarnessJson.Hash(repeated.Report));
        await Assert.ThrowsAsync<InvalidDataException>(() => archive.EvaluateAsync("arm", "discovery", scenario, 17, default));
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-loadout-search-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
