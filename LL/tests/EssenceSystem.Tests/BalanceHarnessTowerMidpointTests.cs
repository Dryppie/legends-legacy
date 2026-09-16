using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerMidpointTests
{
    private static TowerBalanceDefinition Definition()
    {
        var discovery = BalanceHarnessTowerBossDiscoveryContractTests.Definition();
        var scenario = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario;
        var cohort = new TowerBalanceCohort("fixed-cohort", discovery.Budget, discovery.RequiredPartySize, "fixed-equipment",
            TowerBossDiscovery.EquipmentBudgetHash(scenario.Party));
        // Literal unit-test identifiers; no engine invocation or balance-seed reservation.
        return new(1, TowerMidpointStudy.Version, TowerBalanceEvaluator.IntervalPolicy, discovery.ContentHashes,
            discovery.SettingsHash, HarnessJson.Hash(ExecutionIdentity.Current()), [cohort],
            Enumerable.Range(0, 253).Select(i => new TowerBalanceCellDefinition($"team-{i:D3}", cohort.Id, i < 112 ? "reference" : "generated",
                scenario with { Id = $"team-{i:D3}", Seeds = Enumerable.Range(1, 256).ToArray(), Party = scenario.Party.Select(p => p with {
                    Build = p.Build with { Id = $"team-{i:D3}-character-{p.PartySlot}" } }).ToArray() }, 256)).ToArray(), [-1], 64768);
    }
    private static TowerBalanceEvidence[] Evidence(TowerBalanceDefinition d, int maximum) => d.Cells.Select((c, i) => new TowerBalanceEvidence(
        c.Id, "Complete", HarnessJson.Hash(c.Scenario), HarnessJson.Hash(d.ContentHashes), d.SettingsHash, d.ExecutionHash,
        d.Cohorts[0].RequiredPartySize, c.Scenario.Seeds.Select((seed, j) => new TowerBalanceTrial(seed,
            i == 252 && j < maximum ? BattleOutcome.Victory : BattleOutcome.Draw)).ToArray(), new string('a', 64))).ToArray();

    [Theory]
    [InlineData(0, "Unresolved")] [InlineData(30, "Unresolved")] [InlineData(75, "CandidateForFullFamilyConfirmation")]
    [InlineData(96, "Unresolved")] [InlineData(256, "Unresolved")]
    public void Complete_family_uses_original_per_cell_cutoff_and_preserves_draws(int wins, string status)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("No combat.")).Activate();
        var d = Definition(); var r = TowerMidpointStudy.Reconstruct(d, Evidence(d, wins));
        Assert.Equal(status, r.Status); Assert.Equal(1.10m, r.Factor); Assert.Equal(wins, r.MaximumWins);
        Assert.Equal(253, r.Cells.Count); Assert.All(r.Cells, c => Assert.Equal(256, c.Wins + c.Defeats + c.Draws));
        Assert.Equal(1 - .05 / 1012, r.Cells[252].Interval.Confidence);
        Assert.True(r.Cells[252].Interval.Upper >= TowerBalanceEvaluator.Wilson(wins, 256, 253)!.Upper);
    }

    [Theory]
    [InlineData("missing-cell")] [InlineData("duplicate-cell")] [InlineData("missing-seed")]
    [InlineData("reordered-seed")] [InlineData("content")] [InlineData("recipe")] [InlineData("partial")]
    public void Invalid_evidence_cannot_nominate(string defect)
    {
        var d = Definition(); var e = Evidence(d, 75); var row = e[0];
        if (defect == "missing-cell") e = e.Skip(1).ToArray();
        else if (defect == "duplicate-cell") e = e.Append(row).ToArray();
        else e[0] = defect switch {
            "missing-seed" => row with { Trials = row.Trials.Skip(1).ToArray() },
            "reordered-seed" => row with { Trials = row.Trials.Reverse().ToArray() },
            "content" => row with { ContentHash = new string('b', 64) },
            "recipe" => row with { ScenarioHash = new string('b', 64) }, _ => row with { Status = "Cancelled" } };
        Assert.Throws<InvalidDataException>(() => TowerMidpointStudy.Reconstruct(d, e));
    }

    [Fact]
    public void Midpoint_is_direct_decimal_scaling_and_cannot_compound()
    {
        var baseline = JsonNode.Parse("""{"floors":[{"floorNumber":5,"guardianScaling":{"health":3.5366243328,"offense":4.4702934848,"defense":2.5}},{"floorNumber":6,"unchanged":true}]}""")!;
        var copy = baseline.ToJsonString(); var result = TowerMidpointStudy.Content(baseline);
        var scaling = result["floors"]![0]!["guardianScaling"]!;
        Assert.Equal(3.89028676608m, scaling["health"]!.GetValue<decimal>());
        Assert.Equal(4.91732283328m, scaling["offense"]!.GetValue<decimal>());
        Assert.Equal(2.5, scaling["defense"]!.GetValue<double>()); Assert.Equal(copy, baseline.ToJsonString());
        Assert.Throws<InvalidDataException>(() => TowerMidpointStudy.Content(result));
    }

    private static JsonElement Participants(double factor) => JsonSerializer.SerializeToElement(Enumerable.Range(0, 11).Select(i => new {
        slot = new { side = i == 10 ? "Hostile" : "Friendly", index = i }, health = i == 10 ? (int)(1000 * factor) : 100,
        combatAttributes = new Dictionary<string, double> { ["Power"] = i == 10 ? 100 * factor : 10,
            ["MaxHealth"] = i == 10 ? 1000 * factor : 100, ["Defense"] = 20 }
    }).ToArray());

    [Theory]
    [InlineData("health")] [InlineData("power")] [InlineData("defense")] [InlineData("player")]
    public void Prepared_roster_allows_only_the_two_guardian_scalars(string defect)
    {
        var baseline = Participants(1); var valid = Participants(1.1); TowerMidpointStudy.VerifyParticipants(baseline, valid);
        var changed = JsonNode.Parse(valid.GetRawText())!;
        if (defect == "player") changed[0]!["health"] = 99;
        else if (defect == "health") changed[10]!["health"] = 1099;
        else changed[10]!["combatAttributes"]![defect == "power" ? "Power" : "Defense"] = 999;
        Assert.Throws<InvalidDataException>(() => TowerMidpointStudy.VerifyParticipants(baseline, JsonSerializer.SerializeToElement(changed)));
    }

    [Fact]
    public void Empty_reused_or_missing_history_seeds_are_rejected_without_allocation()
    {
        var empty = new TowerMidpointSeeds(TowerMidpointStudy.Version, [-1], []);
        Assert.Throws<InvalidDataException>(() => TowerMidpointStudy.ValidateSeeds(empty, [-1]));
        Assert.Throws<InvalidDataException>(() => TowerMidpointStudy.ValidateSeeds(empty with { Shared = Enumerable.Repeat(-1, 256).ToArray() }, [-1]));
        Assert.Throws<InvalidDataException>(() => TowerMidpointStudy.ValidateSeeds(empty with { Shared = Enumerable.Range(1, 256).ToArray() }, [-2, -1]));
    }

    private static Task Write(string path, TowerBulkOptions options, CancellationToken ct)
    {
        Assert.Equal(0, options.RetryReserve); Assert.Equal(32, options.ChunkSize); Assert.Equal("prepared-v1", options.ExecutionMode);
        Directory.CreateDirectory(path); var child = new TowerStorageAccountant(path, options.MaximumBytes, ["record", "files.json"]);
        TowerStorageOwnership.Parent!.Attach(child);
        for (var i = 0; i < 2; i++) { TowerPerformanceTrace.BattleStarted(); TowerPerformanceTrace.BattleCompleted(); }
        File.WriteAllText(Path.Combine(path, "record"), "synthetic");
        HarnessJson.WriteNew(Path.Combine(path, "files.json"), new Dictionary<string, string> { ["record"] = HarnessJson.FileHash(Path.Combine(path, "record")) });
        child.Audit(ct); return Task.CompletedTask;
    }
    private static Task Verify(string path, CancellationToken ct)
    { TowerBulkCampaign.VerifyFiles(path, "files.json", true, ct); return Task.CompletedTask; }
    private static Task<TowerMidpointExecution> Execute(Temp t, Func<string, TowerBulkOptions, CancellationToken, Task>? run = null,
        Func<string, CancellationToken, Task>? verify = null, Func<CancellationToken, Task>? preflight = null,
        CancellationToken token = default, double seconds = 120, double setup = 0, long bytes = 8388608) =>
        TowerMidpointRun.Execute(t.Path, 2, setup, seconds, bytes, run ?? Write, verify ?? Verify,
            preflight ?? (_ => Task.CompletedTask), _ => Task.CompletedTask, token);

    [Fact]
    public async Task Native_loop_charges_every_start_and_completion_and_seals_inventory()
    {
        using var t = new Temp(); var result = await Execute(t);
        Assert.Equal(2, result.Started); Assert.Equal(2, result.Completed); Assert.Equal("SCSC", File.ReadAllText(t.P("attempts.bin")));
        TowerBulkCampaign.VerifyFiles(t.Path, TowerMidpointStudy.FinalFiles, true, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidDataException>(() => Execute(t));
        File.WriteAllText(t.P("campaign/record"), "tamper");
        Assert.Throws<InvalidDataException>(() => TowerBulkCampaign.VerifyFiles(t.Path, TowerMidpointStudy.FinalFiles, true, CancellationToken.None));
    }

    [Theory]
    [InlineData("cancel")] [InlineData("write-failure")] [InlineData("verify-failure")] [InlineData("extra-start")]
    public async Task Interrupted_attempts_and_partial_writes_survive_without_retry(string defect)
    {
        using var t = new Temp(); using var stop = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<Exception>(() => Execute(t, (path, options, ct) => {
            if (defect == "verify-failure") return Write(path, options, ct);
            Directory.CreateDirectory(path); TowerPerformanceTrace.BattleStarted(); File.WriteAllText(Path.Combine(path, ".pending"), "preserve");
            if (defect == "cancel") { stop.Cancel(); ct.ThrowIfCancellationRequested(); }
            if (defect == "extra-start") TowerPerformanceTrace.BattleStarted();
            throw new IOException("Injected partial write failure");
        }, defect == "verify-failure" ? (_, _) => throw new InvalidDataException("Injected archive failure") : Verify, token: stop.Token));
        Assert.Equal(defect == "verify-failure" ? "SCSC" : "S", File.ReadAllText(t.P("attempts.bin")));
        Assert.True(File.Exists(t.P("failure.json"))); Assert.False(File.Exists(t.P(TowerMidpointStudy.FinalFiles)));
        if (defect != "verify-failure") Assert.Equal("preserve", File.ReadAllText(t.P("campaign/.pending")));
        await Assert.ThrowsAsync<InvalidDataException>(() => Execute(t));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Preflight_and_reconstruction_cannot_enter_combat(bool duringVerification)
    {
        using var t = new Temp(); Task Forbidden(CancellationToken _) { TowerPerformanceTrace.BattleStarted(); return Task.CompletedTask; }
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(t,
            verify: duringVerification ? (_, ct) => Forbidden(ct) : Verify, preflight: duringVerification ? _ => Task.CompletedTask : Forbidden));
        Assert.Equal(duringVerification ? "SCSC" : "", File.ReadAllText(t.P("attempts.bin")));
    }

    [Fact]
    public async Task Deadline_includes_preflight_and_deducts_all_setup()
    {
        using var t = new Temp();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(t, preflight: ct => Task.Delay(200, ct), seconds: 10, setup: 9.99));
        Assert.Empty(File.ReadAllBytes(t.P("attempts.bin"))); Assert.False(Directory.Exists(t.P("campaign")));
    }

    [Fact]
    public async Task Global_storage_guard_stops_before_an_unaffordable_attempt()
    {
        using var t = new Temp();
        await Assert.ThrowsAsync<InvalidDataException>(() => Execute(t, (path, _, _) => {
            Directory.CreateDirectory(path); File.WriteAllBytes(Path.Combine(path, ".pending"), new byte[4 * 1048576]);
            TowerPerformanceTrace.BattleStarted(); return Task.CompletedTask;
        }, bytes: 4 * 1048576));
        Assert.Empty(File.ReadAllBytes(t.P("attempts.bin"))); Assert.True(File.Exists(t.P("campaign/.pending")));
    }

    [Fact]
    public async Task Precancelled_and_override_commands_do_not_start_or_allocate()
    {
        using var t = new Temp(); using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(t, token: stop.Token)); Assert.False(File.Exists(t.P("started.json")));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerMidpointRun.Command(["tower-midpoint-run", t.Path, "--resume"]));
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-midpoint-" + Guid.NewGuid().ToString("N"));
        public Temp() { Directory.CreateDirectory(Path); File.WriteAllText(P("protocol.json"), "{}"); File.WriteAllText(P("setup-charge.json"), "{}"); }
        public string P(string n) => System.IO.Path.Combine(Path, n);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
