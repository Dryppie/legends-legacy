using System.IO.Compression;
using System.Text;
using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessFileAccountingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-file-work-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("File accounting cannot fight.")).Activate();
    private string P(string name) => Path.Combine(root, name);
    public BalanceHarnessFileAccountingTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static long Count(TowerWorkAccounting work, string name) => work.Snapshot().GetValueOrDefault(name);

    [Theory]
    [InlineData(false, null)] [InlineData(true, null)]
    [InlineData(false, "")] [InlineData(true, "")]
    [InlineData(false, "dragon: 🐉æ\n")] [InlineData(true, "dragon: 🐉æ\n")]
    [InlineData(false, "\ud800")] [InlineData(true, "\ud800")]
    public void Text_APIs_preserve_native_encoding_BOM_append_and_failure_behavior(bool append, string? text)
    {
        var initial = new byte[] { 239, 187, 191, 65, 10 };
        File.WriteAllBytes(P("off.jsonl"), initial); File.WriteAllBytes(P("on.jsonl"), initial);
        var baseline = Record.Exception(() => { if (append) File.AppendAllText(P("off.jsonl"), text); else File.WriteAllText(P("off.jsonl"), text); });
        var work = new TowerWorkAccounting(); Exception? failure;
        using (work.Activate()) failure = Record.Exception(() => { if (append) TowerWorkAccounting.AppendAllText(P("on.jsonl"), text); else TowerWorkAccounting.WriteAllText(P("on.jsonl"), text); });
        Assert.Equal(baseline?.GetType(), failure?.GetType());
        Assert.Equal(File.ReadAllBytes(P("off.jsonl")), File.ReadAllBytes(P("on.jsonl")));
        var length = new FileInfo(P("on.jsonl")).Length;
        Assert.Equal(length, Count(work, "trackedRetainedBytes"));
        Assert.Equal(Math.Max(initial.Length, length), Count(work, "peakTrackedCombinedBytes"));
        if (failure is null)
        {
            Assert.Equal(length - (append ? initial.Length : 0), Count(work, "applicationWriteBytes.journal"));
            Assert.Equal(1, Count(work, "textWriteOperationsCompleted"));
        }
        else
        {
            Assert.Equal(0, Count(work, "applicationWriteBytes.journal"));
            Assert.Equal(1, Count(work, "failedTextWriteBytesUnknown"));
        }
    }

    [Fact]
    public void Locked_text_target_retains_original_bytes_and_does_not_claim_write_completion()
    {
        File.WriteAllText(P("locked.jsonl"), "original"); var work = new TowerWorkAccounting();
        using (var locked = new FileStream(P("locked.jsonl"), FileMode.Open, FileAccess.Read, FileShare.None))
        using (work.Activate())
        {
            var baseline = Record.Exception(() => File.AppendAllText(P("locked.jsonl"), "extra"));
            var failure = Record.Exception(() => TowerWorkAccounting.AppendAllText(P("locked.jsonl"), "extra"));
            Assert.NotNull(baseline); Assert.NotNull(failure); Assert.Equal(baseline.GetType(), failure.GetType());
        }
        Assert.Equal("original", File.ReadAllText(P("locked.jsonl")));
        Assert.Equal(0, Count(work, "applicationWriteBytes.journal"));
        Assert.Equal(1, Count(work, "failedTextWriteBytesUnknown"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Native_copy_preserves_metadata_and_overwrite_rules_without_inventing_stream_IO(bool overwrite)
    {
        File.WriteAllBytes(P("source.bin"), Enumerable.Range(0, 200).Select(n => (byte)n).ToArray());
        File.SetLastWriteTimeUtc(P("source.bin"), new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        if (overwrite) { File.WriteAllBytes(P("off.bin"), new byte[300]); File.WriteAllBytes(P("on.bin"), new byte[300]); }
        File.Copy(P("source.bin"), P("off.bin"), overwrite);
        var work = new TowerWorkAccounting(); using (work.Activate()) TowerWorkAccounting.CopyFile(P("source.bin"), P("on.bin"), overwrite);
        Assert.Equal(File.ReadAllBytes(P("off.bin")), File.ReadAllBytes(P("on.bin")));
        Assert.Equal(File.GetLastWriteTimeUtc(P("off.bin")), File.GetLastWriteTimeUtc(P("on.bin")));
        Assert.Equal(File.GetAttributes(P("off.bin")), File.GetAttributes(P("on.bin")));
        Assert.Equal(200, Count(work, "fileCopyLogicalBytes.other"));
        Assert.Equal(200, Count(work, "trackedRetainedBytes"));
        Assert.Equal(overwrite ? 300 : 200, Count(work, "peakTrackedCombinedBytes"));
        Assert.Equal(0, Count(work, "applicationReadBytes.other"));
        Assert.Equal(0, Count(work, "applicationWriteBytes.other"));
    }

    [Theory]
    [InlineData("exists")] [InlineData("source")] [InlineData("parent")] [InlineData("same")]
    public void Failed_native_copy_matches_original_exception_and_marks_progress_unknown(string fault)
    {
        File.WriteAllBytes(P("source.bin"), new byte[10]);
        var source = fault == "source" ? P("missing.bin") : P("source.bin");
        var target = fault == "parent" ? P("missing/target.bin") : fault == "same" ? source : P("target.bin");
        if (fault == "exists") File.WriteAllBytes(target, new byte[3]);
        var baseline = Record.Exception(() => File.Copy(source, target, false));
        var work = new TowerWorkAccounting(); Exception? failure;
        using (work.Activate()) failure = Record.Exception(() => TowerWorkAccounting.CopyFile(source, target));
        Assert.NotNull(baseline); Assert.NotNull(failure); Assert.Equal(baseline.GetType(), failure.GetType());
        Assert.Equal(1, Count(work, "fileCopyOperationsFailed"));
        Assert.Equal(1, Count(work, "failedFileCopyBytesUnknown"));
        Assert.Equal(0, Count(work, "fileCopyLogicalBytes.other"));
        Assert.Equal(File.Exists(target) ? new FileInfo(target).Length : 0, Count(work, "trackedRetainedBytes"));
    }

    [Theory]
    [InlineData(90000, 90000)] [InlineData(65536, 65536)] [InlineData(0, 0)]
    public void Bounded_copy_counts_all_reads_including_the_chunk_rejected_by_the_cap(long cap, long written)
    {
        File.WriteAllBytes(P("source.bin"), new byte[90000]); var work = new TowerWorkAccounting();
        using (work.Activate())
        {
            if (cap == 90000) Assert.Equal(90000, TowerBossStudy.CopyBounded(P("source.bin"), P("target.bin"), cap, default));
            else Assert.Throws<InvalidDataException>(() => TowerBossStudy.CopyBounded(P("source.bin"), P("target.bin"), cap, default));
        }
        Assert.Equal(written, new FileInfo(P("target.bin")).Length);
        Assert.Equal(written, Count(work, "applicationWriteBytes.other"));
        Assert.Equal(cap == 0 ? 65536 : 90000, Count(work, "applicationReadBytes.other"));
        Assert.Equal(written, Count(work, "trackedRetainedBytes"));
        Assert.Equal(0, Count(work, "fileCopyOperationsCompleted"));
    }

    [Fact]
    public void Cancelled_bounded_copy_keeps_existing_open_then_cancel_order_and_releases_handles()
    {
        File.WriteAllBytes(P("source.bin"), new byte[20]); using var stop = new CancellationTokenSource(); stop.Cancel();
        var work = new TowerWorkAccounting(); using (work.Activate())
            Assert.Throws<OperationCanceledException>(() => TowerBossStudy.CopyBounded(P("source.bin"), P("target.bin"), 20, stop.Token));
        Assert.True(File.Exists(P("target.bin"))); Assert.Equal(0, new FileInfo(P("target.bin")).Length);
        Assert.Equal(0, Count(work, "applicationReadBytes.other"));
        using var exclusive = new FileStream(P("target.bin"), FileMode.Open, FileAccess.Write, FileShare.None);
    }

    [Fact]
    public void Content_copy_counts_logical_copy_and_existing_hash_read_separately()
    {
        var work = new TowerWorkAccounting(); IReadOnlyDictionary<string, string> hashes;
        using (work.Activate()) hashes = TowerBundle.CopyContent(TestContentPaths.FindApiRoot(), P("content"), default);
        var paths = hashes.Keys.Select(name => P("content/Data/" + name)).ToArray();
        var bytes = paths.Sum(p => new FileInfo(p).Length);
        Assert.Equal(TowerBundle.Files.Count, Count(work, "fileCopyOperationsCompleted"));
        Assert.Equal(bytes, Count(work, "fileCopyLogicalBytes.json"));
        Assert.Equal(bytes, Count(work, "applicationReadBytes.json"));
        Assert.Equal(bytes, Count(work, "trackedRetainedBytes"));
        Assert.Equal(0, Count(work, "applicationWriteBytes.json"));
        foreach (var name in hashes.Keys) Assert.Equal(HarnessJson.FileHash(Path.Combine(TestContentPaths.FindApiRoot(), "Data", name)), hashes[name]);
    }

    [Fact]
    public void Later_missing_content_preserves_prior_copy_and_hash_work()
    {
        var first = TowerBundle.Files[0]; var path = P("source/Data/" + first);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, "{}");
        var work = new TowerWorkAccounting(); using (work.Activate())
            Assert.ThrowsAny<IOException>(() => TowerBundle.CopyContent(P("source"), P("content"), default));
        Assert.Equal(1, Count(work, "fileCopyOperationsCompleted"));
        Assert.Equal(1, Count(work, "fileCopyOperationsFailed"));
        Assert.Equal(2, Count(work, "fileCopyLogicalBytes.json"));
        Assert.Equal(2, Count(work, "applicationReadBytes.json"));
        Assert.Equal(2, Count(work, "trackedRetainedBytes"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Retained_runtime_inventory_and_copy_work_match_each_original_copy_mode(bool bounded)
    {
        var execution = ExecutionIdentity.Current(); var work = new TowerWorkAccounting(); IReadOnlyDictionary<string, string> files;
        using (work.Activate()) files = TowerBossStudy.RetainExecutable(P("capture"), execution, bounded ? 512L * 1048576 : long.MaxValue);
        var bytes = files.Keys.Sum(name => new FileInfo(P("capture/executable/" + name)).Length);
        Assert.Equal(bytes, Count(work, "trackedRetainedBytes"));
        var snapshot = work.Snapshot();
        long Sum(string prefix) => snapshot.Where(p => p.Key.StartsWith(prefix, StringComparison.Ordinal)).Sum(p => p.Value);
        Assert.Equal(bounded ? bytes : 0, Sum("applicationWriteBytes."));
        Assert.Equal(bounded ? 0 : bytes, Sum("fileCopyLogicalBytes."));
        var deps = Path.Combine(Path.GetDirectoryName(typeof(TowerBossStudy).Assembly.Location)!, "BalanceHarness.deps.json");
        Assert.Equal((bounded ? 2 : 1) * bytes + new FileInfo(deps).Length, Sum("applicationReadBytes."));
        Assert.All(execution.AssemblyHashes, pair => Assert.Equal(pair.Value, files[pair.Key + ".dll"]));
    }

    [Fact]
    public void Root_storage_descriptor_counts_durable_metadata_write_and_preserves_budget()
    {
        var selection = BalanceHarnessEvidenceStorageTests.Selection(); var work = new TowerWorkAccounting();
        using (work.Activate()) _ = new TowerProposalEvidenceStorage.Writer(root, selection, () => { });
        var bytes = new FileInfo(P("evidence-storage.json")).Length;
        Assert.Equal(bytes, Count(work, "applicationWriteBytes.metadata"));
        Assert.Equal(bytes, Count(work, "trackedRetainedBytes"));
        Assert.Equal(1, Count(work, "flushOperationsCompleted"));
        var rejected = P("rejected"); Directory.CreateDirectory(rejected);
        using (work.Activate()) Assert.Throws<InvalidDataException>(() => new TowerProposalEvidenceStorage.Writer(rejected, selection with { MaximumPhysicalBytes = 1 }, () => { }));
        Assert.Empty(Directory.GetFiles(rejected));
        Assert.Equal(bytes, Count(work, "applicationWriteBytes.metadata"));
    }

    [Fact]
    public void Proposal_export_counts_all_members_and_preserves_exclusive_inventory()
    {
        var plan = BalanceHarnessAdaptiveRacingTests.Plan(17, 5);
        var context = new TowerProposalContext(plan.Scope, plan.Mechanics, plan.BenchmarkReferenceId, plan.RootSeed);
        var request = new TowerProposalExportRequest(TowerProposalPolicies.ExportVersion, context,
            [TowerProposalPolicies.Legacy(), TowerProposalPolicies.BenchmarkSmallEdits()]);
        var work = new TowerWorkAccounting(); string manifest;
        using (work.Activate()) manifest = TowerProposalPolicies.WriteExport(request, P("export"));
        Assert.Equal(new FileInfo(P("export/files.json")).Length, Count(work, "applicationWriteBytes.manifest"));
        Assert.Equal(new FileInfo(P("export/request.json")).Length + new FileInfo(P("export/batches.json")).Length, Count(work, "applicationWriteBytes.json"));
        Assert.Equal(Directory.GetFiles(P("export")).Sum(p => new FileInfo(p).Length), Count(work, "trackedRetainedBytes"));
        _ = TowerProposalPolicies.VerifyExport(P("export"), manifest);
        Assert.Throws<IOException>(() => TowerProposalPolicies.WriteExport(request, P("export")));
    }

    [Fact]
    public async Task Compact_chunk_commit_preserves_gzip_bytes_and_moves_scratch_without_double_writes()
    {
        var rows = new[] { new TowerCompactRecord(0, "literal", 1, new('a', 64), null!, true, 0, 1, new('b', 64)) };
        var off = P("off"); var on = P("on"); var work = new TowerWorkAccounting();
        await TowerCompactBundle.CommitChunkAsync(off, 0, rows, default);
        using (work.Activate()) await TowerCompactBundle.CommitChunkAsync(on, 0, rows, default);
        var gzip = Path.Combine(on, "chunks/000000/records.json.gz"); var receipt = Path.Combine(on, "chunks/000000/receipt.json");
        Assert.Equal(File.ReadAllBytes(Path.Combine(off, "chunks/000000/records.json.gz")), File.ReadAllBytes(gzip));
        Assert.Equal(File.ReadAllBytes(Path.Combine(off, "chunks/000000/receipt.json")), File.ReadAllBytes(receipt));
        var total = new FileInfo(gzip).Length + new FileInfo(receipt).Length;
        Assert.Equal(new FileInfo(gzip).Length, Count(work, "applicationWriteBytes.payload"));
        Assert.Equal(new FileInfo(receipt).Length, Count(work, "applicationWriteBytes.json"));
        Assert.Equal(total, Count(work, "trackedRetainedBytes")); Assert.Equal(total, Count(work, "peakTrackedScratchBytes"));
        Assert.Equal(total, Count(work, "peakTrackedCombinedBytes")); Assert.Equal(0, Count(work, "trackedScratchBytes"));
        Assert.Equal(1, Count(work, "trackedDirectoryPublications"));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Compact_publication_retries_preserve_scratch_and_do_not_repeat_writes(bool succeeds)
    {
        var pending = P("chunks/.pending-000000"); Directory.CreateDirectory(pending);
        TowerCompactBundle.WriteGzip(Path.Combine(pending, "records.json.gz"), new[] { new { literal = true } }, scratch: true);
        var receipt = new TowerCompactChunk(1, 0, 0, 1, HarnessJson.FileHash(Path.Combine(pending, "records.json.gz")));
        HarnessJson.WriteNew(Path.Combine(pending, "receipt.json"), receipt);
        var total = Directory.GetFiles(pending).Sum(p => new FileInfo(p).Length); var calls = 0; var work = new TowerWorkAccounting();
        void Move(string source, string target) { if (++calls < 3 || !succeeds) throw new IOException("fixture rename denied"); Directory.Move(source, target); }
        using (work.Activate())
        {
            if (succeeds) await TowerCompactBundle.PublishChunkAsync(root, 0, receipt, default, Move);
            else await Assert.ThrowsAsync<IOException>(() => TowerCompactBundle.PublishChunkAsync(root, 0, receipt, default, Move));
        }
        Assert.Equal(succeeds ? 3 : 4, calls);
        Assert.Equal(succeeds ? 2 : 4, Count(work, "directoryPublicationFailures"));
        Assert.Equal(succeeds ? total : 0, Count(work, "trackedRetainedBytes"));
        Assert.Equal(succeeds ? 0 : total, Count(work, "trackedScratchBytes"));
        Assert.Equal(total, Count(work, "peakTrackedCombinedBytes"));
        Assert.Equal(0, Count(work, "applicationWriteBytes.payload"));
    }

    [Fact]
    public void Compact_attempt_is_flushed_before_return_and_rejects_exhausted_cap()
    {
        var definition = new TowerCompactDefinition(1, "literal", 1, 1, []); var work = new TowerWorkAccounting();
        using (work.Activate())
        {
            TowerCompactBundle.AppendAttempt(root, definition, 0, 0, "literal", 1);
            Assert.Throws<InvalidDataException>(() => TowerCompactBundle.AppendAttempt(root, definition, 1, 1, "literal", 2));
        }
        Assert.Equal(new FileInfo(P("bulk-attempts.jsonl")).Length, Count(work, "applicationWriteBytes.journal"));
        Assert.Equal(1, Count(work, "flushOperationsCompleted")); Assert.Single(File.ReadAllLines(P("bulk-attempts.jsonl")));
    }

    [Fact]
    public void File_copy_and_text_fixture_exports_distinct_measures()
    {
        var content = Encoding.UTF8.GetBytes("{\"dragon\":\"🐉æ\"}\n"); File.WriteAllBytes(P("source.json"), content);
        var work = new TowerWorkAccounting(); using (work.Activate())
        {
            TowerWorkAccounting.CopyFile(P("source.json"), P("target.json"));
            TowerWorkAccounting.AppendAllText(P("trials.jsonl"), "{\"literal\":true}\n");
            TowerBossStudy.CopyBounded(P("source.json"), P("bounded.json"), content.Length, default);
        }
        if (Environment.GetEnvironmentVariable("LL_FILE_ACCOUNTING_EXPORT") is { Length: > 0 } export)
        {
            Assert.False(Directory.Exists(export)); Directory.CreateDirectory(export);
            foreach (var name in new[] { "source.json", "target.json", "bounded.json", "trials.jsonl" }) File.Copy(P(name), Path.Combine(export, name));
            HarnessJson.WriteNew(Path.Combine(export, "native-work.json"), work.Receipt("native", new('a', 64), new('b', 64), true));
            HarnessJson.WriteNew(Path.Combine(export, "files.json"), Directory.GetFiles(export).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
        }
    }
}
