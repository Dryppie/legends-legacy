using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCeilingControllerTests
{
    private static readonly Lazy<TowerBalanceDefinition[]> Definitions = new(() => {
        var discovery = BalanceHarnessTowerBossDiscoveryContractTests.Definition();
        var scenario = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario;
        var cohort = new TowerBalanceCohort("fixed-cohort", discovery.Budget, discovery.RequiredPartySize, "fixed-equipment",
            TowerBossDiscovery.EquipmentBudgetHash(scenario.Party));
        // Literal unit-test identities, never registered as balance seeds or sent to an engine.
        return Enumerable.Range(0, 4).Select(f => new TowerBalanceDefinition(1, TowerCeilingScreenContract.VariantId(f),
            TowerBalanceEvaluator.IntervalPolicy, discovery.ContentHashes, discovery.SettingsHash, HarnessJson.Hash(ExecutionIdentity.Current()), [cohort],
            Enumerable.Range(0, 253).Select(i => new TowerBalanceCellDefinition($"team-{i:D3}", cohort.Id, i < 112 ? "reference" : "generated",
                scenario with { Id = $"team-{i:D3}", Seeds = Enumerable.Range(1, 128).ToArray(), Party = scenario.Party.Select(p => p with {
                    Build = p.Build with { Id = $"team-{i:D3}-character-{p.PartySlot}" } }).ToArray() }, 128)).ToArray(), [-1], 32384)).ToArray();
    });

    private static IReadOnlyList<IReadOnlyList<TowerBalanceEvidence>> Evidence() => Definitions.Value.Select((d, f) =>
        (IReadOnlyList<TowerBalanceEvidence>)d.Cells.Select((c, i) => new TowerBalanceEvidence(c.Id, "Complete", HarnessJson.Hash(c.Scenario),
            HarnessJson.Hash(d.ContentHashes), d.SettingsHash, d.ExecutionHash, d.Cohorts[0].RequiredPartySize,
            c.Scenario.Seeds.Select((seed, n) => new TowerBalanceTrial(seed,
                n < (i == 252 ? f == 0 ? 100 : 38 : 0) ? BattleOutcome.Victory : BattleOutcome.Draw)).ToArray(), new string('a', 64))).ToArray()).ToArray();

    [Fact]
    public void Complete_1012_cell_reconstruction_preserves_draws_and_whole_screen_intervals()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("No combat in controller tests.")).Activate();
        var result = TowerCeilingScreenRun.Reconstruct(Definitions.Value, Evidence());
        Assert.Equal("CandidateForFullFamilyConfirmation", result.Status); Assert.Equal(1.04m, result.SelectedFactor);
        Assert.Equal(1012, result.Cells.Count); Assert.All(result.Cells, c => Assert.Equal(128, c.Wins + c.Defeats + c.Draws));
        Assert.Equal(100, result.Cells[252].Wins); Assert.All(result.Cells, c => Assert.Equal(TowerCeilingScreenContract.ScreenInterval(c.Wins), c.Interval));
        Assert.False(result.Factors[0].Eligible);
    }

    [Theory]
    [InlineData("missing-factor")] [InlineData("missing-cell")] [InlineData("duplicate-cell")] [InlineData("extra-cell")]
    [InlineData("missing-seed")] [InlineData("reordered-seed")] [InlineData("invalid-outcome")] [InlineData("recipe")]
    [InlineData("execution")] [InlineData("content")] [InlineData("party-size")] [InlineData("partial")]
    public void Invalid_archive_evidence_cannot_reach_selection(string defect)
    {
        var e = Evidence().ToArray(); var rows = e[3].ToArray(); var r = rows[0];
        if (defect == "missing-factor") e = e.Take(3).ToArray();
        else if (defect == "missing-cell") e[3] = rows.Skip(1).ToArray();
        else if (defect == "duplicate-cell") e[3] = rows.Skip(1).Append(rows[1]).ToArray();
        else if (defect == "extra-cell") e[3] = rows.Append(r with { CellId = "extra" }).ToArray();
        else
        {
            rows[0] = defect switch {
                "missing-seed" => r with { Trials = r.Trials.Skip(1).ToArray() },
                "reordered-seed" => r with { Trials = r.Trials.Reverse().ToArray() },
                "invalid-outcome" => r with { Trials = r.Trials.Skip(1).Prepend(r.Trials[0] with { Outcome = (BattleOutcome)99 }).ToArray() },
                "recipe" => r with { ScenarioHash = new string('b', 64) }, "execution" => r with { ExecutionHash = new string('b', 64) },
                "content" => r with { ContentHash = new string('b', 64) }, "party-size" => r with { RequiredPartySize = 10 },
                _ => r with { Status = "Cancelled" }
            }; e[3] = rows;
        }
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenRun.Reconstruct(Definitions.Value, e));
    }

    [Theory]
    [InlineData("factor-order")] [InlineData("missing-factor")] [InlineData("identity")] [InlineData("essence-order")]
    [InlineData("empty-seeds")] [InlineData("different-seeds")] [InlineData("cap")]
    public void Bound_definitions_cannot_change_factors_recipes_schedule_or_limits(string defect)
    {
        var d = Definitions.Value.ToArray(); var templates = d.Select(x => x with { Cells = x.Cells.Select(c => c with { Scenario = c.Scenario with { Seeds = [] } }).ToArray() }).ToArray();
        var seeds = new TowerCeilingScreenSeeds(TowerCeilingScreenContract.Version, [-1], d[0].Cells[0].Scenario.Seeds);
        TowerCeilingScreenInputs.ValidateDefinitions(d, templates, seeds);
        if (defect == "factor-order") d = d.Reverse().ToArray();
        else if (defect == "missing-factor") d = d.Skip(1).ToArray();
        else if (defect == "cap") d[3] = d[3] with { MaximumBattles = 32385 };
        else
        {
            var cells = d[3].Cells.ToArray(); var c = cells[0];
            var party = c.Scenario.Party.ToArray();
            if (defect == "identity") party[0] = party[0] with { Build = party[0].Build with { Id = "changed-id" } };
            if (defect == "essence-order") party[0] = party[0] with { Build = party[0].Build with { EssenceIds = party[0].Build.EssenceIds.Reverse().ToArray() } };
            cells[0] = c with { Scenario = c.Scenario with { Party = party, Seeds = defect == "empty-seeds" ? [] : defect == "different-seeds" ? c.Scenario.Seeds.Reverse().ToArray() : c.Scenario.Seeds } };
            d[3] = d[3] with { Cells = cells };
        }
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenInputs.ValidateDefinitions(d, templates, seeds));
    }

    [Fact]
    public void Empty_reused_or_incomplete_seed_binding_is_rejected_without_allocation()
    {
        var historical = Enumerable.Range(-481219, 481219).ToArray();
        var seeds = new TowerCeilingScreenSeeds(TowerCeilingScreenContract.Version, historical, []);
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenInputs.ValidateSeeds(seeds, historical));
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenInputs.ValidateSeeds(seeds with { Shared = historical.Take(128).ToArray() }, historical));
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenInputs.ValidateSeeds(seeds with { Historical = historical.Skip(1).ToArray() }, historical));
    }

    [Theory]
    [InlineData("fights")] [InlineData("seconds")] [InlineData("bytes")] [InlineData("retry")]
    [InlineData("harness")] [InlineData("storage")] [InlineData("setup-nan")]
    public void Frozen_global_limits_and_producing_identity_cannot_drift(string defect)
    {
        var p = new TowerCeilingScreenProtocol(TowerCeilingScreenContract.Version, TowerCeilingScreenInputs.HarnessHash, new string('a', 64),
            129536, 14400, 8589934592, 0, TowerStorageAccountant.Mode, 2, 0, new Dictionary<string, string>());
        TowerCeilingScreenInputs.ValidateLimits(p);
        p = defect switch {
            "fights" => p with { MaximumFights = 129537 }, "seconds" => p with { MaximumSeconds = 14401 },
            "bytes" => p with { MaximumBytes = 8589934593 }, "retry" => p with { Retries = 1 },
            "harness" => p with { HarnessHash = new string('b', 64) }, "storage" => p with { StorageAccounting = "legacy" },
            _ => p with { SetupSeconds = double.NaN }
        };
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenInputs.ValidateLimits(p));
    }

    [Fact]
    public void External_setup_ledger_and_receipts_are_counted_and_cannot_change_after_binding()
    {
        using var temp = new Temp(); File.WriteAllText(temp.P("ledger"), "1234"); File.WriteAllText(temp.P("allocation-receipt"), "ab");
        var files = new Dictionary<string, string> { [temp.P("ledger")] = HarnessJson.FileHash(temp.P("ledger")),
            [temp.P("allocation-receipt")] = HarnessJson.FileHash(temp.P("allocation-receipt")) };
        var request = new TowerCeilingBindingRequest(TowerCeilingScreenContract.Version, "prepared", new string('a',64),
            temp.P("ledger"), files[temp.P("ledger")], new Dictionary<string,string>(), 3, files);
        Assert.Equal(8, TowerCeilingScreenInputs.SetupBytes(request, temp.P("protocol.json")));
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenInputs.SetupBytes(request with { PriorSetupSeconds = 14400 }, temp.P("protocol.json")));
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenInputs.SetupBytes(request with { SetupFiles = new Dictionary<string,string>() }, temp.P("protocol.json")));
        File.WriteAllText(temp.P("ledger"), "1235");
        Assert.Throws<InvalidDataException>(() => TowerCeilingScreenInputs.SetupBytes(request, temp.P("protocol.json")));
    }

    private static Task VerifyFixture(int ordinal, string path, CancellationToken ct)
    { TowerBulkCampaign.VerifyFiles(path, "fixture-files.json", true, ct); return Task.CompletedTask; }
    private static Task WriteFixture(int ordinal, string path, TowerBulkOptions options, CancellationToken ct)
    {
        Assert.Equal(0, options.RetryReserve); Assert.Equal(32, options.ChunkSize); Assert.Equal(TowerStorageAccountant.Mode, options.StorageAccounting);
        Directory.CreateDirectory(path);
        var child = new TowerStorageAccountant(path, options.MaximumBytes, ["record", "fixture-files.json"]);
        Assert.NotNull(TowerStorageOwnership.Parent); TowerStorageOwnership.Parent!.Attach(child);
        for (var j = 0; j < 2; j++) { ct.ThrowIfCancellationRequested(); TowerPerformanceTrace.BattleStarted(); TowerPerformanceTrace.BattleCompleted(); }
        File.WriteAllText(System.IO.Path.Combine(path, "record"), ordinal.ToString());
        HarnessJson.WriteNew(System.IO.Path.Combine(path, "fixture-files.json"), new Dictionary<string, string> {
            ["record"] = HarnessJson.FileHash(System.IO.Path.Combine(path, "record")) });
        child.Audit(ct); return Task.CompletedTask;
    }

    private static Task<TowerCeilingExecutionReceipt> Execute(Temp temp,
        Func<int, string, TowerBulkOptions, CancellationToken, Task>? run = null,
        Func<int, string, CancellationToken, Task>? verify = null, Func<CancellationToken, Task>? preflight = null,
        CancellationToken token = default, double seconds = 120, long bytes = 8388608, double setup = 0) =>
        TowerCeilingScreenRun.ExecuteFactorsAsync(temp.Path, 2, 8, setup, seconds, bytes, run ?? WriteFixture, verify ?? VerifyFixture,
            preflight ?? (_ => Task.CompletedTask), _ => Task.CompletedTask, token);

    [Fact]
    public async Task Actual_controller_loop_shares_ledger_storage_and_verifies_all_four_archives()
    {
        using var temp = new Temp(); var options = new List<TowerBulkOptions>();
        var result = await Execute(temp, (i, p, o, ct) => { options.Add(o); return WriteFixture(i, p, o, ct); });
        Assert.Equal(8, result.Started); Assert.Equal(8, result.Completed); Assert.Equal(4, result.CompletedFactors.Count);
        TowerRescreenAttempts.Verify(temp.P("attempts.bin"), 8);
        Assert.True(options.Zip(options.Skip(1)).All(p => p.Second.MaximumBytes < p.First.MaximumBytes));
        TowerBulkCampaign.VerifyFiles(temp.Path, TowerCeilingScreenInputs.FinalFiles, true, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidDataException>(() => Execute(temp));
        File.WriteAllText(temp.P("scale-100/record"), "X");
        Assert.Throws<InvalidDataException>(() => TowerBulkCampaign.VerifyFiles(temp.Path, TowerCeilingScreenInputs.FinalFiles, true, CancellationToken.None));
    }

    [Theory]
    [InlineData("cancel")] [InlineData("write-failure")] [InlineData("verification-failure")] [InlineData("extra-start")]
    public async Task Interrupted_second_factor_retains_partial_evidence_and_never_runs_later_factors(string defect)
    {
        using var temp = new Temp(); using var stop = new CancellationTokenSource(); var visits = new List<int>();
        await Assert.ThrowsAnyAsync<Exception>(() => Execute(temp, async (i, path, options, ct) => {
            visits.Add(i);
            if (i != 1 || defect == "verification-failure") { await WriteFixture(i, path, options, ct); return; }
            Directory.CreateDirectory(path); TowerPerformanceTrace.BattleStarted(); File.WriteAllText(System.IO.Path.Combine(path, ".pending"), "preserve");
            if (defect == "cancel") { stop.Cancel(); ct.ThrowIfCancellationRequested(); }
            if (defect == "extra-start") TowerPerformanceTrace.BattleStarted();
            throw new IOException("Injected partial write failure.");
        }, (i, path, ct) => i == 1 && defect == "verification-failure" ? throw new InvalidDataException("Injected archive tamper.") : VerifyFixture(i, path, ct), token: stop.Token));
        Assert.Equal(new[] { 0, 1 }, visits); Assert.False(Directory.Exists(temp.P("scale-108")));
        Assert.True(File.Exists(temp.P("failure.json"))); Assert.False(File.Exists(temp.P(TowerCeilingScreenInputs.FinalFiles)));
        Assert.Equal(defect == "verification-failure" ? "SCSCSCSC" : "SCSCS", File.ReadAllText(temp.P("attempts.bin")));
        if (defect != "verification-failure") Assert.Equal("preserve", File.ReadAllText(temp.P("scale-104/.pending")));
        await Assert.ThrowsAsync<InvalidDataException>(() => Execute(temp));
    }

    [Fact]
    public async Task Deadline_covers_preflight_and_setup_time_is_deducted_before_factor_creation()
    {
        using var temp = new Temp(); var calls = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(temp,
            (i, p, o, ct) => { calls++; return WriteFixture(i, p, o, ct); },
            preflight: async ct => await Task.Delay(250, ct), seconds: 10, setup: 9.98));
        Assert.Equal(0, calls); Assert.Empty(File.ReadAllBytes(temp.P("attempts.bin"))); Assert.True(File.Exists(temp.P("failure.json")));
    }

    [Fact]
    public async Task Global_storage_exhaustion_in_one_factor_stops_the_next_factor()
    {
        using var temp = new Temp(); var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => Execute(temp, (i, path, options, ct) => {
            calls++; Directory.CreateDirectory(path); File.WriteAllBytes(System.IO.Path.Combine(path, ".pending"), new byte[4 * 1048576]);
            TowerPerformanceTrace.BattleStarted(); return Task.CompletedTask;
        }, bytes: 4 * 1048576));
        Assert.Equal(1, calls); Assert.True(File.Exists(temp.P("scale-100/.pending"))); Assert.Empty(File.ReadAllBytes(temp.P("attempts.bin")));
    }

    [Fact]
    public async Task Precancelled_run_and_cli_resume_flag_do_not_create_or_charge_output()
    {
        using var temp = new Temp(); using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(temp, token: stop.Token));
        Assert.False(File.Exists(temp.P("started.json")));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerCeilingScreenCommand.ExecuteAsync(["tower-ceiling-run", temp.Path, "--resume"]));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerCeilingScreenCommand.ExecuteAsync(["tower-ceiling-run", temp.Path, "--retry-reserve", "1"]));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Preflight_and_reconstruction_cannot_charge_or_enter_combat(bool duringVerification)
    {
        using var temp = new Temp();
        Task Forbidden(CancellationToken _) { TowerPerformanceTrace.BattleStarted(); return Task.CompletedTask; }
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(temp,
            verify: duringVerification ? (_, _, ct) => Forbidden(ct) : VerifyFixture,
            preflight: duringVerification ? _ => Task.CompletedTask : Forbidden));
        Assert.Equal(duringVerification ? "SCSC" : "", File.ReadAllText(temp.P("attempts.bin")));
        Assert.False(Directory.Exists(temp.P("scale-104")));
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-ceiling-controller-" + Guid.NewGuid().ToString("N"));
        public Temp() { Directory.CreateDirectory(Path); File.WriteAllText(P("protocol.json"), "{}"); File.WriteAllText(P("setup-charge.json"), "{}"); }
        public string P(string name) => System.IO.Path.Combine(Path, name);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
