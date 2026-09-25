using BalanceHarness;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessWorkerReceiptTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-worker-receipt-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Receipt tests cannot fight.")).Activate();
    private string Study => Path.Combine(root, "study");
    private string Receipt => Path.Combine(root, "worker.json");
    private string Binding => Path.Combine(root, "binding.json");
    public BalanceHarnessWorkerReceiptTests()
    {
        Directory.CreateDirectory(Study);
        File.WriteAllText(Path.Combine(Study, "request.json"), "{\"literal\":true}");
    }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private string Bind(string phase = "nativeAudit", Action<JsonObject>? edit = null)
    {
        var producer = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath);
        var node = JsonSerializer.SerializeToNode(new ProposalWorkerBinding(TowerProposalWorkReceipt.Version, phase, Study,
            HarnessJson.FileHash(Path.Combine(Study, "request.json")), producer, producer, Receipt), HarnessJson.Options)!.AsObject();
        edit?.Invoke(node);
        File.WriteAllText(Binding, node.ToJsonString());
        return HarnessJson.FileHash(Binding);
    }
    private JsonElement ReadReceipt() => HarnessJson.Read<JsonElement>(Receipt);

    [Theory]
    [InlineData("native")]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task Success_binds_actual_request_and_assembly_and_restores_enclosing_collector(string phase)
    {
        var pin = Bind(phase);
        var outer = new TowerWorkAccounting();
        using (outer.Activate())
        {
            Assert.Equal(7, await TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, async () => {
                await Task.Yield();
                Assert.True(HarnessJson.Read<JsonElement>(Path.Combine(Study, "request.json")).GetProperty("literal").GetBoolean());
                return 7;
            }));
            TowerWorkAccounting.Add("outerRestored");
        }
        Assert.Equal(1, outer.Snapshot()["outerRestored"]);
        Assert.Single(outer.Snapshot());
        var receipt = ReadReceipt();
        Assert.Equal(phase, receipt.GetProperty("phase").GetString());
        Assert.Equal("Complete", receipt.GetProperty("outcome").GetString());
        Assert.Equal(HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath), receipt.GetProperty("producerSha256").GetString());
        Assert.Equal(HarnessJson.FileHash(Path.Combine(Study, "request.json")), receipt.GetProperty("requestSha256").GetString());
        var counts = receipt.GetProperty("counters");
        Assert.Equal(new FileInfo(Binding).Length + 3 * 16, counts.GetProperty("applicationReadBytes.json").GetInt64());
        Assert.Equal(2 * new FileInfo(TowerProposalWorkReceipt.ProducerPath).Length, counts.GetProperty("applicationReadBytes.other").GetInt64());
        Assert.Equal(new FileInfo(Binding).Length + 16, counts.GetProperty("jsonInputBytes").GetInt64());
        Assert.Equal(2, counts.GetProperty("workerAuthenticationsCompleted").GetInt64());
        Assert.False(receipt.GetProperty("counters").TryGetProperty("applicationWriteBytes.json", out _));
        Assert.False(receipt.GetProperty("wholeProcessCoverage").GetBoolean());
        Assert.False(receipt.GetProperty("usableForAdmission").GetBoolean());
    }

    [Theory]
    [InlineData("version")]
    [InlineData("phase")]
    [InlineData("root")]
    [InlineData("request")]
    [InlineData("producer")]
    [InlineData("module")]
    [InlineData("unknown")]
    [InlineData("relative")]
    [InlineData("inside")]
    [InlineData("pin")]
    [InlineData("duplicate")]
    public async Task Invalid_bindings_fail_before_action_or_receipt(string fault)
    {
        var pin = Bind(edit: node => {
            switch (fault)
            {
                case "version": node["version"] = "unknown"; break;
                case "phase": node["phase"] = "publication"; break;
                case "root": node["studyRoot"] = root; break;
                case "request": node["requestSha256"] = new string('a', 64); break;
                case "producer":
                    node["producerSha256"] = new string('a', 64);
                    node["accountingModuleSha256"] = new string('a', 64); break;
                case "module": node["accountingModuleSha256"] = new string('a', 64); break;
                case "unknown": node["extra"] = true; break;
                case "relative": node["receiptPath"] = "relative.json"; break;
                case "inside": node["receiptPath"] = Path.Combine(Study, "worker.json"); break;
            }
        });
        if (fault == "pin") pin = new('a', 64);
        if (fault == "duplicate")
        {
            File.WriteAllText(Binding, File.ReadAllText(Binding).Replace("{", "{\"phase\":\"nativeAudit\",", StringComparison.Ordinal));
            pin = HarnessJson.FileHash(Binding);
        }
        var invoked = false;
        var outer = new TowerWorkAccounting();
        using (outer.Activate())
        {
            await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
                () => { invoked = true; return Task.FromResult(1); }));
            TowerWorkAccounting.Add("outerRestored");
        }
        Assert.Single(outer.Snapshot()); Assert.Equal(1, outer.Snapshot()["outerRestored"]);
        Assert.False(invoked); Assert.False(File.Exists(Receipt)); Assert.False(File.Exists(Path.Combine(Study, "worker.json")));
    }

    [Fact]
    public async Task Existing_receipt_is_rejected_before_invoking_action()
    {
        var pin = Bind(); File.WriteAllText(Receipt, "existing"); var invoked = false;
        await Assert.ThrowsAsync<IOException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { invoked = true; return Task.FromResult(1); }));
        Assert.False(invoked); Assert.Equal("existing", File.ReadAllText(Receipt));
    }

    [Fact]
    public async Task Failed_async_action_retains_partial_counters_and_original_exception()
    {
        var pin = Bind(); var error = new InvalidOperationException("worker failed");
        var caught = await Assert.ThrowsAsync<InvalidOperationException>(() => TowerProposalWorkReceipt.Run<int>(Study, "nativeAudit", Binding, pin, async () => {
            await Task.Yield(); HarnessJson.Read<JsonElement>(Path.Combine(Study, "request.json")); throw error;
        }));
        Assert.Same(error, caught);
        Assert.Equal("Failed", ReadReceipt().GetProperty("outcome").GetString());
        Assert.Equal(2, ReadReceipt().GetProperty("counters").GetProperty("jsonParseCompleted").GetInt64());
        Assert.Equal(1, ReadReceipt().GetProperty("counters").GetProperty("workerAuthenticationsCompleted").GetInt64());
        Assert.False(TowerWorkAccounting.Enabled);
    }

    [Fact]
    public async Task Cancelled_action_retains_failed_receipt()
    {
        var pin = Bind(); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => Task.FromCanceled<int>(cancel.Token)));
        Assert.Equal("Failed", ReadReceipt().GetProperty("outcome").GetString());
        Assert.Equal(1, ReadReceipt().GetProperty("counters").GetProperty("workerAuthenticationsAttempted").GetInt64());
    }

    [Fact]
    public async Task Request_mutation_cannot_receive_success_receipt()
    {
        var pin = Bind();
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { File.AppendAllText(Path.Combine(Study, "request.json"), " "); return Task.FromResult(1); }));
        Assert.Equal("Failed", ReadReceipt().GetProperty("outcome").GetString());
        var counts = ReadReceipt().GetProperty("counters");
        Assert.Equal(new FileInfo(Binding).Length + 16 + 17, counts.GetProperty("applicationReadBytes.json").GetInt64());
        Assert.Equal(new FileInfo(TowerProposalWorkReceipt.ProducerPath).Length, counts.GetProperty("applicationReadBytes.other").GetInt64());
        Assert.Equal(2, counts.GetProperty("workerAuthenticationsAttempted").GetInt64());
        Assert.Equal(1, counts.GetProperty("workerAuthenticationsCompleted").GetInt64());
        Assert.Equal(1, counts.GetProperty("workerAuthenticationsFailed").GetInt64());
    }

    [Theory]
    [InlineData("native")]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task Authentication_only_counts_each_actual_read_and_binding_parse(string phase)
    {
        var pin = Bind(phase);
        await TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, () => Task.FromResult(0));
        var counts = ReadReceipt().GetProperty("counters");
        Assert.Equal(new FileInfo(Binding).Length + 2 * 16, counts.GetProperty("applicationReadBytes.json").GetInt64());
        Assert.Equal(2 * new FileInfo(TowerProposalWorkReceipt.ProducerPath).Length, counts.GetProperty("applicationReadBytes.other").GetInt64());
        Assert.Equal(new FileInfo(Binding).Length, counts.GetProperty("jsonInputBytes").GetInt64());
        Assert.Equal(1, counts.GetProperty("jsonParseAttempts").GetInt64());
        Assert.Equal(1, counts.GetProperty("jsonParseCompleted").GetInt64());
        Assert.Equal(2, counts.GetProperty("workerAuthenticationsAttempted").GetInt64());
        Assert.DoesNotContain(counts.EnumerateObject(), p => p.Name.StartsWith("applicationWriteBytes", StringComparison.Ordinal));
        Assert.False(TowerWorkAccounting.Enabled);
    }

    [Fact]
    public async Task Nested_async_collector_cannot_steal_worker_authentication_or_pollute_parent()
    {
        var pin = Bind(); var nested = new TowerWorkAccounting(); var outer = new TowerWorkAccounting();
        using (outer.Activate())
        {
            await TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin, async () => {
                using (nested.Activate())
                {
                    await Task.Yield();
                    HarnessJson.FileHash(Path.Combine(Study, "request.json"));
                }
                await Task.Yield();
                TowerWorkAccounting.Add("workerAfterNested");
                return 0;
            });
            TowerWorkAccounting.Add("outerAfterWorker");
        }
        Assert.Single(outer.Snapshot()); Assert.Equal(1, outer.Snapshot()["outerAfterWorker"]);
        Assert.Single(nested.Snapshot()); Assert.Equal(16, nested.Snapshot()["applicationReadBytes.json"]);
        var counts = ReadReceipt().GetProperty("counters");
        Assert.Equal(new FileInfo(Binding).Length + 2 * 16, counts.GetProperty("applicationReadBytes.json").GetInt64());
        Assert.Equal(1, counts.GetProperty("workerAfterNested").GetInt64());
    }

    [Fact]
    public async Task Missing_request_after_action_retains_only_completed_reads_and_restores_parent()
    {
        var pin = Bind(); var outer = new TowerWorkAccounting();
        using (outer.Activate())
        {
            await Assert.ThrowsAsync<FileNotFoundException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin, () => {
                File.Delete(Path.Combine(Study, "request.json")); return Task.FromResult(0);
            }));
            TowerWorkAccounting.Add("outerAfterFailure");
        }
        Assert.Single(outer.Snapshot()); Assert.Equal(1, outer.Snapshot()["outerAfterFailure"]);
        var receipt = ReadReceipt(); var counts = receipt.GetProperty("counters");
        Assert.Equal("Failed", receipt.GetProperty("outcome").GetString());
        Assert.Equal(new FileInfo(Binding).Length + 16, counts.GetProperty("applicationReadBytes.json").GetInt64());
        Assert.Equal(new FileInfo(TowerProposalWorkReceipt.ProducerPath).Length, counts.GetProperty("applicationReadBytes.other").GetInt64());
        Assert.Equal(1, counts.GetProperty("workerAuthenticationsFailed").GetInt64());
    }

    [Theory]
    [InlineData("oversize")]
    [InlineData("bom")]
    [InlineData("trailing")]
    public async Task Malformed_binding_retains_original_byte_parser_rejection(string fault)
    {
        Bind();
        var raw = File.ReadAllBytes(Binding);
        File.WriteAllBytes(Binding, fault switch {
            "oversize" => new byte[16385],
            "bom" => [0xef, 0xbb, 0xbf, .. raw],
            _ => [.. raw, (byte)'x']
        });
        var pin = HarnessJson.FileHash(Binding); var invoked = false;
        await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { invoked = true; return Task.FromResult(0); }));
        Assert.False(invoked); Assert.False(File.Exists(Receipt)); Assert.False(TowerWorkAccounting.Enabled);
    }

    [Theory]
    [InlineData("tower-proposal-study-run", "native")]
    [InlineData("tower-proposal-study-audit", "nativeAudit")]
    [InlineData("tower-proposal-study-publication-check", "publication")]
    public async Task Real_command_retains_invalid_literal_input_failure_without_preparation(string command, string phase)
    {
        var pin = Bind(phase);
        await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalStudy.Command([command, Study, "--work-binding", Binding, pin]));
        var receipt = ReadReceipt();
        Assert.Equal("Failed", receipt.GetProperty("outcome").GetString());
        Assert.Equal(phase, receipt.GetProperty("phase").GetString());
        Assert.Single(Directory.GetFiles(Study));
    }

    [Fact]
    public async Task Literal_worker_exports_bound_native_receipt_for_independent_verification()
    {
        var pin = Bind();
        await TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin, () => {
            HarnessJson.Read<JsonElement>(Path.Combine(Study, "request.json")); return Task.FromResult(0);
        });
        if (Environment.GetEnvironmentVariable("LL_WORKER_RECEIPT_EXPORT") is { Length: > 0 } export)
        {
            Assert.False(Directory.Exists(export)); Directory.CreateDirectory(export);
            foreach (var (source, name) in new[] { (Binding, "binding.json"), (Receipt, "native-work.json"),
                         (Path.Combine(Study, "request.json"), "request.json") }) File.Copy(source, Path.Combine(export, name));
            HarnessJson.WriteNew(Path.Combine(export, "producer.json"), new { path = TowerProposalWorkReceipt.ProducerPath,
                sha256 = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath), bindingSha256 = pin,
                nativeBindingAuthenticationCounted = true, fixtureOnly = true });
            HarnessJson.WriteNew(Path.Combine(export, "files.json"), Directory.GetFiles(export).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
        }
    }
}
