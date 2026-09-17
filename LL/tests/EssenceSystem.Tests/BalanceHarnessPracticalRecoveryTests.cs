using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPracticalRecoveryTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-practical-recovery-fixture-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Recovery fixture entered combat.")).Activate();
    private static readonly Lazy<(int Id, long Ticks)[]> Exited = new(() => Enumerable.Range(0, 2).Select(_ => {
        using var process = Process.Start(new ProcessStartInfo("dotnet", "--version") {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true })!;
        var identity = (process.Id, process.StartTime.ToUniversalTime().Ticks);
        Assert.True(process.WaitForExit(10000)); Assert.Equal(0, process.ExitCode); return identity;
    }).ToArray());
    public BalanceHarnessPracticalRecoveryTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);
    private static void Edit(string path, string property, object value)
    {
        var node = JsonNode.Parse(File.ReadAllText(path))!; node[property] = JsonSerializer.SerializeToNode(value, HarnessJson.Options);
        File.WriteAllText(path, node.ToJsonString(HarnessJson.Options));
    }
    private static Dictionary<string, string> Inventory(string path) => Directory.EnumerateFiles(path)
        .ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash);
    private TowerPracticalRecoveryRequest Seal(TowerPracticalRecoveryRequest q)
    {
        File.WriteAllText(q.ManifestPath, Json(Inventory(q.StudyRoot)));
        return q with { ManifestHash = HarnessJson.FileHash(q.ManifestPath) };
    }
    private async Task<(TowerPracticalRequest Source, TowerPracticalRecoveryRequest Recovery, TowerBossDiscoveryDefinition Definition)> Failed(string boundary = "pending")
    {
        var owners = Exited.Value;
        var prior = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(prior, new { historical = new[] { -987 } });
        var d = I.Definition() with { ExcludedCombatSeeds = [-987] };
        var sourcePath = Path.Combine(root, "input.json"); HarnessJson.WriteNew(sourcePath, d);
        var content = Path.Combine(root, "content"); Directory.CreateDirectory(content);
        var files = new Dictionary<string, string> { [prior] = HarnessJson.FileHash(prior) };
        var source = new TowerPracticalRequest(TowerPracticalSearch.Version, content, sourcePath, HarnessJson.FileHash(sourcePath),
            root, Path.Combine(root, "failed"), files, 300, 32 * 1048576, 3, 128);
        Directory.CreateDirectory(source.OutputRoot);
        var now = DateTimeOffset.UtcNow;
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(source), now, now.AddSeconds(source.MaximumSeconds - source.PriorSeconds), owners[0].Id, owners[0].Ticks);
        HarnessJson.WriteNew(Path.Combine(source.OutputRoot, "request.json"), source);
        HarnessJson.WriteNew(Path.Combine(source.OutputRoot, "launch.json"), launch);
        using var stop = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerPracticalSearch.RunOperation(source, launch,
            _ => new(d, new(files, [-987])), (_, _, _, _) => throw new InvalidOperationException("Recovery fixture attempted study execution."),
            (_, _) => throw new InvalidOperationException("Recovery fixture attempted native verification."), stop.Token,
            stage => { if (stage == boundary) stop.Cancel(); }));
        // Saved fixture owners have really exited; the in-process operation above
        // intentionally substitutes for a child worker and uses no combat engine.
        var workerPath = Path.Combine(source.OutputRoot, "worker-start.json");
        var worker = HarnessJson.Read<TowerPracticalWorkerStart>(workerPath);
        File.WriteAllText(workerPath, Json(worker with { ProcessId = owners[1].Id, ProcessStartedUtcTicks = owners[1].Ticks }));
        HarnessJson.WriteNew(Path.Combine(source.OutputRoot, "failure.json"), TowerPracticalSearch.Unverified("Cancelled", "Failed", "Fixture cancellation"));
        HarnessJson.WriteNew(Path.Combine(source.OutputRoot, "worker-failure.json"), new { status = "Cancelled", error = "Fixture cancellation", retries = 0 });
        var recovery = new TowerPracticalRecoveryRequest(TowerPracticalReservationRecovery.Version, source.OutputRoot,
            Path.Combine(root, "manifest.json"), new string('0', 64), Path.Combine(root, "receipt.json"), 30);
        return (source, Seal(recovery), d);
    }

    [Theory] [InlineData("pending")] [InlineData("before-complete")]
    public async Task Public_recovery_and_audit_preserve_source_and_all_exclusions(string boundary)
    {
        var (source, q, d) = await Failed(boundary); var before = Json(Inventory(q.StudyRoot));
        var requestPath = Path.Combine(root, "recovery-request.json"); HarnessJson.WriteNew(requestPath, q);
        Assert.Equal(0, await TowerPracticalSearch.Command(["tower-practical-search-recover", requestPath], default));
        Assert.Equal(0, await TowerPracticalSearch.Command(["tower-practical-search-recovery-verify", q.ReceiptPath], default));
        var receipt = TowerPracticalReservationRecovery.Verify(q.ReceiptPath);
        Assert.Equal(TowerPracticalSearch.Reserved(d), receipt.Reserved); Assert.Equal(d.ExcludedCombatSeeds, receipt.Historical);
        Assert.Equal(0, receipt.StartedAttempts); Assert.Equal(0, receipt.CompletedAttempts);
        Assert.Equal(source.MaximumSeconds, receipt.ForfeitedMaximumSeconds); Assert.Equal(source.MaximumBytes, receipt.ForfeitedMaximumBytes);
        Assert.Equal(source.PriorSeconds, receipt.PriorSeconds); Assert.Equal(source.PriorBytes, receipt.PriorBytes);
        Assert.Equal(Directory.EnumerateFiles(q.StudyRoot).Sum(p => new FileInfo(p).Length), receipt.RetainedSourceBytes);
        Assert.Equal(before, Json(Inventory(q.StudyRoot)));
        Assert.Equal("Pending", HarnessJson.Read<JsonElement>(Path.Combine(q.StudyRoot, "history-input.json")).GetProperty("reservationState").GetString());
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerPracticalSearch.Run(source));
        Assert.Throws<InvalidDataException>(() => TowerPracticalReservationRecovery.Recover(q));
    }

    [Theory] [InlineData("pending")] [InlineData("before-complete")]
    public async Task Complete_history_requires_an_explicit_receipt_and_retains_the_original_union(string boundary)
    {
        var (source, q, d) = await Failed(boundary); TowerPracticalReservationRecovery.Recover(q);
        var pending = Path.Combine(q.StudyRoot, "history-input.json"); var next = Path.Combine(root, "next");
        var expected = d.ExcludedCombatSeeds.Concat(TowerPracticalSearch.Reserved(d)).Order().ToArray();
        var pins = source.RequiredHistory.ToDictionary(p => p.Key, p => p.Value); pins.Add(pending, HarnessJson.FileHash(pending));
        Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(root, next, pins, expected, default));
        var recovered = new Dictionary<string, string> { [pending] = q.ReceiptPath };
        var history = TowerRefinementComparisonLaunch.Refresh(root, next, pins, expected, default, recovered);
        Assert.Equal(expected, history.Values);
        TowerPracticalSearch.ValidateRequest(source with { OutputRoot = next, RequiredHistory = pins,
            PendingHistoryRecoveries = recovered, RecoveryReceiptHashes = new Dictionary<string, string> { [q.ReceiptPath] = HarnessJson.FileHash(q.ReceiptPath) } });
        Assert.Throws<InvalidDataException>(() => TowerPracticalReservationRecovery.ReadPending(Path.Combine(root, "history-input.json"), q.ReceiptPath, default));
        Assert.False(Directory.Exists(next));
    }

    [Theory]
    [InlineData("reserved")] [InlineData("complete")] [InlineData("source")] [InlineData("definition")]
    [InlineData("history")] [InlineData("deadline")] [InlineData("request")] [InlineData("legacy-worker")]
    [InlineData("worker-alive")] [InlineData("parent-alive")] [InlineData("worker-unknown")] [InlineData("different-host")]
    [InlineData("attempt")] [InlineData("empty-attempts")] [InlineData("completion")]
    [InlineData("missing-source")] [InlineData("failure")] [InlineData("worker-retry")]
    [InlineData("seed-ledger")] [InlineData("partial-invalid")] [InlineData("partial-out-of-order")]
    public async Task Resealing_corrupt_or_ambiguous_evidence_does_not_make_it_recoverable(string change)
    {
        var (_, q, _) = await Failed(); string P(string name) => Path.Combine(q.StudyRoot, name);
        using var live = Process.GetCurrentProcess();
        switch (change)
        {
            case "reserved": Edit(P("history-input.json"), "reserved", Array.Empty<int>()); break;
            case "complete": Edit(P("history-input.json"), "reservationState", "Complete"); break;
            case "source": File.AppendAllText(P("source-definition.json"), " "); break;
            case "definition": File.WriteAllText(P("definition.json"), "{}"); break;
            case "history": File.WriteAllText(P("history-files.json"), "{}"); break;
            case "deadline": Edit(P("launch.json"), "deadline", DateTimeOffset.UtcNow.AddHours(1)); break;
            case "request": Edit(P("launch.json"), "requestHash", new string('0', 64)); break;
            case "legacy-worker": File.WriteAllText(P("worker-start.json"), "{}"); break;
            case "worker-alive": Edit(P("worker-start.json"), "processId", live.Id); Edit(P("worker-start.json"), "processStartedUtcTicks", live.StartTime.ToUniversalTime().Ticks); break;
            case "parent-alive": Edit(P("launch.json"), "parentProcessId", live.Id); Edit(P("launch.json"), "parentStartedUtcTicks", live.StartTime.ToUniversalTime().Ticks); break;
            case "worker-unknown": Edit(P("worker-start.json"), "processId", 0); break;
            case "different-host": Edit(P("worker-start.json"), "machineName", Environment.MachineName + "-other"); break;
            case "attempt": File.WriteAllText(P("attempts.jsonl"), "{\"kind\":\"Started\",\"ordinal\":1}\n"); break;
            case "empty-attempts": File.WriteAllText(P("attempts.jsonl"), ""); break;
            case "completion": File.WriteAllText(P("completion.json"), "{}"); break;
            case "missing-source": File.Delete(P("source-definition.json")); break;
            case "failure": Edit(P("failure.json"), "executionStatus", "Complete"); break;
            case "worker-retry": Edit(P("worker-failure.json"), "retries", 1); break;
            case "seed-ledger": File.WriteAllText(P("seed-ledger.json"), "{\"historical\":[-999]}"); break;
            case "partial-invalid": File.WriteAllText(P("seed-ledger.json.pending"), "not a registration write"); break;
            case "partial-out-of-order": File.WriteAllText(P("history-input.json.pending"), "{"); break;
        }
        q = Seal(q); var before = Json(Inventory(q.StudyRoot));
        Assert.ThrowsAny<Exception>(() => TowerPracticalReservationRecovery.Recover(q));
        Assert.False(File.Exists(q.ReceiptPath)); Assert.False(File.Exists(q.ReceiptPath + ".pending"));
        Assert.Equal(before, Json(Inventory(q.StudyRoot)));
    }

    [Theory] [InlineData("seed-ledger", false)] [InlineData("seed-ledger", true)]
    [InlineData("history-input", false)] [InlineData("history-input", true)]
    public async Task Recognized_interrupted_atomic_writes_remain_reserved(string file, bool truncated)
    {
        var (_, q, d) = await Failed(file == "seed-ledger" ? "pending" : "before-complete");
        object expected = file == "seed-ledger"
            ? new { reservationState = "Complete", historical = d.ExcludedCombatSeeds.Order().ToArray(), reserved = TowerPracticalSearch.Reserved(d) }
            : new { reservationState = "Complete", reserved = TowerPracticalSearch.Reserved(d) };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(expected, HarnessJson.Options);
        File.WriteAllBytes(Path.Combine(q.StudyRoot, file + ".json.pending"), truncated ? bytes[..(bytes.Length / 2)] : bytes);
        q = Seal(q); var before = Json(Inventory(q.StudyRoot));
        TowerPracticalReservationRecovery.Recover(q); TowerPracticalReservationRecovery.Verify(q.ReceiptPath);
        Assert.Equal(before, Json(Inventory(q.StudyRoot)));
    }

    [Theory] [InlineData("source")] [InlineData("manifest")] [InlineData("receipt")]
    [InlineData("cost")] [InlineData("attempt-count")] [InlineData("version")]
    public async Task Published_receipt_is_reaudited_not_trusted_by_hash_alone(string change)
    {
        var (_, q, _) = await Failed(); TowerPracticalReservationRecovery.Recover(q);
        switch (change)
        {
            case "source": File.AppendAllText(Path.Combine(q.StudyRoot, "definition.json"), " "); break;
            case "manifest": File.AppendAllText(q.ManifestPath, " "); break;
            case "receipt": Edit(q.ReceiptPath, "reserved", Array.Empty<int>()); break;
            case "cost": Edit(q.ReceiptPath, "forfeitedMaximumSeconds", 0); break;
            case "attempt-count": Edit(q.ReceiptPath, "startedAttempts", 1); break;
            case "version": Edit(q.ReceiptPath, "version", "unknown-recovery"); break;
        }
        Assert.ThrowsAny<Exception>(() => TowerPracticalReservationRecovery.Verify(q.ReceiptPath));
    }

    [Theory] [InlineData("registry")] [InlineData("source")]
    public async Task An_active_writer_blocks_recovery_without_changing_evidence(string owner)
    {
        var (_, q, _) = await Failed(); var before = Json(Inventory(q.StudyRoot));
        using var lease = TowerCompactBundle.AcquireWriter(owner == "registry" ? Path.Combine(root, "complete-family-allocation") : q.StudyRoot);
        Assert.Throws<IOException>(() => TowerPracticalReservationRecovery.Recover(q));
        Assert.False(File.Exists(q.ReceiptPath)); Assert.Equal(before, Json(Inventory(q.StudyRoot)));
    }

    [Fact]
    public async Task Missing_failure_markers_after_owner_death_do_not_require_rewriting_the_source()
    {
        var (_, q, _) = await Failed();
        File.Delete(Path.Combine(q.StudyRoot, "failure.json")); File.Delete(Path.Combine(q.StudyRoot, "worker-failure.json"));
        q = Seal(q); var before = Json(Inventory(q.StudyRoot));
        TowerPracticalReservationRecovery.Recover(q); Assert.Equal(before, Json(Inventory(q.StudyRoot)));
    }

    [Fact]
    public async Task Interrupted_receipt_publication_is_not_accepted_or_overwritten()
    {
        var (_, q, _) = await Failed(); File.WriteAllText(q.ReceiptPath + ".pending", "{");
        Assert.Throws<InvalidDataException>(() => TowerPracticalReservationRecovery.Recover(q));
        Assert.Equal("{", File.ReadAllText(q.ReceiptPath + ".pending")); Assert.False(File.Exists(q.ReceiptPath));
    }

    [Theory] [InlineData("recover")] [InlineData("recovery-verify")]
    public async Task Cancelled_public_commands_do_not_read_inputs_or_create_files(string command)
    {
        using var stop = new CancellationTokenSource(); stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerPracticalSearch.Command(
            ["tower-practical-search-" + command, Path.Combine(root, "absent.json")], stop.Token));
        Assert.Empty(Directory.EnumerateFileSystemEntries(root));
    }
}
