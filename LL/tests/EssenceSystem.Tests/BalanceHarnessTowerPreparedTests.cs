using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerPreparedTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static TowerScenario Scenario(string id) => TowerContractJson.Read<TowerPerformanceDefinition>(
        Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures", TowerPerformanceBenchmark.Fixture)))
        .Cases.Single(c => c.Id == id).Scenario;

    [Theory]
    [InlineData("weak")]
    [InlineData("strong")]
    [InlineData("summon-status")]
    [InlineData("long-duration")]
    public async Task Reuse_preserves_complete_results_in_reversed_and_concurrent_seed_order(string id)
    {
        var settings = TowerBundle.ReadSettings(Root);
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, settings.Threat));
        var scenario = Scenario(id); scenario = scenario with { Seeds = scenario.Seeds.Take(3).ToArray() };
        var input = runner.CreateInput(scenario, scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks);
        var trace = new TowerPerformanceTrace(); TowerPreparedBattle prepared;
        using (trace.Activate()) prepared = await runner.PrepareReusableAsync(input);
        Assert.DoesNotContain(trace.Snapshot(), s => s.Path.StartsWith("battle", StringComparison.Ordinal));
        var before = prepared.TemplateDigest();
        var expected = new Dictionary<int, string>();
        foreach (var seed in scenario.Seeds)
            expected[seed] = TowerCompactBundle.ReportHash(await runner.RunAsync(runner.CreateInput(scenario, seed, settings.Threat, settings.CheckpointIntervalTicks)));
        using (trace.Activate())
        {
            foreach (var seed in scenario.Seeds.Reverse()) Assert.Equal(expected[seed], TowerCompactBundle.ReportHash(await prepared.RunAsync(seed)));
            var concurrent = await Task.WhenAll(scenario.Seeds.Select(seed => Task.Run(() => prepared.RunAsync(seed))));
            Assert.All(concurrent, report => Assert.Equal(expected[report.Battle.Seed], TowerCompactBundle.ReportHash(report)));
        }
        Assert.Equal(before, prepared.TemplateDigest());
        Assert.DoesNotContain(trace.Snapshot(), s => s.Path.EndsWith("engine.playback-including-checkpoints", StringComparison.Ordinal));
        Assert.Equal(1, trace.Snapshot().Where(s => s.Path.EndsWith("combat.prepare-and-validate", StringComparison.Ordinal)).Sum(s => s.Calls));
        Assert.Equal(6, trace.Snapshot().Where(s => s.Path.EndsWith("engine.simulation-without-checkpoints", StringComparison.Ordinal)).Sum(s => s.Calls));
        var detailed = await prepared.RunAsync(scenario.Seeds[0], detailed: true);
        Assert.NotEmpty(detailed.Battle.EventLog!);
        Assert.Equal(expected[detailed.Battle.Seed], TowerCompactBundle.ReportHash(detailed with { Battle = detailed.Battle with { EventLog = null } }));
    }

    [Fact]
    public async Task Caller_mutation_and_returned_results_cannot_poison_later_battles()
    {
        var settings = TowerBundle.ReadSettings(Root);
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, settings.Threat));
        var scenario = Scenario("strong"); var seeds = new List<int> { scenario.Seeds[0] };
        scenario = scenario with { Seeds = seeds };
        var input = runner.CreateInput(scenario, seeds[0], settings.Threat, settings.CheckpointIntervalTicks);
        var expected = TowerCompactBundle.ReportHash(await runner.RunAsync(input));
        var prepared = await runner.PrepareReusableAsync(input); var seed = seeds[0];
        seeds.Clear(); settings.Threat.Enabled = !settings.Threat.Enabled;
        ((IList<TowerPartyMember>)input.Party)[0] = input.Party[0] with { PartyNumber = 99 };
        var first = await prepared.RunAsync(seed);
        Assert.Equal(expected, TowerCompactBundle.ReportHash(first));
        Assert.All(first.Battle.Summary.Statistics.SelectMany(s => s.Abilities), a => Assert.Null(a.Definition));
        first.Battle.Summary.Friendly[0].Health = 0;
        ((IList<Domain.Models.Combat.AbilityStats>)first.Battle.Summary.Statistics[0].Abilities).Clear();
        Assert.Equal(expected, TowerCompactBundle.ReportHash(await prepared.RunAsync(seed)));
        await Assert.ThrowsAsync<InvalidDataException>(() => prepared.RunAsync(seed + 1));
    }

    [Fact]
    public async Task Cancellation_does_not_leave_reused_state_or_a_locked_worker()
    {
        var settings = TowerBundle.ReadSettings(Root);
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, settings.Threat));
        var scenario = Scenario("summon-status"); var seed = scenario.Seeds[0];
        var input = runner.CreateInput(scenario, seed, settings.Threat, settings.CheckpointIntervalTicks);
        var prepared = await runner.PrepareReusableAsync(input);
        var before = prepared.TemplateDigest(); using var cancel = new CancellationTokenSource();
        var trace = new TowerPerformanceTrace(done => { if (!done) cancel.Cancel(); });
        using (trace.Activate()) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => prepared.RunAsync(seed, token: cancel.Token));
        Assert.Equal(before, prepared.TemplateDigest());
        Assert.Equal(TowerCompactBundle.ReportHash(await runner.RunAsync(input)), TowerCompactBundle.ReportHash(await prepared.RunAsync(seed)));
    }

    [Theory]
    [InlineData("rules")]
    [InlineData("party")]
    [InlineData("settings")]
    public async Task Reuse_requires_a_valid_frozen_input_and_matching_provider(string defect)
    {
        var settings = TowerBundle.ReadSettings(Root);
        var runner = new TowerBattleRunner(Root, new OfflineContent(Root, settings.Threat));
        var scenario = Scenario("weak");
        var input = runner.CreateInput(scenario, scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks);
        if (defect == "rules") input = input with { Rules = input.Rules with { MaxTicks = 1 } };
        else if (defect == "party") input = input with { Party = input.Party.Reverse().ToArray() };
        else input.ThreatAndTanking.Enabled = !input.ThreatAndTanking.Enabled;
        await Assert.ThrowsAsync<InvalidDataException>(() => runner.PrepareReusableAsync(input));
    }

    [Theory]
    [InlineData(null, "prepared-v1")]
    [InlineData("tower-compact-v1", "unknown")]
    public void Invalid_execution_selection_is_rejected(string? format, string mode) =>
        Assert.Throws<InvalidDataException>(() => TowerPerformanceBenchmark.ValidateExecution(format, mode));

    [Fact]
    public async Task Full_tick_limit_preserves_draw_statistics_and_reuse()
    {
        var root = Path.Combine(Path.GetTempPath(), "tower-prepared-timeout-" + Guid.NewGuid().ToString("N"));
        try
        {
            TowerBundle.CopyContent(Root, root, default);
            var path = Path.Combine(root, "Data", TowerBattleRunner.FloorFile);
            var data = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
            var scaling = data["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 1)!["guardianScaling"]!;
            scaling["health"] = 1000; scaling["offense"] = 0.000001; scaling["defense"] = 1000; scaling["resistance"] = 1000;
            File.WriteAllText(path, data.ToJsonString(HarnessJson.Options));
            var scenario = Scenario("strong");
            scenario = scenario with { FloorNumber = 1, Party = scenario.Party.Take(5).ToArray(), Seeds = [17] };
            var settings = TowerBundle.ReadSettings(Root);
            var runner = new TowerBattleRunner(root, new OfflineContent(root, settings.Threat));
            var input = runner.CreateInput(scenario, 17, settings.Threat, settings.CheckpointIntervalTicks);
            var normal = await runner.RunAsync(input);
            Assert.Equal("TickLimit", normal.Battle.Summary.TerminationReason);
            Assert.Equal(6000, normal.Battle.Summary.DurationTicks);
            var prepared = await runner.PrepareReusableAsync(input);
            Assert.Equal(TowerCompactBundle.ReportHash(normal), TowerCompactBundle.ReportHash(await prepared.RunAsync(17)));
            Assert.Equal(TowerCompactBundle.ReportHash(normal), TowerCompactBundle.ReportHash(await prepared.RunAsync(17)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
