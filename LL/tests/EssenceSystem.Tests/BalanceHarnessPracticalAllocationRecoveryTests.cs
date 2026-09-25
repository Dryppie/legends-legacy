using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPracticalAllocationRecoveryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-practical-allocation-recovery-fixture-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Allocation recovery entered combat.")).Activate();
    private static readonly Lazy<(int Id, long Ticks)[]> Exited = new(() => Enumerable.Range(0, 2).Select(_ => {
        using var process = Process.Start(new ProcessStartInfo("dotnet", "--version") {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true })!;
        var identity = (process.Id, process.StartTime.ToUniversalTime().Ticks);
        Assert.True(process.WaitForExit(10000)); Assert.Equal(0, process.ExitCode); return identity;
    }).ToArray());
    private sealed record Input(TowerPracticalRequest Source, TowerPracticalRecoveryRequest Recovery);
    public BalanceHarnessPracticalAllocationRecoveryTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);
    private static Dictionary<string, string> Inventory(string path) => Directory.EnumerateFiles(path)
        .ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash);
    private TowerPracticalRecoveryRequest Seal(TowerPracticalRecoveryRequest q)
    {
        File.WriteAllText(q.ManifestPath, Json(Inventory(q.StudyRoot)));
        return q with { ManifestHash = HarnessJson.FileHash(q.ManifestPath) };
    }
    private static void Edit(string path, string key, object value)
    {
        var json = JsonNode.Parse(File.ReadAllText(path))!; json[key] = JsonSerializer.SerializeToNode(value, HarnessJson.Options);
        File.WriteAllText(path, json.ToJsonString(HarnessJson.Options));
    }
    private async Task<Input> Failed(string boundary = "allocation-candidate", int occurrence = 1, Func<string, int, int>? candidate = null, bool threeReferences = false, bool exploration = false, string explorationVersion = TowerReferenceExploration.Version)
    {
        var owners = Exited.Value;
        var d = BalanceHarnessPracticalAllocationTests.Template(exploration ? BalanceHarnessReferenceExplorationTests.Definition(policyVersion: explorationVersion) : threeReferences ? BalanceHarnessThreeReferenceTests.Definition()
            : I.Definition() with { ExcludedCombatSeeds = [-987] });
        var prior = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(prior, new { historical = new[] { -987 } });
        var sourcePath = Path.Combine(root, "template.json"); HarnessJson.WriteNew(sourcePath, d);
        var content = Path.Combine(root, "content"); Directory.CreateDirectory(content);
        var files = new Dictionary<string, string> { [prior] = HarnessJson.FileHash(prior) };
        var source = new TowerPracticalRequest(threeReferences ? TowerPracticalSearch.ThreeReferenceAllocationVersion : TowerPracticalSearch.AllocationVersion, content, sourcePath, HarnessJson.FileHash(sourcePath),
            root, Path.Combine(root, "failed"), files, 300, 32 * 1048576, 3, 128,
            Allocation: new(19, "literal-allocation-fixture", 8, 32, 256));
        Directory.CreateDirectory(source.OutputRoot);
        var now = DateTimeOffset.UtcNow;
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(source), now, now.AddSeconds(source.MaximumSeconds - source.PriorSeconds), owners[0].Id, owners[0].Ticks);
        HarnessJson.WriteNew(Path.Combine(source.OutputRoot, "request.json"), source);
        HarnessJson.WriteNew(Path.Combine(source.OutputRoot, "launch.json"), launch);
        using var stop = new CancellationTokenSource(); var seen = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerPracticalSearch.RunOperation(source, launch,
            _ => new(d, new(files, [-987])), (_, _, _, _) => throw new InvalidOperationException("Recovery attempted study execution."),
            (_, _) => throw new InvalidOperationException("Recovery attempted native verification."), stop.Token,
            stage => { if (stage == boundary && ++seen == occurrence) stop.Cancel(); }, candidate ?? FixtureHost.AllocationCandidate));
        var workerPath = Path.Combine(source.OutputRoot, "worker-start.json");
        var worker = HarnessJson.Read<TowerPracticalWorkerStart>(workerPath);
        File.WriteAllText(workerPath, Json(worker with { ProcessId = owners[1].Id, ProcessStartedUtcTicks = owners[1].Ticks }));
        var recovery = new TowerPracticalRecoveryRequest(TowerPracticalReservationRecovery.AllocationVersion, source.OutputRoot,
            Path.Combine(root, "manifest.json"), new string('0', 64), Path.Combine(root, "receipt.json"), 30);
        return new(source, Seal(recovery));
    }
    private static TowerPracticalRecoveryRecord Recover(TowerPracticalRecoveryRequest q, Func<string, int, int>? candidate = null)
        => TowerPracticalReservationRecovery.RecoverCore(q, candidate: candidate ?? FixtureHost.AllocationCandidate);
    private static TowerPracticalRecoveryRecord Verify(TowerPracticalRecoveryRequest q, Func<string, int, int>? candidate = null)
        => TowerPracticalReservationRecovery.VerifyCore(q.ReceiptPath, candidate: candidate ?? FixtureHost.AllocationCandidate);

    [Theory]
    [InlineData("allocation-candidate", 1, false)]
    [InlineData("before-complete", 297, false)]
    [InlineData("allocation-candidate", 1, true)]
    [InlineData("before-complete", 297, true)]
    [InlineData("allocation-candidate", 1, true, TowerReferenceExploration.OffsetVersion)]
    [InlineData("before-complete", 297, true, TowerReferenceExploration.OffsetVersion)]
    public async Task Three_reference_allocations_preserve_closed_prefixes_and_versioned_receipts(string boundary, int count, bool exploration, string explorationVersion = TowerReferenceExploration.Version)
    {
        var input = await Failed(boundary, threeReferences: true, exploration: exploration, explorationVersion: explorationVersion);
        var before = Json(Inventory(input.Recovery.StudyRoot));
        var recovered = Recover(input.Recovery);
        Assert.Equal(count, recovered.Reserved.Length);
        Assert.Equal("AbandonedPermanentlyReserved", recovered.Status);
        Assert.Equal(Json(recovered), Json(Verify(input.Recovery)));
        Assert.Equal(before, Json(Inventory(input.Recovery.StudyRoot)));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.AllocateAndRun(input.Source));
    }

    [Theory]
    [InlineData("allocation-pending", 1, 0)]
    [InlineData("allocation-candidate", 1, 1)]
    [InlineData("allocation-candidate", 9, 9)]
    [InlineData("allocation-candidate", 41, 41)]
    [InlineData("allocation-candidate", 296, 296)]
    [InlineData("allocation-candidate", 297, 297)]
    [InlineData("allocation-complete", 1, 297)]
    [InlineData("before-complete", 1, 297)]
    public async Task Closed_prefixes_recover_only_recorded_values_without_refunds_or_source_changes(string boundary, int occurrence, int count)
    {
        var input = await Failed(boundary, occurrence); var q = input.Recovery;
        var before = Json(Inventory(q.StudyRoot)); var calls = 0;
        int Candidate(string stage, int ordinal) { calls++; return FixtureHost.AllocationCandidate(stage, ordinal); }
        var recovered = Recover(q, Candidate); Assert.Equal(count, calls); Assert.Equal(count, recovered.Reserved.Length);
        Assert.Equal(TowerPracticalReservationRecovery.AllocationVersion, recovered.Version);
        Assert.Equal("AbandonedPermanentlyReserved", recovered.Status); Assert.Equal(new[] { -987 }, recovered.Historical);
        Assert.Equal(0, recovered.StartedAttempts); Assert.Equal(0, recovered.CompletedAttempts);
        Assert.Equal(input.Source.PriorSeconds, recovered.PriorSeconds); Assert.Equal(input.Source.PriorBytes, recovered.PriorBytes);
        Assert.Equal(input.Source.MaximumSeconds, recovered.ForfeitedMaximumSeconds); Assert.Equal(input.Source.MaximumBytes, recovered.ForfeitedMaximumBytes);
        Assert.Equal(Directory.EnumerateFiles(q.StudyRoot).Sum(p => new FileInfo(p).Length), recovered.RetainedSourceBytes);
        Assert.Equal(Json(recovered), Json(Verify(q)));
        var pending = Path.Combine(q.StudyRoot, "history-input.json"); var expected = recovered.Historical.Concat(recovered.Reserved).Order().ToArray();
        var pins = input.Source.RequiredHistory.ToDictionary(p => p.Key, p => p.Value); pins.Add(pending, HarnessJson.FileHash(pending));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(root, Path.Combine(root, "next"), pins, expected, default));
        var history = TowerRefinementComparisonLaunch.Refresh(root, Path.Combine(root, "next"), pins, expected, default,
            new Dictionary<string, string> { [pending] = q.ReceiptPath }, FixtureHost.AllocationCandidate);
        Assert.Equal(expected, history.Values); Assert.Equal(before, Json(Inventory(q.StudyRoot)));
        Assert.Throws<InvalidDataException>(() => Recover(q));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.AllocateAndRun(input.Source));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Public_commands_and_history_admit_zero_derivations_with_absent_or_empty_journal(bool emptyJournal)
    {
        var input = await Failed("allocation-pending"); var q = input.Recovery;
        if (emptyJournal) File.WriteAllText(Path.Combine(q.StudyRoot, "allocation-journal.jsonl"), "");
        q = Seal(q); var request = Path.Combine(root, "recovery-request.json"); HarnessJson.WriteNew(request, q);
        Assert.Equal(0, await TowerPracticalSearch.Command(["tower-practical-search-recover", request], default));
        Assert.Equal(0, await TowerPracticalSearch.Command(["tower-practical-search-recovery-verify", q.ReceiptPath], default));
        var pending = Path.Combine(q.StudyRoot, "history-input.json");
        Assert.Equal(new[] { -987 }, TowerPracticalReservationRecovery.ReadPending(pending, q.ReceiptPath, default));
        Assert.Throws<InvalidDataException>(() => TowerPracticalReservationRecovery.ReadPending(Path.Combine(root, "history-input.json"), q.ReceiptPath, default));
    }

    [Fact]
    public async Task Historical_and_cross_stage_collisions_remain_excluded_and_every_recorded_candidate_is_audited()
    {
        int Candidate(string stage, int ordinal) => (stage, ordinal) switch {
            ("construction", 0) => -987, ("construction", 1) => 17, ("discovery", 0) => 17,
            _ => FixtureHost.AllocationCandidate(stage, ordinal) };
        var input = await Failed(occurrence: 4, candidate: Candidate); var calls = 0;
        var receipt = Recover(input.Recovery, (s, n) => { calls++; return Candidate(s, n); });
        Assert.Equal(4, calls); Assert.Equal(new[] { 17, 102 }, receipt.Reserved);
        Assert.Equal(new[] { -987 }, receipt.Historical); Verify(input.Recovery, Candidate);
    }

    [Theory]
    [InlineData("unresolved")] [InlineData("torn-result")] [InlineData("torn-start")]
    [InlineData("value")] [InlineData("accepted")] [InlineData("stage")] [InlineData("ordinal")]
    [InlineData("extra-row")] [InlineData("unknown-field")] [InlineData("duplicate-field")]
    [InlineData("missing-intent")] [InlineData("intent")] [InlineData("request-binding")]
    [InlineData("pending-values")] [InlineData("complete")]
    [InlineData("empty-attempts")] [InlineData("study-directory")] [InlineData("unknown-artifact")]
    [InlineData("definition-too-early")] [InlineData("partial-too-early")] [InlineData("oversized-journal")]
    [InlineData("old-recovery-version")]
    public async Task Ambiguous_or_changed_prefixes_stay_blocked_even_when_resealed(string change)
    {
        var input = await Failed(); var q = input.Recovery; string P(string name) => Path.Combine(q.StudyRoot, name);
        var journal = P("allocation-journal.jsonl"); var rows = File.ReadAllLines(journal);
        switch (change)
        {
            case "unresolved": File.WriteAllText(journal, rows[0] + "\n"); break;
            case "torn-result": File.WriteAllText(journal, rows[0] + "\n" + rows[1]); break;
            case "torn-start": File.WriteAllText(journal, rows[0][..20]); break;
            case "value": case "accepted": case "stage": case "ordinal":
                var result = JsonNode.Parse(rows[1])!;
                result[change] = JsonSerializer.SerializeToNode(change switch { "value" => (object)999, "accepted" => false, "stage" => "discovery", _ => 1 });
                File.WriteAllLines(journal, [rows[0], result.ToJsonString()]); break;
            case "extra-row": File.AppendAllText(journal, rows[1] + "\n"); break;
            case "unknown-field": File.WriteAllLines(journal, [rows[0], rows[1].Replace("{", "{\"extra\":0,")]); break;
            case "duplicate-field": File.WriteAllLines(journal, [rows[0], rows[1].Replace("{", "{\"value\":17,")]); break;
            case "missing-intent": File.Delete(P("allocation-intent.json")); break;
            case "intent": Edit(P("allocation-intent.json"), "historicalHash", new string('0', 64)); break;
            case "request-binding": Edit(P("history-input.json"), "allocationRequestHash", new string('0', 64)); break;
            case "pending-values": Edit(P("history-input.json"), "reserved", new[] { 17 }); break;
            case "complete": Edit(P("history-input.json"), "reservationState", "Complete"); break;
            case "empty-attempts": File.WriteAllText(P("attempts.jsonl"), ""); break;
            case "study-directory": Directory.CreateDirectory(P("study")); break;
            case "unknown-artifact": File.WriteAllText(P("unknown.json"), "{}"); break;
            case "definition-too-early": File.WriteAllText(P("definition.json"), "{}"); break;
            case "partial-too-early": File.WriteAllText(P("definition.json.pending"), "{"); break;
            case "oversized-journal": using (var file = File.OpenWrite(journal)) file.SetLength(2L * TowerPracticalSearch.MaximumAllocationCandidates * 256 + 1); break;
            case "old-recovery-version": q = q with { Version = TowerPracticalReservationRecovery.Version }; break;
        }
        q = Seal(q); var before = Json(Inventory(q.StudyRoot)); var calls = 0;
        Assert.ThrowsAny<Exception>(() => Recover(q, (s, n) => { calls++; return FixtureHost.AllocationCandidate(s, n); }));
        if (change is "unresolved" or "torn-result" or "torn-start") Assert.Equal(0, calls);
        Assert.False(File.Exists(q.ReceiptPath)); Assert.False(File.Exists(q.ReceiptPath + ".pending"));
        Assert.Equal(before, Json(Inventory(q.StudyRoot)));
    }

    [Theory]
    [InlineData(2)] [InlineData(10)] [InlineData(42)]
    public async Task An_unresolved_start_after_a_closed_prefix_never_derives_the_missing_result(int occurrence)
    {
        var input = await Failed("allocation-start", occurrence); var q = input.Recovery;
        var before = Json(Inventory(q.StudyRoot)); var calls = 0;
        Assert.Throws<InvalidDataException>(() => Recover(q, (stage, ordinal) => {
            Assert.True(++calls < occurrence, "Recovery derived an unrecorded result.");
            return FixtureHost.AllocationCandidate(stage, ordinal);
        }));
        Assert.Equal(occurrence - 1, calls); Assert.Equal(before, Json(Inventory(q.StudyRoot)));
        Assert.False(File.Exists(q.ReceiptPath));
    }

    [Theory]
    [InlineData("definition.json", false)] [InlineData("definition.json", true)]
    [InlineData("allocation.json", false)] [InlineData("allocation.json", true)]
    [InlineData("seed-ledger.json", false)] [InlineData("seed-ledger.json", true)]
    [InlineData("history-input.json", false)] [InlineData("history-input.json", true)]
    public async Task A_complete_journal_supports_exact_prefixes_of_each_ordered_atomic_write(string name, bool truncated)
    {
        var input = await Failed("before-complete"); var q = input.Recovery; string P(string n) => Path.Combine(q.StudyRoot, n);
        string[] writes = ["definition.json", "allocation.json", "seed-ledger.json", "history-input.json"];
        var bytes = name == "history-input.json" ? JsonSerializer.SerializeToUtf8Bytes(new {
            reservationState = "Complete", reserved = TowerPracticalSearch.Reserved(I.Definition()) }, HarnessJson.Options) : File.ReadAllBytes(P(name));
        foreach (var remove in writes.Skip(Array.IndexOf(writes, name)).Where(n => n != "history-input.json")) File.Delete(P(remove));
        File.WriteAllBytes(P(name + ".pending"), truncated ? bytes[..(bytes.Length / 2)] : bytes);
        q = Seal(q); var before = Json(Inventory(q.StudyRoot));
        Assert.Equal(297, Recover(q).Reserved.Length); Verify(q); Assert.Equal(before, Json(Inventory(q.StudyRoot)));
    }

    [Theory]
    [InlineData("both-writes")] [InlineData("missing-predecessor")] [InlineData("two-partials")]
    [InlineData("wrong-prefix")] [InlineData("allocation-count")] [InlineData("bound-definition")]
    [InlineData("ledger")]
    public async Task Binding_writes_cannot_override_the_journal_or_skip_a_durable_predecessor(string change)
    {
        var input = await Failed("before-complete"); var q = input.Recovery; string P(string n) => Path.Combine(q.StudyRoot, n);
        switch (change)
        {
            case "both-writes": File.WriteAllText(P("definition.json.pending"), ""); break;
            case "missing-predecessor": File.Delete(P("allocation.json")); break;
            case "two-partials": File.Delete(P("allocation.json")); File.Delete(P("seed-ledger.json"));
                File.WriteAllText(P("allocation.json.pending"), ""); File.WriteAllText(P("seed-ledger.json.pending"), ""); break;
            case "wrong-prefix": File.WriteAllText(P("history-input.json.pending"), "not the expected write"); break;
            case "allocation-count": Edit(P("allocation.json"), "candidates", 0); break;
            case "bound-definition": Edit(P("definition.json"), "excludedCombatSeeds", new[] { -999 }); break;
            case "ledger": Edit(P("seed-ledger.json"), "reserved", Array.Empty<int>()); break;
        }
        q = Seal(q); Assert.ThrowsAny<Exception>(() => Recover(q)); Assert.False(File.Exists(q.ReceiptPath));
    }

    [Fact]
    public async Task Public_recovery_and_verification_cannot_authenticate_literal_fixture_candidates()
    {
        var input = await Failed(); var q = input.Recovery;
        Assert.Throws<InvalidDataException>(() => TowerPracticalReservationRecovery.Recover(q));
        Recover(q); Assert.Throws<InvalidDataException>(() => TowerPracticalReservationRecovery.Verify(q.ReceiptPath));
    }

    [Theory] [InlineData("reserved")] [InlineData("cost")] [InlineData("attempts")] [InlineData("version")]
    [InlineData("journal")] [InlineData("manifest")]
    public async Task Receipt_verification_repeats_the_audit_and_preserves_costs(string change)
    {
        var input = await Failed(); var q = input.Recovery; Recover(q);
        switch (change)
        {
            case "reserved": Edit(q.ReceiptPath, "reserved", Array.Empty<int>()); break;
            case "cost": Edit(q.ReceiptPath, "forfeitedMaximumSeconds", 0); break;
            case "attempts": Edit(q.ReceiptPath, "startedAttempts", 1); break;
            case "version": Edit(q.ReceiptPath, "version", TowerPracticalReservationRecovery.Version); break;
            case "journal": File.AppendAllText(Path.Combine(q.StudyRoot, "allocation-journal.jsonl"), "\n"); break;
            case "manifest": File.AppendAllText(q.ManifestPath, " "); break;
        }
        Assert.ThrowsAny<Exception>(() => Verify(q));
    }
}
