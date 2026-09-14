using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerSearchBenchmarkTests
{
    private static BossDiscoveryMeasurement Row(string id, double deficit, double denial, double health = 50, double wins = 0) =>
        new(id, new(wins, health, 0, double.MaxValue), [], new(0, deficit, 0, 0, denial));

    [Fact]
    public void Archive_partitions_all_32_cells_preserves_best_and_uses_original_rank_with_order_independent_input()
    {
        var rows = Enumerable.Range(0, 4).SelectMany(d => new[] { 0d, 1, 31, 101, 301, 1001, 3001, 10001 }
            .Select((denial, i) => Row($"{d}-{i}", d / 4d, denial))).ToArray();
        var best = Row("best", .01, 1, health: 90, wins: 1);
        var duplicate = Row("weaker", 0, 0, health: 99);
        var selected = TowerBehaviorArchive.Select(rows.Concat([best, duplicate]));
        Assert.Equal(32, selected.Length); Assert.Equal("best", selected[0]); Assert.DoesNotContain("weaker", selected);
        Assert.DoesNotContain("0-1", selected);
        Assert.Equal(selected, TowerBehaviorArchive.Select(rows.Concat([best, duplicate]).Reverse()));
        Assert.Equal((3, 7), TowerBehaviorArchive.Cell(new(0, 1, 0, 0, 20000)));
        Assert.Equal((1, 1), TowerBehaviorArchive.Cell(new(0, .25, 0, 0, 30)));
        Assert.Empty(TowerBehaviorArchive.Select([]));
    }

    [Theory]
    [InlineData(-.01, 0)] [InlineData(1.01, 0)] [InlineData(double.NaN, 0)]
    [InlineData(.5, -1)] [InlineData(.5, double.PositiveInfinity)]
    public void Archive_rejects_missing_or_invalid_behavior(double deficit, double denial) =>
        Assert.Throws<InvalidDataException>(() => TowerBehaviorArchive.Select([Row("bad", deficit, denial)]));

    private static readonly Lazy<TowerBossInventoryReport> Inventory = new(() => TowerBossInventory.Create(TestContentPaths.FindApiRoot(), new()));
    private static TowerBossDiscoveryDefinition Definition(int candidates = 32)
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(5, 5);
        return d with { Generation = new(TowerBossGeneration.DepthBehaviorMethods, [17], candidates, 1024, 4,
            TowerBossDiscovery.Objective, TowerBossGeneration.DepthBehaviorVersion), Stages = d.Stages with { Shortlist = 6 } };
    }

    [Fact]
    public async Task Baselines_match_v4_at_each_budget_and_independent_boundary_preserves_metadata_and_provenance()
    {
        var definition = Definition(); var input = TowerBossDiscovery.GenerationInputs(definition);
        var mechanics = TowerBossPartyGenerator.FromInventory(input, Inventory.Value);
        Task<BossDiscoveryMeasurement> Score(PartyChoice party, string _, CancellationToken token)
        {
            var value = Convert.ToInt32(party.Id[..2], 16);
            return Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0, value / 3d, 0)
                with { Behavior = new(0, value / 255d, 0, 0, value * 10) });
        }
        var result = await TowerBossGeneration.RunAsync(input, mechanics, Score);
        Assert.Equal("Complete", result.Status);
        Assert.Equal(new[] { 8, 32, 32 }, result.Arms.Select(a => a.Evaluations.Count));
        for (var i = 0; i < 2; i++)
        {
            var old = input with { Generation = input.Generation with { PolicyVersion = TowerBossGeneration.CoverageVersion,
                Methods = TowerBossGeneration.CoverageMethods, CandidatesPerArm = i == 0 ? 8 : 32 } };
            Assert.Equal(HarnessJson.Hash(mechanics), HarnessJson.Hash(TowerBossPartyGenerator.FromInventory(old, Inventory.Value)));
            var baseline = await TowerBossGeneration.RunAsync(old, mechanics, Score);
            var normalized = JsonSerializer.Deserialize<BossGenerationArm>(JsonSerializer.Serialize(result.Arms[i], HarnessJson.Options)
                .Replace(TowerBossGeneration.DepthBehaviorMethods[i], "coverage-joint"), HarnessJson.Options)!;
            Assert.Equal(HarnessJson.Hash(baseline.Arms[1]), HarnessJson.Hash(normalized));
        }
        var provenance = result.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray();
        TowerBossDiscovery.ValidateProvenance(definition, provenance);
        Assert.All(provenance, p => Assert.Empty(p.ReferenceIds));
        Assert.All(result.Arms.SelectMany(a => a.Proposals).Where(p => p.Result == "evaluated"), p => TowerBossDiscovery.ValidateParty(definition, p.Party!));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(definition,
            provenance.Select(p => p with { ReferenceIds = ["control"] }).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(definition,
            provenance.Select(p => p.Operator == "fresh-coverage" ? p with { Operator = "fresh-defense" } : p).ToArray()));
        var first = result.Arms[0].Proposals.First(p => p.Result == "evaluated").Party!;
        var reference = new BossBenchmarkReference("outside-control", definition.Contexts[0].Id,
            TowerBossDiscovery.Scenario(definition, definition.Contexts[0].Id, first, []), "Fixture", new string('a', 64));
        Assert.Equal(HarnessJson.Hash(input), HarnessJson.Hash(TowerBossDiscovery.GenerationInputs(definition with { References = [reference] })));
        Assert.Throws<InvalidDataException>(() => new TowerBossPartyGenerator(input, mechanics with { Coverage = null }));
    }

    [Fact]
    public void Asymmetric_cost_is_charged_exactly_and_policy_mismatches_are_rejected()
    {
        var d = Definition(384) with { Generation = Definition(384).Generation with { Seeds = [17, 18, 19], MaximumAttemptsPerArm = 8192 },
            Stages = Definition(384).Stages with { Shortlist = 18 } };
        Assert.Equal(20736, TowerBossDiscovery.Validate(d).Discovery);
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { MaximumBattles = 20735 }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { CandidatesPerArm = 383 } }));
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d with { Generation = d.Generation with { Methods = d.Generation.Methods.Reverse().ToArray() } }));
        Assert.Equal(TowerBossGeneration.Version, BalanceHarnessTowerBossDiscoveryContractTests.Definition().Generation.PolicyVersion);
    }

    [Fact]
    public async Task Cancellation_and_partial_discovery_cannot_produce_a_benchmark_selection()
    {
        var d = Definition(); var input = TowerBossDiscovery.GenerationInputs(d);
        using var cancel = new CancellationTokenSource(); cancel.Cancel(); var evaluations = 0;
        var result = await TowerBossGeneration.RunAsync(input, TowerBossPartyGenerator.FromInventory(input, Inventory.Value),
            (party, _, _) => { evaluations++; return Task.FromResult(BalanceHarnessTowerBossGenerationTests.Measure(input, party, 0)); }, cancel.Token);
        Assert.Equal("Cancelled", result.Status); Assert.Equal(0, evaluations);
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.Select(d, new("Cancelled", 0, 0, 0, result, null)));
    }

    [Fact]
    public void Ledger_includes_unused_and_constructor_arrays_but_not_scalar_metadata()
    {
        using var ledger = JsonDocument.Parse("{\"count\":999,\"old\":[1,2],\"unused\":{\"constructor\":[2,3],\"validation\":[4]}}");
        Assert.Equal(new[] { 1, 2, 3, 4 }, TowerSearchBenchmark.History(ledger.RootElement));
    }

    [Fact]
    public async Task New_policy_runs_real_compact_combat_reconstructs_and_rejects_modified_evidence_without_fights()
    {
        using var temp = new DiscoveryTemp(); var d = Definition(8);
        d = d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key,
            p => p.Value with { Discovery = p.Value.Discovery.Take(1).ToArray() }) } };
        var output = Path.Combine(temp.Path, "campaign");
        var run = await TowerCompactDiscovery.RunAsync(TestContentPaths.FindApiRoot(), output, d, new(32, 0, 120, 134217728));
        Assert.Equal("Complete", run.Status); Assert.Equal(18, run.ActualBattles);
        Assert.Equal(new[] { 2, 8, 8 }, run.Generation!.Arms.Select(a => a.Evaluations.Count));
        using var noCombat = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Unexpected verification fight")).Activate();
        Assert.Equal(HarnessJson.Hash(run), HarnessJson.Hash(await TowerCompactDiscovery.VerifyAsync(output)));
        File.AppendAllText(Path.Combine(output, "discovery.json"), " ");
        await Assert.ThrowsAnyAsync<Exception>(() => TowerCompactDiscovery.VerifyAsync(output));
    }

    private static TowerSearchBenchmarkSelection Selection()
    {
        var arms = TowerBossGeneration.DepthBehaviorMethods.SelectMany((method, index) => Enumerable.Range(0, 3)
            .Select(seed => new TowerSearchArm(method, seed, $"m{index}-{seed}", $"m{index}-{seed}-secondary"))).ToArray();
        var scenario = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario with { Seeds = [] };
        return new(arms, arms.SelectMany(a => new[] { a.Primary, a.Secondary }).Select(id => new TowerSearchSelected(id, scenario, []))
            .Concat(Enumerable.Range(0, 6).Select(i => new TowerSearchSelected("control-" + i, scenario,
                [new("saved-control", null, null, null, "control-" + i)]))).ToArray());
    }

    private static TowerBalanceReport ScreenReport(TowerSearchBenchmarkSelection selection, Func<string, int> wins) =>
        new(1, "fixture", "fixture", "fixture", "fixture", selection.Family.Count, .95, GoalOutcome.Fail, 1, [], [],
            selection.Family.Select(p => new TowerBalanceCellCheck(p.Id, "cohort", "generated", GoalOutcome.Fail, 64, 64, 64,
                wins(p.Id), 64 - wins(p.Id), 0, false, false, false, false, null, null, [], null)).ToArray(), "fixture");

    [Fact]
    public void Screen_requires_two_fixed_primaries_and_never_uses_successful_secondaries_or_incomplete_cells()
    {
        var selection = Selection();
        var secondaries = ScreenReport(selection, id => id.EndsWith("secondary") ? 64 : 0);
        Assert.False(TowerSearchBenchmark.Screen(selection, "control-0", secondaries).ContinueValidation);
        var one = ScreenReport(selection, id => id == "m1-0" ? 7 : 0);
        Assert.False(TowerSearchBenchmark.Screen(selection, "control-0", one).ContinueValidation);
        var two = ScreenReport(selection, id => id is "m1-0" or "m1-1" ? 7 : id == "control-0" ? 13 : 0);
        Assert.True(TowerSearchBenchmark.Screen(selection, "control-0", two).ContinueValidation);
        var tooFar = ScreenReport(selection, id => id is "m1-0" or "m1-1" ? 7 : id == "control-0" ? 14 : 0);
        Assert.False(TowerSearchBenchmark.Screen(selection, "control-0", tooFar).ContinueValidation);
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.Screen(selection, "control-0", two with { Cells = two.Cells.Skip(1).ToArray() }));
    }

    [Fact]
    public void Joint_quality_separates_viability_improvement_anchor_recovery_and_observed_ceiling()
    {
        var selection = Selection();
        var evidence = selection.Family.Select(p => new TowerBalanceEvidence(p.Id, "Complete", "", "", "", "", 10,
            Enumerable.Range(1, 256).Select(seed => new TowerBalanceTrial(seed,
                seed <= (p.Id.StartsWith("m1-") ? 110 : p.Id.StartsWith("m2-") ? 150 : p.Id.StartsWith("control-") ? 100 : 0)
                    ? BattleOutcome.Victory : BattleOutcome.Defeat)).ToArray(), "")).ToArray();
        var quality = TowerSearchBenchmark.Quality(selection, "control-0", evidence);
        Assert.Equal("Fail", quality.JointFamilyAssessment); // 150/256 must remain a breach, even when search succeeds.
        Assert.Equal("Pass", quality.Methods[0].Reliability);
        Assert.All(quality.Primaries, p => Assert.True(p.Viable));
        Assert.All(quality.Rates.Values, rate => Assert.Equal(1 - .025 / 24, rate.Confidence, 12));
        var left = evidence[0].Trials; var right = evidence[1].Trials.Reverse().ToArray();
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.Pair(left, right));
        Assert.Throws<InvalidDataException>(() => TowerSearchBenchmark.Quality(selection, "control-0", evidence.Skip(1).ToArray()));
    }
}
