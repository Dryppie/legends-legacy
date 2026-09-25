using System.IO.Compression;
using System.Text;
using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessWriteAccountingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-write-counters-" + Guid.NewGuid().ToString("N"));
    private string P(string name) => Path.Combine(root, name);
    public BalanceHarnessWriteAccountingTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);
    private static long Count(TowerWorkAccounting ledger, string name) => ledger.Snapshot().GetValueOrDefault(name);

    [Fact]
    public void Disabled_accounting_preserves_stream_identity()
    {
        using var stream = new MemoryStream();
        Assert.Same(stream, TowerWorkAccounting.WriteStream(stream, P("x.json")));
    }

    [Fact]
    public async Task Stream_captures_owner_and_counts_sync_async_overwrites_without_inflating_storage()
    {
        var first = new TowerWorkAccounting(); var second = new TowerWorkAccounting();
        using var memory = new MemoryStream(); Stream writer;
        using (first.Activate()) writer = TowerWorkAccounting.WriteStream(memory, P("x.json"), leaveOpen: true);
        using (second.Activate())
        {
            writer.Write(new byte[] { 1, 2, 3 }, 0, 3);
            writer.Position = 0; writer.WriteByte(4);
            await writer.WriteAsync(new byte[] { 5, 6 }, 0, 2);
            await writer.WriteAsync(new byte[] { 7 }.AsMemory());
            await writer.FlushAsync();
            writer.SetLength(2);
            writer.Dispose(); writer.Dispose();
        }
        Assert.Equal(7, Count(first, "applicationWriteBytes.json"));
        Assert.Equal(2, Count(first, "trackedRetainedBytes"));
        Assert.Equal(4, Count(first, "peakTrackedCombinedBytes"));
        Assert.Empty(second.Snapshot()); Assert.True(memory.CanWrite);
    }

    private sealed class FaultStream(bool overwrite = false, bool unknownLength = false) : MemoryStream
    {
        internal readonly IOException Failure = new("injected write failure");
        public override long Length => unknownLength ? throw new NotSupportedException() : base.Length;
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (unknownLength) throw Failure;
            if (overwrite) { base.SetLength(5); Position = 0; }
            base.Write(buffer[..2]); throw Failure;
        }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        { try { Write(buffer.Span); return ValueTask.CompletedTask; } catch (Exception e) { return ValueTask.FromException(e); } }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Partial_write_failure_never_claims_requested_or_inferred_bytes(bool overwrite)
    {
        var ledger = new TowerWorkAccounting(); using var inner = new FaultStream(overwrite);
        using (ledger.Activate())
        using (var stream = TowerWorkAccounting.WriteStream(inner, P("x.json"), leaveOpen: true))
            Assert.Same(inner.Failure, Assert.Throws<IOException>(() => stream.Write(new byte[8])));
        Assert.Equal(0, Count(ledger, "applicationWriteBytes.json"));
        Assert.Equal(8, Count(ledger, "failedWriteRequestedBytes.json"));
        Assert.Equal(1, Count(ledger, "failedWriteBytesUnknown"));
        Assert.Equal(overwrite ? 5 : 2, Count(ledger, "trackedRetainedBytes"));
    }

    [Fact]
    public async Task Async_failure_retains_partial_length_and_original_exception()
    {
        var ledger = new TowerWorkAccounting(); using var inner = new FaultStream();
        using (ledger.Activate())
        using (var stream = TowerWorkAccounting.WriteStream(inner, P("x.json"), leaveOpen: true))
            Assert.Same(inner.Failure, await Assert.ThrowsAsync<IOException>(() => stream.WriteAsync(new byte[8].AsMemory()).AsTask()));
        Assert.Equal(2, Count(ledger, "trackedRetainedBytes"));
        Assert.Equal(1, Count(ledger, "writeOperationsFailed"));
    }

    [Fact]
    public void Length_observation_failure_does_not_mask_write_failure_or_claim_zero_coverage()
    {
        var ledger = new TowerWorkAccounting(); using var inner = new FaultStream(unknownLength: true);
        using (ledger.Activate())
        using (var stream = TowerWorkAccounting.WriteStream(inner, P("x.json"), leaveOpen: true))
            Assert.Same(inner.Failure, Assert.Throws<IOException>(() => stream.Write(new byte[8])));
        Assert.True(Count(ledger, "storageLengthObservationFailures") > 0);
        Assert.False(ledger.Snapshot().ContainsKey("trackedRetainedBytes"));
        var receipt = JsonSerializer.SerializeToElement(ledger.Receipt("native", new('a', 64), new('b', 64), false));
        Assert.False(receipt.GetProperty("wholeProcessCoverage").GetBoolean());
        Assert.False(receipt.GetProperty("usableForAdmission").GetBoolean());
    }

    private sealed class TerminalFaultStream(bool close) : MemoryStream
    {
        internal readonly IOException Failure = new("injected terminal failure");
        public override void Flush() { if (!close) throw Failure; base.Flush(); }
        protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing && close) throw Failure; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Flush_and_close_failures_keep_written_bytes_and_exception(bool close)
    {
        var ledger = new TowerWorkAccounting(); var inner = new TerminalFaultStream(close);
        using var active = ledger.Activate(); var stream = TowerWorkAccounting.WriteStream(inner, P("x.json"));
        stream.Write(new byte[5]);
        Assert.Same(inner.Failure, Assert.Throws<IOException>(() => { if (close) stream.Dispose(); else stream.Flush(); }));
        Assert.Equal(5, Count(ledger, "applicationWriteBytes.json"));
        Assert.Equal(5, Count(ledger, "trackedRetainedBytes"));
        Assert.Equal(1, Count(ledger, close ? "writeStreamCloseFailures" : "flushOperationsFailed"));
        stream.Dispose();
    }

    [Fact]
    public void Atomic_replacement_includes_old_target_and_pending_then_reclassifies_without_a_second_write()
    {
        File.WriteAllBytes(P("result.json"), new byte[5]);
        var storage = new TowerCompleteReservation.Storage(root, 100000); var ledger = new TowerWorkAccounting();
        using (ledger.Activate()) storage.PutBytes("result.json", new byte[9], replace: true);
        Assert.Equal(9, Count(ledger, "applicationWriteBytes.other"));
        Assert.Equal(14, Count(ledger, "peakTrackedCombinedBytes"));
        Assert.Equal(9, Count(ledger, "peakTrackedScratchBytes"));
        Assert.Equal(9, Count(ledger, "trackedRetainedBytes"));
        Assert.Equal(0, Count(ledger, "trackedScratchBytes"));
        Assert.Equal(5, Count(ledger, "trackedReplacedBytes"));
        Assert.Equal(1, Count(ledger, "flushOperationsCompleted"));
        Assert.False(File.Exists(P("result.json.pending")));
    }

    [Fact]
    public void Failed_atomic_rename_retains_flushed_scratch_and_original_target()
    {
        Directory.CreateDirectory(P("result.json"));
        var storage = new TowerCompleteReservation.Storage(root, 100000); var ledger = new TowerWorkAccounting();
        File.WriteAllBytes(P("baseline.pending"), new byte[9]);
        var baseline = Record.Exception(() => File.Move(P("baseline.pending"), P("result.json"), true));
        using (ledger.Activate())
        {
            var failure = Record.Exception(() => storage.PutBytes("result.json", new byte[9], replace: true));
            Assert.NotNull(baseline); Assert.NotNull(failure); Assert.Equal(baseline.GetType(), failure.GetType());
        }
        Assert.Equal(9, new FileInfo(P("result.json.pending")).Length);
        Assert.Equal(9, Count(ledger, "trackedScratchBytes"));
        Assert.Equal(1, Count(ledger, "fileMoveFailures"));
        Assert.Equal(0, Count(ledger, "trackedFileMoves"));
        Assert.True(Directory.Exists(P("result.json")));
    }

    [Fact]
    public void Existing_pending_and_storage_caps_still_fail_before_writes()
    {
        var ledger = new TowerWorkAccounting(); using var active = ledger.Activate();
        File.WriteAllBytes(P("result.json.pending"), new byte[3]);
        var storage = new TowerCompleteReservation.Storage(root, 100000);
        Assert.Throws<IOException>(() => storage.PutBytes("result.json", new byte[9]));
        Assert.Throws<InvalidDataException>(() => new TowerCompleteReservation.Storage(root, 32768).PutBytes("x.json", new byte[9]));
        Assert.Equal(0, Count(ledger, "writeOperationsAttempted"));
        Assert.Equal(3, new FileInfo(P("result.json.pending")).Length);
    }

    [Fact]
    public void Scratch_deletion_keeps_peaks_and_successful_write_totals()
    {
        var ledger = new TowerWorkAccounting(); using var active = ledger.Activate();
        using (var stream = TowerWorkAccounting.WriteStream(File.Create(P("x.pending")), P("x.pending"), scratch: true)) stream.Write(new byte[7]);
        TowerWorkAccounting.DeleteFile(P("x.pending"));
        Assert.Equal(7, Count(ledger, "applicationWriteBytes.other"));
        Assert.Equal(7, Count(ledger, "peakTrackedCombinedBytes"));
        Assert.Equal(7, Count(ledger, "trackedDeletedBytes"));
        Assert.Equal(0, Count(ledger, "trackedCombinedBytes"));
    }

    [Fact]
    public void Failed_delete_and_untracked_operations_remain_explicit()
    {
        var ledger = new TowerWorkAccounting(); using var active = ledger.Activate();
        Directory.CreateDirectory(P("directory"));
        var baseline = Record.Exception(() => File.Delete(P("directory")));
        var failure = Record.Exception(() => TowerWorkAccounting.DeleteFile(P("directory")));
        Assert.NotNull(baseline); Assert.NotNull(failure); Assert.Equal(baseline.GetType(), failure.GetType());
        File.WriteAllBytes(P("external"), new byte[6]);
        TowerWorkAccounting.MoveFile(P("external"), P("moved"), false);
        TowerWorkAccounting.DeleteFile(P("moved"));
        Assert.Equal(1, Count(ledger, "fileDeleteFailures"));
        Assert.Equal(1, Count(ledger, "untrackedFileMoves"));
        Assert.Equal(1, Count(ledger, "untrackedFileDeletes"));
        Assert.False(ledger.Snapshot().ContainsKey("trackedCombinedBytes"));
    }

    [Fact]
    public void Durable_attempt_journal_keeps_share_mode_and_completed_line_bytes()
    {
        var ledger = new TowerWorkAccounting(); using var active = ledger.Activate();
        using (var attempts = new TowerPracticalSearch.Attempts(P("attempts.jsonl"), 1, () => { }))
        {
            attempts.Event(false);
            using (var reader = new FileStream(P("attempts.jsonl"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) Assert.True(reader.Length > 0);
            attempts.Event(true);
            Assert.Throws<InvalidDataException>(() => attempts.Event(false));
        }
        var bytes = File.ReadAllBytes(P("attempts.jsonl"));
        Assert.Equal("{\"kind\":\"Started\",\"ordinal\":1}\n{\"kind\":\"Completed\",\"ordinal\":1}\n", Encoding.UTF8.GetString(bytes));
        Assert.Equal(bytes.Length, Count(ledger, "applicationWriteBytes.journal"));
        Assert.Equal(2, Count(ledger, "flushOperationsCompleted"));
    }

    [Fact]
    public void Allocation_append_counts_existing_journal_and_keeps_caller_handle_open()
    {
        File.WriteAllText(P("allocation-journal.jsonl"), "previous\n");
        var storage = new TowerCompleteReservation.Storage(root, 100000); var ledger = new TowerWorkAccounting();
        using var stream = new FileStream(P("allocation-journal.jsonl"), FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        using (ledger.Activate()) storage.Append(stream, new("Start", "literal", 0));
        Assert.True(stream.CanWrite);
        Assert.Equal(stream.Length - 9, Count(ledger, "applicationWriteBytes.journal"));
        Assert.Equal(stream.Length, Count(ledger, "trackedRetainedBytes"));
    }

    [Fact]
    public void Adaptive_evidence_counts_appends_once_and_keeps_cap_and_no_overwrite_rules()
    {
        var path = P("evidence"); var ledger = new TowerWorkAccounting(); using var active = ledger.Activate();
        using (var evidence = new TowerAdaptiveRacingNative.Evidence(path, 1000, () => { }))
        {
            evidence.Put("files.json", new { a = 1 });
            evidence.Append("events.jsonl", new { stage = 1 });
            evidence.Append("events.jsonl", new { stage = 2 });
            Assert.Throws<IOException>(() => evidence.Put("files.json", new { a = 2 }));
            Assert.Throws<InvalidDataException>(() => evidence.Put("large.json", new string('x', 1000)));
        }
        Assert.Equal(new FileInfo(Path.Combine(path, "files.json")).Length, Count(ledger, "applicationWriteBytes.manifest"));
        Assert.Equal(new FileInfo(Path.Combine(path, "events.jsonl")).Length, Count(ledger, "applicationWriteBytes.journal"));
        Assert.Equal(Directory.GetFiles(path).Sum(p => new FileInfo(p).Length), Count(ledger, "trackedRetainedBytes"));
        Assert.Equal(3, Count(ledger, "flushOperationsCompleted"));
        Assert.False(File.Exists(Path.Combine(path, "large.json")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_json_and_gzip_bytes_match_inactive_writers_including_trailers(bool compressed)
    {
        var off = P("off"); var on = P("on");
        Directory.CreateDirectory(Path.Combine(off, "battles")); Directory.CreateDirectory(Path.Combine(on, "battles"));
        var report = new TowerBattleReport(null!, true, 0, 7); var ledger = new TowerWorkAccounting();
        TowerLoadoutArchive.WriteBattle(off, "trial-000001", report, compressed ? "gzip-json-v1" : null);
        using (ledger.Activate()) TowerLoadoutArchive.WriteBattle(on, "trial-000001", report, compressed ? "gzip-json-v1" : null);
        var a = Directory.GetFiles(Path.Combine(off, "battles")).Single(); var b = Directory.GetFiles(Path.Combine(on, "battles")).Single();
        Assert.Equal(File.ReadAllBytes(a), File.ReadAllBytes(b));
        Assert.Equal(new FileInfo(b).Length, Count(ledger, "applicationWriteBytes." + (compressed ? "payload" : "json")));
        Assert.Equal(new FileInfo(b).Length, Count(ledger, "trackedRetainedBytes"));
        Assert.Equal(report, TowerLoadoutArchive.ReadBattle(on, "trial-000001", compressed ? "gzip-json-v1" : null));
    }

    [Fact]
    public void Codec_limits_and_charges_remain_separate_from_completed_physical_writes()
    {
        var ledger = new TowerWorkAccounting(); var limits = new ProposalEvidenceLimits(100000, 100000); long charged = 0;
        ProposalEvidenceEntry entry;
        using (ledger.Activate()) entry = TowerProposalEvidenceCodec.WriteNew(root, "search.json", new { text = "🐉æ" },
            TowerProposalEvidenceCodec.Version, limits, n => charged += n);
        Assert.Equal(entry.PhysicalBytes, charged);
        Assert.Equal(entry.PhysicalBytes, Count(ledger, "applicationWriteBytes.payload"));
        Assert.Equal(entry.PhysicalBytes, Count(ledger, "trackedRetainedBytes"));
        Assert.Equal("🐉æ", TowerProposalEvidenceCodec.Read<JsonElement>(root, entry, TowerProposalEvidenceCodec.Version, limits).Value.GetProperty("text").GetString());
    }

    [Fact]
    public void Failed_codec_charge_does_not_count_dispatched_bytes()
    {
        var ledger = new TowerWorkAccounting(); var failure = new IOException("charge denied");
        using (ledger.Activate()) Assert.Same(failure, Assert.Throws<IOException>(() =>
            TowerProposalEvidenceCodec.WriteNew(root, "search.json", new { text = "literal" },
                TowerProposalEvidenceCodec.Version, new(100000, 100000), _ => throw failure)));
        Assert.Equal(0, Count(ledger, "applicationWriteBytes.payload"));
        Assert.Equal(0, new FileInfo(P("search.json.gz")).Length);
    }

    private sealed class BrokenJson
    {
        public string Prefix => new('a', 100000);
        public string Failure => throw new InvalidOperationException("serialization failure");
    }

    [Fact]
    public void Serialization_failure_retains_created_file_and_completed_writes()
    {
        var ledger = new TowerWorkAccounting(); using var active = ledger.Activate();
        Assert.Throws<InvalidOperationException>(() => HarnessJson.WriteNew(P("result.json"), new BrokenJson()));
        Assert.True(File.Exists(P("result.json")));
        Assert.Equal(new FileInfo(P("result.json")).Length, Count(ledger, "applicationWriteBytes.json"));
        Assert.Equal(new FileInfo(P("result.json")).Length, Count(ledger, "trackedRetainedBytes"));
        Assert.Throws<IOException>(() => HarnessJson.WriteNew(P("result.json"), new { x = 1 }));
    }

    [Fact]
    public void Atomic_fixture_exports_bound_receipt_with_independently_checkable_lifetimes()
    {
        byte[] old = Encoding.UTF8.GetBytes("{\"v\":1}"); byte[] next = Encoding.UTF8.GetBytes("{\"v\":123456789}");
        File.WriteAllBytes(P("result.json"), old);
        var storage = new TowerCompleteReservation.Storage(root, 100000); var ledger = new TowerWorkAccounting();
        using (ledger.Activate())
        {
            storage.PutBytes("result.json", next, replace: true);
            using (var temporary = TowerWorkAccounting.WriteStream(File.Create(P("scratch.tmp")), P("scratch.tmp"), scratch: true)) temporary.Write(new byte[20]);
            TowerWorkAccounting.DeleteFile(P("scratch.tmp"));
        }
        Assert.Equal(next.Length + 20, Count(ledger, "peakTrackedCombinedBytes"));
        if (Environment.GetEnvironmentVariable("LL_WRITE_ACCOUNTING_EXPORT") is { Length: > 0 } export)
        {
            Assert.False(Directory.Exists(export)); Directory.CreateDirectory(export);
            File.Copy(P("result.json"), Path.Combine(export, "result.json"));
            HarnessJson.WriteNew(Path.Combine(export, "expected.json"), new { oldText = Encoding.UTF8.GetString(old), newText = Encoding.UTF8.GetString(next), deletedScratchBytes = 20 });
            HarnessJson.WriteNew(Path.Combine(export, "native-work.json"), ledger.Receipt("native", new('a', 64), new('b', 64), true));
            HarnessJson.WriteNew(Path.Combine(export, "files.json"), Directory.GetFiles(export).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
        }
    }
}
