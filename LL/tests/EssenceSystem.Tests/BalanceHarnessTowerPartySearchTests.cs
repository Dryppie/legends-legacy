using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerPartySearchTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static TowerPartySearchDefinition Small => TowerPartySelection.Default with
    { SearchSeeds = [8563], CandidatesPerArm = 4, CharacterSamples = 1, PartyCandidates = 10, PartySamples = 1, Finalists = 4, ConfirmationSamples = 1 };

    [Fact]
    public void Cost_and_actual_seed_reuse_are_rejected_before_execution()
    {
        Assert.Equal(29760, TowerPartySelection.Validate(TowerPartySelection.Default));
        Assert.Equal(873, TowerPartySelection.Default.ExcludedCombatSeeds.Count);
        Assert.Throws<InvalidDataException>(() => TowerPartySelection.Validate(Small with { MaximumBattles = 1 }));
        Assert.Throws<InvalidDataException>(() => TowerPartySelection.Validate(Small with { PartySeed = Small.CharacterSeed }));
        var used = TowerLoadoutPilot.Schedule(Small.ConfirmationSeed, 1)[15][0];
        Assert.Throws<InvalidDataException>(() => TowerPartySelection.Validate(Small with { ExcludedCombatSeeds = [used] }));
    }

    [Fact]
    public void Robust_selection_rejects_being_carried_and_retains_a_floor_specialist()
    {
        PartyFloorScore Cell(string context, int floor, int wins) => new(context, floor,
            Enumerable.Range(0, 10).Select(i => i < wins).ToArray(), 0, 50, .5, []);
        var control = new[] { Cell("weak", 1, 5), Cell("strong", 1, 10), Cell("weak", 2, 0), Cell("strong", 2, 2) };
        var carried = new[] { Cell("weak", 1, 3), Cell("strong", 1, 10), Cell("weak", 2, 0), Cell("strong", 2, 8) };
        var robust = new[] { Cell("weak", 1, 8), Cell("strong", 1, 10), Cell("weak", 2, 2), Cell("strong", 2, 4) };
        var specialist = new[] { Cell("weak", 1, 2), Cell("strong", 1, 9), Cell("weak", 2, 10), Cell("strong", 2, 2) };
        PartyMeasurement Row(string id, IReadOnlyList<PartyFloorScore> cells) => new(id, TowerPartySelection.Fitness(cells, control), cells);
        var rows = new[] { Row("control", control), Row("carried", carried), Row("robust", robust), Row("specialist", specialist) };
        Assert.Equal("robust", TowerPartySelection.Rank(rows).First().Id);
        Assert.Equal(-2, rows[1].Fitness.EntryWins);
        Assert.Equal(0, rows[2].Fitness.EntryWins); // Strong context is already at its ceiling.
        Assert.Equal("specialist", TowerPartySelection.Specialist(rows, rows[0], ["control", "robust"]));
    }

    [Fact]
    public async Task Historical_starts_receive_equal_cost_and_random_still_explores()
    {
        var families = Enumerable.Range(0, 12).ToDictionary(i => "e" + i, i => "f" + i);
        IReadOnlyList<string> control = ["e0", "e1", "e2", "e3"];
        IReadOnlyList<IReadOnlyList<string>> starts = [["e4", "e5", "e6", "e7"], ["e8", "e9", "e10", "e11"]];
        async Task<LoadoutSearchResult> Run(string method) => await LoadoutSearch.RunAsync(method, 8563, 8, 100,
            control, families, starts, (ids, ct) => Task.FromResult((new LoadoutFitness(0, 0, 30, 0, 0), (IReadOnlyList<string>)[])), sharedStarts: true);
        var guided = await Run("guided"); var random = await Run("random");
        Assert.Equal(8, guided.Evaluations.Count); Assert.Equal(8, random.Evaluations.Count);
        Assert.Equal(HarnessJson.Hash(guided.Evaluations.Take(3)), HarnessJson.Hash(random.Evaluations.Take(3)));
        Assert.All(random.Evaluations.Skip(3), c => Assert.Equal("random-restart", c.Origin));
        Assert.Equal(HarnessJson.Hash(random), HarnessJson.Hash(await Run("random")));
    }

    [Fact]
    public void Joint_changes_keep_independent_strikers_and_all_other_members_and_budgets_fixed()
    {
        var contexts = TowerPartySearch.Contexts(Root, Catalogs, 0);
        var scenario = contexts.Values.Last().Single(s => s.FloorNumber == 15);
        var builds = new Dictionary<int, IReadOnlyList<string>> { [2] = TowerPartySelection.CandidateC.Essences,
            [3] = TowerLoadoutReliability.Default.FixedCandidates![0].Essences, [4] = TowerLoadoutReliability.Default.FixedCandidates![1].Essences };
        var changed = TowerPartySelection.Apply(scenario, builds, [123]);
        Assert.Equal(scenario.Party.Count, changed.Party.Count);
        Assert.False(changed.Party[2].Build.EssenceIds.SequenceEqual(changed.Party[3].Build.EssenceIds));
        foreach (var pair in scenario.Party.Zip(changed.Party))
            Assert.Equal(HarnessJson.Hash(pair.First), HarnessJson.Hash(pair.Second with { Build = pair.Second.Build with { EssenceIds = pair.First.Build.EssenceIds } }));
        Assert.Equal(HarnessJson.Hash(contexts.Values.First()[0].Party), HarnessJson.Hash(contexts.Values.Last()[0].Party));
        Assert.NotEqual(HarnessJson.Hash(contexts.Values.First()[14].Party), HarnessJson.Hash(contexts.Values.Last()[14].Party));
    }

    [Fact]
    public async Task Complete_three_stage_search_reconstructs_and_replays_and_rejects_resealed_invented_scores()
    {
        using var temp = new Temp(); var output = Path.Combine(temp.Path, "run");
        var report = await TowerPartySearch.RunAsync(Root, Catalogs, output, Small);
        Assert.Equal("Complete", report.Status);
        Assert.Equal(8, report.Characters.Count);
        Assert.All(report.Characters, arm => Assert.Equal(120, arm.Search.Evaluations.Sum(e => e.Trials.Count)));
        Assert.All(report.Confirmation, row => Assert.Equal(30, row.Cells.Count));
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerPartySearch.VerifyAsync(output)));
        var trials = TowerLoadoutArchive.Verify(output);
        foreach (var stage in new[] { "character", "party", "confirmation" })
            Assert.NotEmpty((await TowerLoadoutArchive.ReplayAsync(output, trials.First(t => t.Stage == stage).Id, true)).Battle.EventLog!);
        var path = Path.Combine(output, "party-search.json");
        var node = JsonNode.Parse(File.ReadAllText(path))!; node["confirmation"]![0]!["fitness"]!["wins"] = 999;
        File.WriteAllText(path, node.ToJsonString());
        var hashes = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(output, "files.json")); hashes["party-search.json"] = HarnessJson.FileHash(path);
        File.WriteAllText(Path.Combine(output, "files.json"), JsonSerializer.Serialize(hashes, HarnessJson.Options));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPartySearch.VerifyAsync(output));
    }

    [Fact]
    public void A_method_party_deduplicated_against_a_historical_start_still_receives_confirmation()
    {
        var controlBuilds = TowerPartySelection.Slots.ToDictionary(slot => slot, slot => (IReadOnlyList<string>)new[] { "a", "b", "c", "d" });
        var historicalBuilds = new Dictionary<int, IReadOnlyList<string>>(controlBuilds) { [2] = TowerPartySelection.CandidateC.Essences };
        var historical = TowerPartySelection.Choice("historical-C", historicalBuilds);
        var parties = new List<PartyChoice> { TowerPartySelection.Choice("control", controlBuilds), historical };
        for (var i = 0; i < 4; i++) parties.Add(TowerPartySelection.Choice("other", new Dictionary<int, IReadOnlyList<string>>(controlBuilds)
            { [1] = ["alternative-" + i, "b", "c", "d"] }));
        var rows = parties.Select((p, i) => new PartyMeasurement(p.Id, new(i == 1 ? -10 : i, 0, 1, 0, 0), [])).ToArray();
        var arms = TowerPartySelection.Slots.SelectMany(slot => new[] { "guided", "random" }.Select(method =>
            new CharacterSearchArm(slot, new(method, Small.SearchSeeds[0], "candidate-budget",
                [new(HarnessJson.Hash(historicalBuilds[slot]), "control", null, historicalBuilds[slot], new(0, 0, 1, 0, 0), [])], []), []))).ToArray();
        var selected = TowerPartySelection.Select(Small, parties, rows, arms);
        Assert.Contains(selected, p => p.Id == historical.Id);
        Assert.Equal(parties[0].Id, selected[0].Id);
        Assert.Equal(Small.Finalists, selected.Count);
    }

    [Fact]
    public async Task Cancellation_preserves_completed_trials_without_publishing_a_finalist()
    {
        using var temp = new Temp(); using var cancel = new CancellationTokenSource();
        var output = Path.Combine(temp.Path, "cancelled");
        await Assert.ThrowsAsync<OperationCanceledException>(() => TowerPartySearch.RunAsync(Root, Catalogs, output, Small, cancel.Token,
            message => { if (message.Contains("candidates;", StringComparison.Ordinal)) cancel.Cancel(); }));
        Assert.Equal(30, TowerLoadoutArchive.Verify(output).Count);
        Assert.Equal("Cancelled", HarnessJson.Read<PartySearchReport>(Path.Combine(output, "party-search.json")).Status);
        Assert.False(File.Exists(Path.Combine(output, "selection.json")));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPartySearch.VerifyAsync(output));
        Assert.NotEmpty((await TowerLoadoutArchive.ReplayAsync(output, "trial-000001", true)).Battle.EventLog!);
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-party-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
