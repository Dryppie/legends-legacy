using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerBulkTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static TowerCompactDefinition Compact(int reserve = 1, int chunk = 2) => new(1, "resume-test", 4 + reserve, chunk,
        [new("first", BalanceHarnessTowerBossDiscoveryContractTests.UserScenario with { Seeds = [910031, 910032] }),
         new("second", BalanceHarnessTowerBossDiscoveryContractTests.UserScenario with { Seeds = [910033, 910034] })]);

    [Theory]
    [InlineData(null)]
    [InlineData("prepared-v1")]
    public async Task Resume_preserves_committed_bytes_charges_lost_attempt_and_matches_uninterrupted_results(string? mode)
    {
        using var temp = new DiscoveryTemp(); using var cancel = new CancellationTokenSource();
        var path = Path.Combine(temp.Path, "resumed"); var d = Compact(); var finished = 0;
        var trace = new TowerPerformanceTrace(done => { if (done && ++finished == 3) cancel.Cancel(); });
        using (trace.Activate()) await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            TowerCompactBundle.CreateAsync(Root, d, path, cancel.Token, executionMode: mode));
        var committed = HarnessJson.FileHash(Path.Combine(path, "chunks/000000/records.json.gz"));
        Assert.Equal(3, TowerCompactBundle.AttemptCount(path));
        Assert.Equal("Invalid", TowerBalanceRuns.Read("first", path, compactCaseId: "first").Status);
        var started = 0; var resumeTrace = new TowerPerformanceTrace(done => { if (!done) started++; });
        using (resumeTrace.Activate()) await TowerCompactBundle.CreateAsync(Root, d, path, executionMode: mode, resume: true);
        Assert.Equal(2, started); Assert.Equal(5, TowerCompactBundle.AttemptCount(path));
        Assert.Equal(committed, HarnessJson.FileHash(Path.Combine(path, "chunks/000000/records.json.gz")));
        var normal = Path.Combine(temp.Path, "normal"); await TowerCompactBundle.CreateAsync(Root, d, normal, executionMode: mode);
        Assert.Equal(HarnessJson.Hash(TowerCompactBundle.ReadSaved(normal).Cases), HarnessJson.Hash(TowerCompactBundle.ReadSaved(path).Cases));
        using (resumeTrace.Activate()) await TowerCompactBundle.CreateAsync(Root, d, path, executionMode: mode, resume: true);
        Assert.Equal(2, started); // A completed resume verifies without executing combat.
        await TowerCompactBundle.ReplayAsync(path, "second", "tower.0001", detailed: true);
    }

    [Theory]
    [InlineData("seeds")]
    [InlineData("recipe")]
    [InlineData("mode")]
    [InlineData("settings")]
    [InlineData("scope")]
    [InlineData("chunk")]
    [InlineData("attempt")]
    [InlineData("torn-journal")]
    [InlineData("pending-chunk")]
    [InlineData("extra-file")]
    public async Task Resume_rejects_changed_identity_or_ambiguous_evidence_before_new_combat(string defect)
    {
        using var temp = new DiscoveryTemp(); using var cancel = new CancellationTokenSource(); var d = Compact();
        var path = Path.Combine(temp.Path, "run");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerCompactBundle.CreateAsync(Root, d, path,
            cancel.Token, _ => cancel.Cancel(), executionMode: "prepared-v1"));
        var requested = d; string? mode = "prepared-v1"; TowerSettings? settings = null;
        if (defect == "seeds") requested = d with { Cases = [d.Cases[0] with { Scenario = d.Cases[0].Scenario with { Seeds = [910032, 910031] } }, d.Cases[1]] };
        else if (defect == "recipe") requested = d with { Cases = [d.Cases[0] with { Scenario = d.Cases[0].Scenario with { Id = "other" } }, d.Cases[1]] };
        else if (defect == "mode") mode = null;
        else if (defect == "settings") settings = TowerBundle.ReadSettings(Root) with { CheckpointIntervalTicks = 11 };
        else if (defect == "scope") File.AppendAllText(Path.Combine(path, "bulk-scope.json"), " ");
        else if (defect == "chunk") File.AppendAllText(Path.Combine(path, "chunks/000000/records.json.gz"), "broken");
        else if (defect == "attempt") File.WriteAllText(Path.Combine(path, "bulk-attempts.jsonl"), "{\"index\":0,\"trialIndex\":0,\"caseId\":\"first\",\"seed\":0}\n");
        else if (defect == "torn-journal") File.AppendAllText(Path.Combine(path, "bulk-attempts.jsonl"), "{");
        else if (defect == "pending-chunk") Directory.CreateDirectory(Path.Combine(path, "chunks/.pending-000001"));
        else File.WriteAllText(Path.Combine(path, "unexpected.json"), "{}");
        var started = 0; using var active = new TowerPerformanceTrace(done => { if (!done) started++; }).Activate();
        await Assert.ThrowsAnyAsync<Exception>(() => TowerCompactBundle.CreateAsync(Root, requested, path,
            settingsOverride: settings, executionMode: mode, resume: true));
        Assert.Equal(0, started);
    }

    [Fact]
    public async Task Lost_work_cannot_reset_the_frozen_combat_cap()
    {
        using var temp = new DiscoveryTemp(); using var cancel = new CancellationTokenSource(); var d = Compact(reserve: 0);
        var path = Path.Combine(temp.Path, "run"); var finished = 0;
        using (new TowerPerformanceTrace(done => { if (done && ++finished == 1) cancel.Cancel(); }).Activate())
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerCompactBundle.CreateAsync(Root, d, path, cancel.Token, executionMode: "prepared-v1"));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerCompactBundle.CreateAsync(Root, d, path, executionMode: "prepared-v1", resume: true));
        Assert.Equal(1, TowerCompactBundle.AttemptCount(path));
    }

    [Fact]
    public async Task Streaming_verification_retains_only_a_chunk_and_cancellation_never_returns_accepted_evidence()
    {
        using var temp = new DiscoveryTemp(); var path = Path.Combine(temp.Path, "run"); var first = Compact().Cases[0];
        var d = new TowerCompactDefinition(1, "stream-test", 16, 2, [first with { Scenario = first.Scenario with { Seeds = Enumerable.Range(910100, 16).ToArray() } }]);
        await TowerCompactBundle.CreateAsync(Root, d, path, executionMode: "prepared-v1");
        var visited = 0; var saved = TowerCompactBundle.Verify(path, visit: (_, _) => visited++);
        Assert.Equal(16, visited); Assert.Equal(2, saved.MaximumBufferedReports);
        Assert.Equal(HarnessJson.Hash(TowerCompactBundle.ReadSaved(path).ResultDigests), HarnessJson.Hash(saved.ResultDigests));
        Assert.Equal(16, saved.Cases["first"].Count);
        using var cancel = new CancellationTokenSource();
        Assert.ThrowsAny<OperationCanceledException>(() => TowerCompactBundle.Verify(path, cancel.Token, visit: (_, _) => cancel.Cancel()));
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-compact-verify", "--run", path]));
    }

    internal static TowerBossDiscoveryDefinition Discovery()
    {
        var d = BalanceHarnessTowerBossDiscoveryContractTests.Definition(); var context = d.Contexts[0].Id;
        return d with { Generation = d.Generation with { CandidatesPerArm = 2, Seeds = [613719], MaximumAttemptsPerArm = 128 },
            Stages = d.Stages with { Shortlist = 2, GeneratedFinalists = 1, DiagnosticCandidates = 0, ReplayReserve = 0,
                Schedules = new Dictionary<string, BossDiscoverySchedule> { [context] = new([81091, 81092], [82091], [83091], []) } } };
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task Discovery_interrupt_resume_reconstructs_identical_proposals_fitness_and_shortlist_without_replaying_completed_fights(int version)
    {
        using var temp = new DiscoveryTemp(); using var cancel = new CancellationTokenSource(); var d = Discovery();
        if (version > 1) d = d with { Generation = d.Generation with {
            PolicyVersion = version == 2 ? TowerBossGeneration.CoordinatedVersion : version == 3 ? TowerBossGeneration.MechanicsVersion : TowerBossGeneration.CoverageVersion,
            Methods = version == 2 ? TowerBossGeneration.CoordinatedMethods : version == 3 ? TowerBossGeneration.MechanicsMethods : TowerBossGeneration.CoverageMethods } };
        var path = Path.Combine(temp.Path, "compact"); var options = new TowerBulkOptions(ChunkSize: 2);
        var partial = await TowerCompactDiscovery.RunAsync(Root, path, d, options, token: cancel.Token, progress: message => {
            if (message.Contains("2/2 trials committed", StringComparison.Ordinal)) cancel.Cancel();
        });
        Assert.Equal("Cancelled", partial.Status);
        Assert.False(File.Exists(Path.Combine(path, "campaign-manifest.json")));
        var started = 0;
        BossDiscoveryRunReport resumed;
        using (new TowerPerformanceTrace(done => { if (!done) started++; }).Activate())
            resumed = await TowerCompactDiscovery.RunAsync(Root, path, d, options, resume: true);
        Assert.Equal("Complete", resumed.Status); Assert.Equal(6, started); Assert.Equal(8, resumed.ActualBattles);
        var normal = await TowerBossDiscoveryRun.RunAsync(Root, Path.Combine(temp.Path, "normal"), d);
        Assert.Equal(HarnessJson.Hash(normal), HarnessJson.Hash(resumed));
        using (new TowerPerformanceTrace(done => { if (!done) started++; }).Activate())
            Assert.Equal(HarnessJson.Hash(resumed), HarnessJson.Hash(await TowerBossDiscoveryRun.VerifyAsync(path)));
        Assert.Equal(6, started);
        var accounting = HarnessJson.Read<TowerBulkAccounting>(Path.Combine(path, "campaign-accounting.json"));
        Assert.Equal(8, accounting.ChargedAttempts); Assert.Equal(0, accounting.RetryOrUncommittedAttempts);
        Assert.All(Directory.GetDirectories(Path.Combine(path, "batches")), batch => Assert.False(Directory.Exists(Path.Combine(batch, "content"))));
        var changed = d with { Generation = d.Generation with { Seeds = [613720] } };
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerCompactDiscovery.RunAsync(Root, path, changed, options, resume: true));
    }

    internal static TowerBalanceDefinition Balance()
    {
        var d = Discovery(); var scenario = BalanceHarnessTowerBossDiscoveryContractTests.UserScenario with { Seeds = [910201, 910202, 910203, 910204] };
        return new(1, "bulk-balance-test", TowerBalanceEvaluator.IntervalPolicy, d.ContentHashes, d.SettingsHash, d.ExecutionHash,
            [new("floor-one", d.Budget, scenario.Party.Count, "fixed", TowerBossDiscovery.EquipmentBudgetHash(scenario.Party))],
            [new("user-control", "floor-one", "reference", scenario, 4)], [], 40);
    }

    [Fact]
    public async Task Frozen_balance_family_resumes_into_the_existing_evaluator_and_verifies_without_combat()
    {
        using var temp = new DiscoveryTemp(); using var cancel = new CancellationTokenSource(); var d = Balance();
        var path = Path.Combine(temp.Path, "balance"); var options = new TowerBulkOptions(ChunkSize: 2);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerCompactBalanceRun.RunAsync(Root, path, d, options,
            token: cancel.Token, progress: _ => cancel.Cancel()));
        var started = 0; TowerBalanceReport report;
        using (new TowerPerformanceTrace(done => { if (!done) started++; }).Activate())
            report = await TowerCompactBalanceRun.RunAsync(Root, path, d, options, resume: true);
        Assert.Equal(2, started); Assert.NotEqual(GoalOutcome.Pass, report.Assessment); // Four samples cannot support this family.
        using (new TowerPerformanceTrace(done => { if (!done) started++; }).Activate())
            Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerCompactBalanceRun.VerifyAsync(path)));
        Assert.Equal(2, started);
        var assessed = TowerBalanceRuns.Evaluate(Path.Combine(path, "definition.json"), Path.Combine(path, "sources.json"), Path.Combine(temp.Path, "evaluation"));
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(assessed));
        Assert.Equal(report.ExitCode, await BalanceHarness.Program.Main(["tower-balance-run-verify", "--run", path]));
    }

    [Fact]
    public async Task Shared_campaign_content_and_late_corruption_are_rejected_and_prepared_replay_stays_independent()
    {
        using var temp = new DiscoveryTemp(); var path = Path.Combine(temp.Path, "balance"); var d = Balance();
        await TowerCompactBalanceRun.RunAsync(Root, path, d, new());
        var batch = Path.Combine(path, "batches/confirmation-000000");
        var replay = await TowerCompactBundle.ReplayAsync(batch, "cell-000", "tower.0001", true);
        Assert.NotEmpty(replay.Battle.EventLog!);
        var contentFile = Path.Combine(path, "content/Data", TowerBundle.Files[0]);
        File.AppendAllText(contentFile, " ");
        Assert.Throws<InvalidDataException>(() => TowerCompactBundle.Verify(batch));
        var started = 0;
        using (new TowerPerformanceTrace(done => { if (!done) started++; }).Activate())
            await Assert.ThrowsAsync<InvalidDataException>(() => TowerCompactBalanceRun.RunAsync(Root, path, d, new(), resume: true));
        Assert.Equal(0, started);
    }

    [Fact]
    public async Task Campaign_storage_limit_and_overreserved_budget_start_no_fights()
    {
        using var temp = new DiscoveryTemp(); var d = Discovery(); var started = 0;
        using var active = new TowerPerformanceTrace(done => { if (!done) started++; }).Activate();
        var result = await TowerCompactDiscovery.RunAsync(Root, Path.Combine(temp.Path, "small-disk"), d,
            new(MaximumBytes: 1048576));
        Assert.Equal("Invalid", result.Status); Assert.Equal(0, started);
        var balance = Balance() with { MaximumBattles = 4 };
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerCompactBalanceRun.RunAsync(Root, Path.Combine(temp.Path, "budget"), balance, new()));
        Assert.Equal(0, started);
    }

    [Fact]
    public void Storage_scan_counts_hidden_and_pending_files_and_observes_changes_between_checks()
    {
        using var temp = new DiscoveryTemp();
        var nested = Path.Combine(temp.Path, "nested"); Directory.CreateDirectory(nested);
        var visible = Path.Combine(temp.Path, "report.json"); File.WriteAllBytes(visible, new byte[17]);
        var hidden = Path.Combine(nested, ".hidden"); File.WriteAllBytes(hidden, new byte[31]);
        File.SetAttributes(hidden, File.GetAttributes(hidden) | FileAttributes.Hidden);
        var pending = Path.Combine(nested, "receipt.pending"); File.WriteAllBytes(pending, new byte[43]);
        Assert.Equal(91, TowerBulkCampaign.StorageBytes(temp.Path));
        Assert.Equal(new[] { hidden, pending, visible }.Order(), TowerBulkCampaign.Paths(temp.Path).Order());
        File.WriteAllBytes(visible, new byte[101]);
        File.Delete(pending);
        Assert.Equal(132, TowerBulkCampaign.StorageBytes(temp.Path));
        Assert.Equal(new[] { hidden, visible }.Order(), TowerBulkCampaign.Paths(temp.Path).Order());
    }

    [Fact]
    public void Cancelled_storage_scan_never_returns_a_partial_byte_total()
    {
        using var temp = new DiscoveryTemp();
        File.WriteAllBytes(Path.Combine(temp.Path, "report.json"), new byte[17]);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => TowerBulkCampaign.StorageBytes(temp.Path, cancel.Token));
    }

    [Fact]
    public async Task Writer_lease_prevents_concurrent_resume_without_changing_evidence()
    {
        using var temp = new DiscoveryTemp(); var path = Path.Combine(temp.Path, "run");
        await TowerCompactBundle.CreateAsync(Root, Compact(), path);
        var hash = HarnessJson.FileHash(Path.Combine(path, TowerCompactBundle.ManifestFile));
        using (TowerCompactBundle.AcquireWriter(path))
            await Assert.ThrowsAsync<IOException>(() => TowerCompactBundle.CreateAsync(Root, Compact(), path, resume: true));
        Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(path, TowerCompactBundle.ManifestFile)));
    }

    [Fact]
    public async Task Explicit_supplied_improvement_keeps_its_provenance_and_scores_on_the_compact_path()
    {
        using var temp = new DiscoveryTemp(); var reference = BalanceHarnessTowerBossDiscoveryContractTests.Reference();
        var independent = Discovery();
        var choice = TowerPartySelection.Choice("supplied", reference.Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
        reference = reference with { Scenario = TowerBossDiscovery.Scenario(independent, independent.Contexts[0].Id, choice, []) };
        var d = TowerBossImprovement.Prepare(independent with { References = [reference] }, [reference.Id]);
        var normal = await TowerBossDiscoveryRun.RunAsync(Root, Path.Combine(temp.Path, "normal"), d);
        var compact = await TowerCompactDiscovery.RunAsync(Root, Path.Combine(temp.Path, "compact"), d, new());
        Assert.Equal("Complete", compact.Status); Assert.Equal(HarnessJson.Hash(normal), HarnessJson.Hash(compact));
        Assert.Equal(HarnessJson.Hash(compact), HarnessJson.Hash(await TowerCompactDiscovery.VerifyAsync(Path.Combine(temp.Path, "compact"))));
    }
}
