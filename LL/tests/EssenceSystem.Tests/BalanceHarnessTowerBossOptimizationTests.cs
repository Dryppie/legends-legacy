using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBossOptimizationTests
{
    private static readonly BossBehavior NoBehavior = new(0, 0, 0, 0, 0);
    private static PartyFloorScore Cell(string context, int floor, bool[] wins, double health = 40, double survival = 60) =>
        new(context, floor, wins, 0, health, survival, wins.Select((_, i) => $"{context}/{floor}/{i}").ToArray());
    private static PartyChoice Party(string source, params (int Slot, string[] Ids)[] builds) =>
        TowerPartySelection.Choice(source, builds.ToDictionary(p => p.Slot, p => (IReadOnlyList<string>)p.Ids));
    private static BossMeasurement Measure(PartyChoice party) => new(party.Id,
        new(party.Builds.Sum(p => p.Value.Count(id => id == "e")), 40, 60, 10), [Cell("base", 3, [true])], NoBehavior);

    [Fact]
    public void Boss_specialist_beats_all_floor_winner_without_changing_generalist_objective()
    {
        PartyFloorScore[] control = [Cell("base", 1, [false]), Cell("base", 3, [false])];
        PartyFloorScore[] specialist = [Cell("base", 1, [false]), Cell("base", 3, [true])];
        PartyFloorScore[] generalist = [Cell("base", 1, [true]), Cell("base", 3, [false])];
        Dictionary<int, double> weights = new() { [3] = 1 };
        var boss = new[] { new BossMeasurement("specialist", TowerBossOptimization.Fitness(specialist, control, weights), specialist, NoBehavior),
            new BossMeasurement("generalist", TowerBossOptimization.Fitness(generalist, control, weights), generalist, NoBehavior) };
        Assert.Equal("specialist", TowerBossOptimization.Rank(boss).First().Id);
        Assert.Equal("generalist", TowerPartySelection.Rank(new[] {
            new PartyMeasurement("specialist", TowerPartySelection.Fitness(specialist, control), specialist),
            new PartyMeasurement("generalist", TowerPartySelection.Fitness(generalist, control), generalist) }).First().Id);
        Assert.NotEqual(TowerPartySelection.Version, TowerBossOptimization.Version);
    }

    [Fact]
    public void Weighted_target_gain_uses_worst_context_and_ignores_transfer_in_all_tiebreaks()
    {
        PartyFloorScore[] control = [Cell("a", 3, [false, false]), Cell("a", 7, [false, false]),
            Cell("b", 3, [true, false]), Cell("b", 7, [true, true])];
        PartyFloorScore[] cells = [Cell("a", 3, [true, true], 20, 80), Cell("a", 7, [false, false], 60, 40),
            Cell("b", 3, [true, false], 20, 80), Cell("b", 7, [false, false], 60, 40),
            Cell("a", 1, [true], 900, -900)];
        Dictionary<int, double> weights = new() { [3] = 3, [7] = 1 };
        var fitness = TowerBossOptimization.Fitness(cells, control, weights, 12);
        Assert.Equal(-.25, fitness.PrimaryGain);
        Assert.Equal(30, fitness.GuardianHealth);
        Assert.Equal(70, fitness.Survival);
        Assert.Equal(12, fitness.VictoryDuration);
        Assert.Equal(fitness, TowerBossOptimization.Fitness(cells.Reverse().ToArray(), control.Reverse().ToArray(), weights, 12));
    }

    [Fact]
    public void Objective_rejects_incomplete_or_unpaired_matrices_and_fast_losses_cannot_win_duration()
    {
        PartyFloorScore[] control = [Cell("base", 3, [false, false])];
        Dictionary<int, double> weights = new() { [3] = 1 };
        var lose = TowerBossOptimization.Fitness(control, control, weights, 1);
        Assert.Equal(double.MaxValue, lose.VictoryDuration);
        Assert.Throws<InvalidDataException>(() => TowerBossOptimization.Fitness([], control, weights));
        Assert.Throws<InvalidDataException>(() => TowerBossOptimization.Fitness([control[0], control[0]], control, weights));
        Assert.Throws<InvalidDataException>(() => TowerBossOptimization.Fitness([Cell("base", 3, [false])], control, weights));
        Assert.Throws<InvalidDataException>(() => TowerBossOptimization.Fitness(control, control, new Dictionary<int, double> { [3] = 0 }));
        Assert.Throws<InvalidDataException>(() => TowerBossOptimization.Fitness(control, control, weights, double.PositiveInfinity));
        var tied = new[] { new BossMeasurement("b", lose, control, NoBehavior), new BossMeasurement("a", lose, control, NoBehavior) };
        Assert.Equal("a", TowerBossOptimization.Rank(tied).First().Id);
        Assert.NotEmpty(HarnessJson.Hash(tied));
    }

    [Fact]
    public void Archive_preserves_five_distinct_measured_approaches_without_sacrificing_primary_gain()
    {
        BossMeasurement Row(string id, double health, BossBehavior behavior, double gain = .5) =>
            new(id, new(gain, health, 50, 10), [Cell("base", 3, [true])], behavior);
        var primary = Row("primary", 0, new(100, .5, 20, 30, 5));
        var rows = new[] { primary, Row("adds", 1, new(10, .6, 21, 31, 6)), Row("prevent", 2, new(110, .7, 100, 32, 7)),
            Row("sustain", 3, new(120, .1, 23, 33, 8)), Row("denial", 4, new(130, .8, 24, 34, 100)),
            Row("loser", 0, new(0, 0, 1000, 1000, 1000), .49), Row("duplicate-behavior", 7, primary.Behavior) };
        var archive = TowerBossOptimization.Archive(rows);
        Assert.Equal(5, archive.Count);
        Assert.Equal("primary", archive[0].Id);
        Assert.Equal(new[] { "primary", "adds", "prevent", "sustain", "denial" }, archive.Select(a => a.Id));
        Assert.Equal(HarnessJson.Hash(archive), HarnessJson.Hash(TowerBossOptimization.Archive(rows.Reverse())));
        Assert.Single(TowerBossOptimization.Archive([primary, primary with { Id = "same", Fitness = primary.Fitness with { GuardianHealth = 1 } }]));
    }

    [Theory]
    [InlineData("random")] [InlineData("legacy")] [InlineData("joint")] [InlineData("graph")]
    public async Task Every_arm_is_deterministic_has_identical_starts_and_equal_full_party_cost(string method)
    {
        Dictionary<string, string> families = new() { ["a"] = "a", ["a2"] = "a", ["b"] = "b", ["c"] = "c", ["d"] = "d", ["e"] = "e" };
        PartyChoice[] starts = [Party("control", (1, ["a", "b"]), (2, ["c", "d"]), (3, ["e", "a"])),
            Party("retained", (1, ["b", "a"]), (2, ["d", "c"]), (3, ["e", "a"]))];
        var calls = 0;
        Task<BossMeasurement> Evaluate(PartyChoice p, CancellationToken _) { calls++; return Task.FromResult(Measure(p)); }
        var result = await TowerBossOptimization.RunAsync(method, 971, 24, 2000, starts, families, [1, 2], [("e", "d")], Evaluate);
        Assert.Equal(24, calls);
        Assert.Equal("candidate-budget", result.StopReason);
        Assert.Equal(starts.Select(p => p.Id), result.Parties.Take(2).Select(p => p.Id));
        Assert.Equal(24, result.Parties.Select(p => p.Id).Distinct().Count());
        Assert.All(result.Parties, p => {
            Assert.Equal(starts[0].Builds[3], p.Builds[3]);
            Assert.All(p.Builds, b => Assert.Equal(b.Value.Count, b.Value.Select(id => families[id]).Distinct().Count()));
        });
        var again = await TowerBossOptimization.RunAsync(method, 971, 24, 2000, starts, families.Reverse().ToDictionary(p => p.Key, p => p.Value),
            [2, 1], [("e", "d")], Evaluate);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(again));
    }

    [Fact]
    public async Task Random_small_space_oracle_visits_each_legal_ordered_recipe_and_reports_exhaustion()
    {
        Dictionary<string, string> families = new() { ["a"] = "a", ["b"] = "b", ["c"] = "c" };
        PartyChoice[] starts = [Party("control", (1, ["a", "b"]))];
        var result = await TowerBossOptimization.RunAsync("random", 601, 7, 1000, starts, families, [1], [],
            (p, _) => Task.FromResult(Measure(p)));
        var legal = families.Keys.SelectMany(a => families.Keys.Where(b => b != a).Select(b => Party("oracle", (1, [a, b])).Id));
        Assert.Equal(legal.Order(), result.Parties.Select(p => p.Id).Order());
        Assert.Equal("proposal-budget", result.StopReason);
        Assert.Equal(1000, result.Proposals.Count);
        Assert.Contains(result.Proposals, p => p.Result == "duplicate");
    }

    [Fact]
    public async Task Fifty_character_ten_slot_sampling_rejects_families_per_character_without_changing_the_budget()
    {
        var families = Enumerable.Range(0, 20).SelectMany(i => new[] {
            new KeyValuePair<string, string>($"essence-{i}-a", $"family-{i}"),
            new KeyValuePair<string, string>($"essence-{i}-b", $"FAMILY-{i}") }).ToDictionary(p => p.Key, p => p.Value);
        var baseline = TowerPartySelection.Choice("control", Enumerable.Range(1, 50).ToDictionary(slot => slot,
            _ => (IReadOnlyList<string>)Enumerable.Range(0, 10).Select(i => $"essence-{i}-a").ToArray()));
        var calls = 0;
        var result = await TowerBossOptimization.RunAsync("random", 8441, 5, 5, [baseline], families, Enumerable.Range(1, 50).ToArray(), [],
            (p, _) => { calls++; return Task.FromResult(Measure(p)); });
        Assert.Equal(5, calls);
        Assert.Equal("candidate-budget", result.StopReason);
        Assert.All(result.Parties, p => {
            Assert.Equal(50, p.Builds.Count);
            Assert.All(p.Builds.Values, ids => Assert.Equal(10, ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count()));
        });
    }

    [Fact]
    public async Task Graph_can_propose_enabler_and_consumer_across_characters_as_one_complete_party()
    {
        Dictionary<string, string> families = new() { ["a"] = "a", ["b"] = "b", ["enable"] = "enable", ["consume"] = "consume" };
        PartyChoice[] starts = [Party("control", (1, ["a"]), (2, ["b"]), (3, ["a"]))];
        Task<BossMeasurement> Evaluate(PartyChoice p, CancellationToken _)
        {
            var partners = p.Builds.Where(b => b.Key is 1 or 2).SelectMany(b => b.Value).ToArray();
            var synergy = partners.Contains("enable") && partners.Contains("consume");
            return Task.FromResult(Measure(p) with { Fitness = new(synergy ? 1 : 0, 40, 60, 10), Cells = [Cell("base", 3, [synergy])] });
        }
        var result = await TowerBossOptimization.RunAsync("graph", 88, 12, 1000, starts, families, [1, 2], [("enable", "consume")],
            Evaluate);
        var coordinated = Assert.Single(result.Proposals, p => p.Attempt == 1);
        Assert.Equal("graph-pair-cross-character", coordinated.Origin);
        Assert.Equal("evaluated", coordinated.Result);
        Assert.Contains("enable", coordinated.Party.Builds.Where(p => p.Key != 3).SelectMany(p => p.Value));
        Assert.Contains("consume", coordinated.Party.Builds.Where(p => p.Key != 3).SelectMany(p => p.Value));
        Assert.Equal(starts[0].Builds[3], coordinated.Party.Builds[3]);
        Assert.Equal(1, result.Evaluations.Single(e => e.Id == coordinated.Party.Id).Fitness.PrimaryGain);
        Assert.Equal(0, result.Evaluations[0].Fitness.PrimaryGain);
        Assert.Equal(1, TowerBossOptimization.Rank(result.Evaluations).First().Fitness.PrimaryGain);
    }

    [Fact]
    public async Task Same_owner_graph_pairs_always_place_both_ingredients_on_one_character()
    {
        Dictionary<string, string> families = new() { ["a"] = "a", ["b"] = "b", ["c"] = "c", ["d"] = "d",
            ["enable"] = "enable", ["consume"] = "consume" };
        PartyChoice[] starts = [Party("control", (1, ["a", "b"]), (2, ["c", "d"]))];
        var pair = (Enabler: "enable", Consumer: "consume");
        var result = await TowerBossOptimization.RunAsync("graph", 88, 24, 1000, starts, families, [1, 2], [pair],
            (p, _) => Task.FromResult(Measure(p)), sameOwnerPairs: new HashSet<(string Enabler, string Consumer)> { pair });
        var coordinated = result.Proposals.Where(p => p.Origin.StartsWith("graph-pair-", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(coordinated);
        Assert.All(coordinated, p => {
            Assert.Equal("graph-pair-same-character", p.Origin);
            Assert.Contains(p.Party.Builds.Values, ids => ids.Contains(pair.Enabler) && ids.Contains(pair.Consumer));
        });
    }

    [Theory]
    [InlineData(1)] [InlineData(2)]
    public async Task Same_owner_graph_pair_cannot_overwrite_its_enabler_in_a_one_slot_character(int characters)
    {
        Dictionary<string, string> families = new() { ["a"] = "a", ["b"] = "b", ["enable"] = "enable", ["consume"] = "consume" };
        var baseline = TowerPartySelection.Choice("control", Enumerable.Range(1, characters).ToDictionary(slot => slot,
            _ => (IReadOnlyList<string>)new[] { "a" }));
        var pair = (Enabler: "enable", Consumer: "consume");
        var result = await TowerBossOptimization.RunAsync("graph", 88, 4, 1000, [baseline], families, Enumerable.Range(1, characters).ToArray(), [pair],
            (p, _) => Task.FromResult(Measure(p)), sameOwnerPairs: new HashSet<(string Enabler, string Consumer)> { pair });
        Assert.Equal("candidate-budget", result.StopReason);
        Assert.DoesNotContain(result.Proposals, p => p.Origin.StartsWith("graph-pair-", StringComparison.Ordinal));
        Assert.All(result.Parties, p => Assert.All(p.Builds.Values, ids => Assert.Single(ids)));
    }

    [Fact]
    public async Task Invalid_starts_fixed_changes_and_cancellation_stop_before_any_evaluation()
    {
        Dictionary<string, string> families = new() { ["a"] = "a", ["a2"] = "a", ["b"] = "b" };
        var baseline = Party("control", (1, ["a", "b"]), (2, ["a", "b"])); var calls = 0;
        Task<BossMeasurement> Evaluate(PartyChoice p, CancellationToken _) { calls++; return Task.FromResult(Measure(p)); }
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossOptimization.RunAsync("joint", 1, 2, 20,
            [baseline, Party("illegal", (1, ["a", "a2"]), (2, ["a", "b"]))], families, [1], [], Evaluate));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossOptimization.RunAsync("joint", 1, 2, 20,
            [baseline, Party("fixed-change", (1, ["a", "b"]), (2, ["b", "a"]))], families, [1], [], Evaluate));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossOptimization.RunAsync("joint", 1, 2, 20,
            [baseline with { Id = "tampered" }], families, [1], [], Evaluate));
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerBossOptimization.RunAsync("joint", 1, 2, 20,
            [baseline], families, [1], [], Evaluate, cts.Token));
        Assert.Equal(0, calls);
    }
}
