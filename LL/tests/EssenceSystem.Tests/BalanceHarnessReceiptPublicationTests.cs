using BalanceHarness;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessReceiptPublicationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-native-publication-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Publication tests cannot fight.")).Activate();
    private string Study => Path.Combine(root, "study");
    private string Receipt => Path.Combine(root, "worker.json");
    private string Observation => Path.Combine(root, "publication.json");
    private string Persistence => Path.Combine(root, "persistence.json");
    private string Binding => Path.Combine(root, "binding.json");
    public BalanceHarnessReceiptPublicationTests()
    {
        Directory.CreateDirectory(Study);
        File.WriteAllText(Path.Combine(Study, "request.json"), "{\"literal\":true}");
    }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private ProposalWorkerBinding Value(string phase = "nativeAudit")
    {
        var producer = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath);
        return new(TowerProposalWorkReceipt.PublicationVersion, phase, Study,
            HarnessJson.FileHash(Path.Combine(Study, "request.json")), producer, producer, Receipt) { PublicationPath = Observation };
    }
    private string Bind(string phase = "nativeAudit", Action<JsonObject>? edit = null)
    {
        var value = JsonSerializer.SerializeToNode(Value(phase), HarnessJson.Options)!.AsObject();
        edit?.Invoke(value);
        File.WriteAllText(Binding, value.ToJsonString());
        return HarnessJson.FileHash(Binding);
    }
    private JsonElement Read(string path) => JsonSerializer.Deserialize<JsonElement>(File.ReadAllBytes(path));
    private void Complete(JsonElement observed)
    {
        Assert.Equal("Complete", observed.GetProperty("outcome").GetString());
        Assert.Equal(HarnessJson.FileHash(Binding), observed.GetProperty("bindingSha256").GetString());
        Assert.Equal(HarnessJson.FileHash(Receipt), observed.GetProperty("serializedReceiptSha256").GetString());
        Assert.Equal(new FileInfo(Receipt).Length, observed.GetProperty("serializedReceiptBytes").GetInt64());
        var counts = observed.GetProperty("counters");
        Assert.Equal(new FileInfo(Receipt).Length, counts.GetProperty("acceptedWriteBytes").GetInt64());
        foreach (var op in new[] { "open", "serialize", "write", "flush", "sync", "close" })
        {
            Assert.Equal(1, counts.GetProperty(op + "Attempted").GetInt64());
            Assert.Equal(1, counts.GetProperty(op + "Completed").GetInt64());
            Assert.False(counts.TryGetProperty(op + "Failed", out _));
        }
        Assert.True(observed.GetProperty("observationPersistenceExcluded").GetBoolean());
        Assert.False(observed.GetProperty("wholeProcessCoverage").GetBoolean());
        Assert.False(observed.GetProperty("usableForAdmission").GetBoolean());
    }
    private void Export(string name, bool realWorkerBoundary, bool injectedFailure = false)
    {
        if (Environment.GetEnvironmentVariable("LL_NATIVE_PUBLICATION_EXPORT") is not { Length: > 0 } export) return;
        var destination = Path.Combine(export, name);
        Assert.False(Directory.Exists(destination)); Directory.CreateDirectory(destination);
        foreach (var (source, member) in new[] { (Binding, "binding.json"), (Receipt, "native-work.json"),
            (Observation, "publication.json"), (Persistence, "persistence.json"), (Path.Combine(Study, "request.json"), "request.json") })
            if (File.Exists(source)) File.Copy(source, Path.Combine(destination, member));
        HarnessJson.WriteNew(Path.Combine(destination, "fixture.json"), new {
            fixtureOnly = true, realWorkerBoundary, injectedFailure, actualCombat = 0, productionEntropyDraws = 0,
            producerPath = TowerProposalWorkReceipt.ProducerPath, producerSha256 = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath),
            wholeProcessCoverage = false, usableForAdmission = false
        });
        HarnessJson.WriteNew(Path.Combine(destination, "files.json"), Directory.GetFiles(destination).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
    }

    [Theory]
    [InlineData("native")]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task Real_v2_boundary_observes_closed_receipt_and_restores_outer_collector(string phase)
    {
        var pin = Bind(phase); var outer = new TowerWorkAccounting();
        using (outer.Activate())
        {
            Assert.Equal(7, await TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, async () => {
                await Task.Yield(); TowerWorkAccounting.Add("literalWork", 3); return 7;
            }));
            TowerWorkAccounting.Add("outerAfterPublication");
        }
        Assert.Single(outer.Snapshot()); Assert.Equal(1, outer.Snapshot()["outerAfterPublication"]);
        Complete(Read(Observation));
        using (File.Open(Receipt, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        using (File.Open(Observation, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        Assert.Equal(3, Read(Receipt).GetProperty("counters").GetProperty("literalWork").GetInt64());
        Assert.False(Read(Receipt).GetProperty("counters").TryGetProperty("acceptedWriteBytes", out _));
        Export("success-" + phase, true);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("relative")]
    [InlineData("inside")]
    [InlineData("overlap")]
    [InlineData("v1-with-path")]
    [InlineData("v1-with-null")]
    [InlineData("unknown")]
    [InlineData("internal-marker")]
    [InlineData("duplicate")]
    public async Task Invalid_publication_binding_fails_before_action_and_sidecars(string fault)
    {
        var pin = Bind(edit: node => {
            switch (fault)
            {
                case "missing": node.Remove("publicationPath"); break;
                case "null": node["publicationPath"] = null; break;
                case "relative": node["publicationPath"] = "relative.json"; break;
                case "inside": node["publicationPath"] = Path.Combine(Study, "bad.json"); break;
                case "overlap": node["publicationPath"] = Path.Combine(root, ".", "worker.json"); break;
                case "v1-with-path": node["version"] = TowerProposalWorkReceipt.Version; break;
                case "v1-with-null": node["version"] = TowerProposalWorkReceipt.Version; node["publicationPath"] = null; break;
                case "unknown": node["extra"] = true; break;
                case "internal-marker": node["publicationPathSpecified"] = true; break;
            }
        });
        if (fault == "duplicate")
        {
            File.WriteAllText(Binding, File.ReadAllText(Binding).Replace("{", "{\"publicationPath\":null,", StringComparison.Ordinal));
            pin = HarnessJson.FileHash(Binding);
        }
        var called = false;
        await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { called = true; return Task.FromResult(0); }));
        Assert.False(called); Assert.False(File.Exists(Receipt)); Assert.False(File.Exists(Observation));
        Assert.False(TowerWorkAccounting.Enabled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Worker_failure_or_cancellation_retains_separately_complete_publication(bool cancel)
    {
        var pin = Bind(); Exception original = cancel ? new OperationCanceledException("literal cancellation") : new InvalidOperationException("literal failure");
        var caught = await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalWorkReceipt.Run<int>(Study, "nativeAudit", Binding, pin,
            async () => { await Task.Yield(); throw original; }));
        Assert.Same(original, caught); Complete(Read(Observation));
        Assert.Equal("Failed", Read(Receipt).GetProperty("outcome").GetString());
        Assert.False(TowerWorkAccounting.Enabled);
        Export(cancel ? "worker-cancelled" : "worker-failed", true);
    }

    [Fact]
    public async Task Existing_observation_is_preserved_before_receipt_or_action()
    {
        var pin = Bind(); File.WriteAllText(Observation, "previous"); var called = false;
        await Assert.ThrowsAsync<IOException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { called = true; return Task.FromResult(0); }));
        Assert.False(called); Assert.False(File.Exists(Receipt)); Assert.Equal("previous", File.ReadAllText(Observation));
    }

    [Fact]
    public async Task Existing_receipt_retains_failed_reservation_observation_without_action()
    {
        var pin = Bind(); File.WriteAllText(Receipt, "previous"); var called = false;
        await Assert.ThrowsAsync<IOException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { called = true; return Task.FromResult(0); }));
        Assert.False(called); Assert.Equal("previous", File.ReadAllText(Receipt));
        var value = Read(Observation); Assert.Equal("Failed", value.GetProperty("outcome").GetString());
        Assert.Equal(JsonValueKind.Null, value.GetProperty("serializedReceiptBytes").ValueKind);
        Assert.Equal(1, value.GetProperty("counters").GetProperty("openFailed").GetInt64());
        Export("open-failed", true);
    }

    [Fact]
    public async Task Changed_request_after_action_cannot_become_successful_worker()
    {
        var pin = Bind();
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { File.AppendAllText(Path.Combine(Study, "request.json"), " "); return Task.FromResult(0); }));
        Complete(Read(Observation)); Assert.Equal("Failed", Read(Receipt).GetProperty("outcome").GetString());
    }

    private sealed class FailingStream(string? fail) : MemoryStream
    {
        internal bool Closed { get; private set; }
        internal List<string> Calls { get; } = [];
        public override void Write(ReadOnlySpan<byte> buffer)
        { var raw = buffer.ToArray(); Write(raw, 0, raw.Length); }
        public override void Write(byte[] buffer, int offset, int count)
        {
            Calls.Add("write");
            if (fail is "write" or "write-close") { base.Write(buffer, offset, 1); throw new IOException("literal write failure"); }
            base.Write(buffer, offset, count);
        }
        public override void Flush()
        {
            Calls.Add("flush"); if (fail == "flush") throw new IOException("literal flush failure");
            base.Flush();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !Closed)
            {
                Calls.Add("close"); Closed = true; base.Dispose(disposing);
                if (fail is "close" or "write-close") throw new IOException("literal close failure");
            }
        }
    }

    [Theory]
    [InlineData("serialize")]
    [InlineData("write")]
    [InlineData("flush")]
    [InlineData("sync")]
    [InlineData("close")]
    [InlineData("write-close")]
    public void Publication_failures_record_actual_progress_and_preserve_original(string fault)
    {
        var pin = Bind(); var publication = new TowerReceiptPublication(pin, Value());
        var stream = new FailingStream(fault); publication.Open(() => stream);
        var error = Assert.ThrowsAny<Exception>(() => publication.Publish(() => {
            if (fault == "serialize") throw new InvalidOperationException("literal serialize failure");
            return JsonSerializer.SerializeToUtf8Bytes(new TowerWorkAccounting().Receipt("nativeAudit", Value().RequestSha256, Value().ProducerSha256, true), HarnessJson.Options);
        }, _ => { stream.Calls.Add("sync"); if (fault == "sync") throw new IOException("literal sync failure"); }));
        Assert.Contains(fault == "write-close" ? "write" : fault, error.Message);
        Assert.True(stream.Closed);
        if (fault == "write-close") Assert.Contains("close failure", (string)error.Data["WorkReceiptCloseError"]!);
        File.WriteAllBytes(Receipt, stream.ToArray());
        publication.Persist(new FileStream(Observation, FileMode.CreateNew, FileAccess.Write), output => ((FileStream)output).Flush(true), null);
        var value = Read(Observation); var counts = value.GetProperty("counters");
        Assert.Equal("Failed", value.GetProperty("outcome").GetString());
        Assert.Equal(1, counts.GetProperty("closeAttempted").GetInt64());
        if (fault.StartsWith("write", StringComparison.Ordinal))
        {
            Assert.Equal(1, counts.GetProperty("failedWriteBytesUnknown").GetInt64());
            Assert.False(counts.TryGetProperty("acceptedWriteBytes", out _));
            Assert.Single(stream.ToArray());
        }
        else if (fault != "serialize") Assert.Equal(stream.ToArray().LongLength, counts.GetProperty("acceptedWriteBytes").GetInt64());
        Export(fault + "-failed", false, true);
    }

    [Theory]
    [InlineData("write", false)]
    [InlineData("flush", false)]
    [InlineData("sync", false)]
    [InlineData("close", false)]
    [InlineData("write-close", false)]
    [InlineData("write", true)]
    [InlineData("flush", true)]
    [InlineData("sync", true)]
    [InlineData("close", true)]
    [InlineData("write-close", true)]
    public void Observation_persistence_errors_never_hide_worker_error_or_return_success(string fault, bool failedWorker)
    {
        var publication = new TowerReceiptPublication(Bind(), Value());
        publication.Open(() => new MemoryStream()); publication.Publish(() => Encoding.UTF8.GetBytes("{}"), _ => { });
        var output = new FailingStream(fault); var original = failedWorker ? new InvalidOperationException("worker failed") : null;
        void Persist() => publication.Persist(output, _ => { if (fault == "sync") throw new IOException("literal sync failure"); }, original);
        if (original is not null)
        {
            Persist(); Assert.Contains("failure", (string)original.Data["PublicationObservationPersistenceError"]!);
        }
        else
        {
            var error = Assert.ThrowsAny<Exception>(Persist);
            Assert.Contains(fault == "write-close" ? "write" : fault, error.Message);
            if (fault == "write-close") Assert.Contains("close", (string)error.Data["PublicationObservationCloseError"]!);
        }
        Assert.True(output.Closed);
    }

    [Theory]
    [InlineData("tower-proposal-study-run", "native")]
    [InlineData("tower-proposal-study-audit", "nativeAudit")]
    [InlineData("tower-proposal-study-publication-check", "publication")]
    public async Task Real_native_command_rejects_literal_input_and_still_publishes_bound_failure(string command, string phase)
    {
        var pin = Bind(phase);
        await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalStudy.Command([command, Study, "--work-binding", Binding, pin]));
        Complete(Read(Observation)); Assert.Equal("Failed", Read(Receipt).GetProperty("outcome").GetString());
        Assert.Single(Directory.GetFiles(Study)); Export("command-failed-" + phase, true);
    }

    private string BindPersistence(string phase = "nativeAudit", Action<JsonObject>? edit = null) => Bind(phase, node => {
        node["version"] = TowerProposalWorkReceipt.PersistenceVersion;
        node["publicationPersistencePath"] = Persistence;
        edit?.Invoke(node);
    });

    private void CompletePersistence()
    {
        var value = Read(Persistence);
        Assert.Equal(TowerReceiptPublication.PersistenceVersion, value.GetProperty("version").GetString());
        Assert.Equal("Complete", value.GetProperty("outcome").GetString());
        Assert.Equal(HarnessJson.FileHash(Binding), value.GetProperty("bindingSha256").GetString());
        Assert.Equal(HarnessJson.FileHash(Observation), value.GetProperty("serializedObservationSha256").GetString());
        Assert.Equal(new FileInfo(Observation).Length, value.GetProperty("serializedObservationBytes").GetInt64());
        Assert.Equal(new FileInfo(Observation).Length, value.GetProperty("counters").GetProperty("acceptedWriteBytes").GetInt64());
        foreach (var op in new[] { "open", "serialize", "write", "flush", "sync", "close" })
            Assert.Equal(1, value.GetProperty("counters").GetProperty(op + "Completed").GetInt64());
        Assert.True(value.GetProperty("terminalObservationPersistenceExcluded").GetBoolean());
        Assert.False(value.GetProperty("wholeProcessCoverage").GetBoolean());
        Assert.False(value.GetProperty("usableForAdmission").GetBoolean());
        using (File.Open(Persistence, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        using (File.Open(Observation, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
    }

    [Theory]
    [InlineData("native")]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task V3_observes_publication_file_through_close_without_merging_counters(string phase)
    {
        var pin = BindPersistence(phase); var outer = new TowerWorkAccounting();
        using (outer.Activate())
        {
            Assert.Equal(9, await TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, async () => {
                await Task.Yield(); TowerWorkAccounting.Add("literalWork"); return 9;
            }));
            TowerWorkAccounting.Add("outerAfterPersistence");
        }
        Assert.Single(outer.Snapshot()); Assert.Equal(1, outer.Snapshot()["outerAfterPersistence"]);
        Complete(Read(Observation)); CompletePersistence();
        Assert.True(Read(Observation).GetProperty("observationPersistenceExcluded").GetBoolean());
        Assert.False(Read(Receipt).GetProperty("counters").TryGetProperty("acceptedWriteBytes", out _));
        Export("v3-success-" + phase, true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task V3_worker_error_or_cancellation_survives_complete_persistence(bool cancelled)
    {
        var pin = BindPersistence(); Exception original = cancelled ? new OperationCanceledException("literal cancellation") : new IOException("literal worker failure");
        var caught = await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalWorkReceipt.Run<int>(Study, "nativeAudit", Binding, pin,
            async () => { await Task.Yield(); throw original; }));
        Assert.Same(original, caught); CompletePersistence();
        Assert.Equal("Failed", Read(Receipt).GetProperty("outcome").GetString());
        Assert.False(TowerWorkAccounting.Enabled);
        Export(cancelled ? "v3-worker-cancelled" : "v3-worker-failed", true);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("relative")]
    [InlineData("inside")]
    [InlineData("receipt-overlap")]
    [InlineData("publication-overlap")]
    [InlineData("missing-publication")]
    [InlineData("v2-with-path")]
    [InlineData("v2-with-null")]
    [InlineData("v1-with-path")]
    [InlineData("internal-marker")]
    [InlineData("duplicate")]
    public async Task V3_invalid_binding_rejects_before_all_sidecars_and_action(string fault)
    {
        var pin = BindPersistence(edit: node => {
            switch (fault)
            {
                case "missing": node.Remove("publicationPersistencePath"); break;
                case "null": node["publicationPersistencePath"] = null; break;
                case "relative": node["publicationPersistencePath"] = "relative.json"; break;
                case "inside": node["publicationPersistencePath"] = Path.Combine(Study, "bad.json"); break;
                case "receipt-overlap": node["publicationPersistencePath"] = Path.Combine(root, ".", "worker.json"); break;
                case "publication-overlap": node["publicationPersistencePath"] = Observation; break;
                case "missing-publication": node.Remove("publicationPath"); break;
                case "v2-with-path": node["version"] = TowerProposalWorkReceipt.PublicationVersion; break;
                case "v2-with-null": node["version"] = TowerProposalWorkReceipt.PublicationVersion; node["publicationPersistencePath"] = null; break;
                case "v1-with-path": node["version"] = TowerProposalWorkReceipt.Version; node.Remove("publicationPath"); break;
                case "internal-marker": node["publicationPersistencePathSpecified"] = true; break;
            }
        });
        if (fault == "duplicate")
        {
            File.WriteAllText(Binding, File.ReadAllText(Binding).Replace("{", "{\"publicationPersistencePath\":null,", StringComparison.Ordinal));
            pin = HarnessJson.FileHash(Binding);
        }
        var called = false;
        await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { called = true; return Task.FromResult(0); }));
        Assert.False(called); Assert.False(File.Exists(Receipt)); Assert.False(File.Exists(Observation)); Assert.False(File.Exists(Persistence));
        Assert.False(TowerWorkAccounting.Enabled);
    }

    [Theory]
    [InlineData("terminal")]
    [InlineData("observation")]
    [InlineData("receipt")]
    public async Task V3_reserves_outer_files_before_worker_and_preserves_existing_evidence(string target)
    {
        var pin = BindPersistence(); var path = target == "terminal" ? Persistence : target == "observation" ? Observation : Receipt;
        File.WriteAllText(path, "previous"); var called = false;
        await Assert.ThrowsAsync<IOException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { called = true; return Task.FromResult(0); }));
        Assert.False(called); Assert.Equal("previous", File.ReadAllText(path));
        if (target == "terminal") { Assert.False(File.Exists(Observation)); Assert.False(File.Exists(Receipt)); }
        else if (target == "observation")
        {
            Assert.False(File.Exists(Receipt));
            Assert.Equal("Failed", Read(Persistence).GetProperty("outcome").GetString());
            Assert.Equal(1, Read(Persistence).GetProperty("counters").GetProperty("openFailed").GetInt64());
            Assert.Equal(JsonValueKind.Null, Read(Persistence).GetProperty("serializedObservationBytes").ValueKind);
        }
        else CompletePersistence();
        if (target != "terminal") Export("v3-existing-" + target, true);
    }

    [Theory]
    [InlineData("serialize", false)]
    [InlineData("write", false)]
    [InlineData("flush", false)]
    [InlineData("sync", false)]
    [InlineData("close", false)]
    [InlineData("write-close", false)]
    [InlineData("write", true)]
    [InlineData("sync", true)]
    public void V3_persistence_failures_keep_known_progress_and_primary_error(string fault, bool failedWorker)
    {
        var pin = BindPersistence(); var publication = new TowerReceiptPublication(pin, Value());
        publication.Open(() => new MemoryStream()); publication.Publish(() => Encoding.UTF8.GetBytes("{}"), _ => { });
        var persistence = new TowerReceiptPublication(pin, Value(), true);
        var stream = new FailingStream(fault); persistence.Open(() => stream);
        var original = failedWorker ? new IOException("worker failed") : null;
        void Persist()
        {
            if (fault == "serialize") persistence.Publish(() => throw new InvalidOperationException("literal serialize failure"), _ => { });
            else publication.Persist(stream, _ => { if (fault == "sync") throw new IOException("literal sync failure"); }, original, persistence);
        }
        if (failedWorker)
        {
            Persist(); Assert.Contains("failure", (string)original!.Data["PublicationObservationPersistenceError"]!);
        }
        else
        {
            var error = Assert.ThrowsAny<Exception>(Persist);
            Assert.Contains(fault == "write-close" ? "write" : fault, error.Message);
            if (fault == "write-close") Assert.Contains("close", (string)error.Data["WorkReceiptCloseError"]!);
        }
        Assert.True(stream.Closed); File.WriteAllBytes(Observation, stream.ToArray());
        persistence.Persist(new FileStream(Persistence, FileMode.CreateNew, FileAccess.Write), output => ((FileStream)output).Flush(true), null);
        var value = Read(Persistence); Assert.Equal("Failed", value.GetProperty("outcome").GetString());
        Assert.Equal(1, value.GetProperty("counters").GetProperty("closeAttempted").GetInt64());
        if (fault.StartsWith("write", StringComparison.Ordinal))
        {
            Assert.Equal(1, value.GetProperty("counters").GetProperty("failedWriteBytesUnknown").GetInt64());
            Assert.False(value.GetProperty("counters").TryGetProperty("acceptedWriteBytes", out _));
        }
        Export("v3-" + fault + (failedWorker ? "-worker" : "") + "-failed", false, true);
    }

    [Theory]
    [InlineData("tower-proposal-study-run", "native")]
    [InlineData("tower-proposal-study-audit", "nativeAudit")]
    [InlineData("tower-proposal-study-publication-check", "publication")]
    public async Task V3_real_native_command_rejects_literal_input_and_retains_persistence(string command, string phase)
    {
        var pin = BindPersistence(phase);
        await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalStudy.Command([command, Study, "--work-binding", Binding, pin]));
        CompletePersistence(); Assert.Equal("Failed", Read(Receipt).GetProperty("outcome").GetString());
        Assert.Single(Directory.GetFiles(Study)); Export("v3-command-failed-" + phase, true);
    }

    [Fact]
    public void V3_terminal_failure_does_not_replace_prior_publication_error_details()
    {
        var pin = BindPersistence(); var persistence = new TowerReceiptPublication(pin, Value(), true);
        Assert.Throws<IOException>(() => persistence.Open(() => throw new IOException("observation open failed")));
        var original = new InvalidOperationException("worker failed");
        original.Data["PublicationObservationPersistenceError"] = "prior publication failure";
        var output = new FailingStream("write-close");
        persistence.Persist(output, _ => { }, original);
        Assert.True(output.Closed);
        Assert.Equal("prior publication failure", original.Data["PublicationObservationPersistenceError"]);
        Assert.Contains("write failure", (string)original.Data["TerminalPublicationPersistenceError"]!);
        Assert.Equal("worker failed", original.Message);
    }
}
