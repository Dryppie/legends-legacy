using BalanceHarness;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPendingBindingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-pending-binding-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Pending binding tests cannot fight.")).Activate();
    private string Study => Path.Combine(root, "study");
    private string Binding => Path.Combine(root, "binding.json");
    private string Destination => Path.Combine(Study, "literal.json");
    private string Pending => Destination + ".pending";
    private string FileFor(string role) => Path.Combine(root, role + ".json");
    public BalanceHarnessPendingBindingTests()
    {
        Directory.CreateDirectory(Study);
        File.WriteAllText(Path.Combine(Study, "request.json"), "{\"literal\":true}");
    }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private string Bind(string phase = "nativeAudit", Action<JsonObject>? edit = null)
    {
        var producer = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath);
        var value = new ProposalWorkerBinding(TowerProposalWorkReceipt.PendingVersion, phase, Study,
            HarnessJson.FileHash(Path.Combine(Study, "request.json")), producer, producer, FileFor("receipt")) {
            PublicationPath = FileFor("publication"), PublicationPersistencePath = FileFor("persistence"),
            SidecarByteLimits = new(100000, 100000, 100000, 300000),
            PendingStorageBudget = new([new(Pending, Destination, 3, 2)], 3, 6)
        };
        var node = JsonSerializer.SerializeToNode(value, HarnessJson.Options)!.AsObject();
        edit?.Invoke(node); File.WriteAllText(Binding, node.ToJsonString());
        return HarnessJson.FileHash(Binding);
    }
    private void Put() => new TowerCompleteReservation.Storage(Study, 100000).PutBytes("literal.json", [1, 2, 3], replace: true);
    private JsonElement Receipt => JsonSerializer.Deserialize<JsonElement>(File.ReadAllBytes(FileFor("receipt")));
    private Task<int> Run(string pin, Func<Task<int>> body, string phase = "nativeAudit")
        => TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, body);
    private void Export(string name)
    {
        if (Environment.GetEnvironmentVariable("LL_PENDING_BINDING_EXPORT") is not { Length: > 0 } export) return;
        var destination = Path.Combine(export, name);
        Assert.False(Directory.Exists(destination)); Directory.CreateDirectory(destination);
        foreach (var role in new[] { "receipt", "publication", "persistence" }) File.Copy(FileFor(role), Path.Combine(destination, role + ".json"));
        File.Copy(Binding, Path.Combine(destination, "binding.json"));
        File.Copy(Path.Combine(Study, "request.json"), Path.Combine(destination, "request.json"));
        if (File.Exists(Destination)) File.Copy(Destination, Path.Combine(destination, "literal.json"));
        if (File.Exists(Pending)) File.Copy(Pending, Path.Combine(destination, "literal.pending"));
        HarnessJson.WriteNew(Path.Combine(destination, "fixture.json"), new {
            realWorkerBoundary = true, literalBody = true, actualCombat = 0, productionEntropyDraws = 0,
            scientificReservations = 0, nativeEncounterPreparations = 0,
            producerPath = TowerProposalWorkReceipt.ProducerPath, producerSha256 = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath),
            wholeProcessCoverage = false, usableForAdmission = false
        });
        HarnessJson.WriteNew(Path.Combine(destination, "files.json"), Directory.GetFiles(destination).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
    }

    [Theory]
    [InlineData("native")]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task Authenticated_v5_bounds_replacements_and_publishes_v2_inside_capped_receipt(string phase)
    {
        var pin = Bind(phase);
        Assert.Equal(7, await Run(pin, () => { Put(); Put(); return Task.FromResult(7); }, phase));
        var receipt = Receipt; var pending = receipt.GetProperty("pendingStorage");
        Assert.Equal(TowerProposalWorkReceipt.PendingReceiptVersion, receipt.GetProperty("version").GetString());
        Assert.Equal(pin, receipt.GetProperty("bindingSha256").GetString());
        Assert.Equal("Complete", pending.GetProperty("outcome").GetString());
        Assert.Equal(6, pending.GetProperty("acceptedWriteBytes").GetInt64());
        Assert.Equal(6, pending.GetProperty("publishedBytes").GetInt64());
        Assert.Equal(0, pending.GetProperty("trackedLiveBytes").GetInt64());
        Assert.Equal(3, pending.GetProperty("peakTrackedLiveBytes").GetInt64());
        Assert.Equal(2, pending.GetProperty("creates")[0].GetProperty("count").GetInt32());
        Assert.False(File.Exists(Pending)); Assert.False(TowerWorkAccounting.Enabled);
        var publication = JsonSerializer.Deserialize<JsonElement>(File.ReadAllBytes(FileFor("publication")));
        Assert.Equal(HarnessJson.FileHash(FileFor("receipt")), publication.GetProperty("serializedReceiptSha256").GetString());
        Export(phase);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("empty")]
    [InlineData("negative")]
    [InlineData("bool")]
    [InlineData("overflow")]
    [InlineData("extra")]
    [InlineData("partial")]
    [InlineData("duplicate")]
    [InlineData("alias")]
    [InlineData("nested")]
    [InlineData("relative")]
    [InlineData("legacy")]
    [InlineData("legacy-null")]
    [InlineData("no-sidecar")]
    [InlineData("no-persistence")]
    [InlineData("independentAudit")]
    [InlineData("receipt")]
    [InlineData("binding")]
    [InlineData("request")]
    [InlineData("producer")]
    public async Task Invalid_binding_rejects_before_body_and_sidecar_creation(string fault)
    {
        var pin = Bind(edit: node => {
            var budget = node["pendingStorageBudget"]!.AsObject(); var file = budget["files"]![0]!;
            switch (fault)
            {
                case "missing": node.Remove("pendingStorageBudget"); break;
                case "null": node["pendingStorageBudget"] = null; break;
                case "empty": budget["files"] = new JsonArray(); break;
                case "negative": budget["maxLiveBytes"] = -1; break;
                case "bool": file["maxCreates"] = true; break;
                case "overflow": budget["maxTotalWrittenBytes"] = ulong.MaxValue; break;
                case "extra": file["extra"] = 1; break;
                case "partial": file.AsObject().Remove("maxCreates"); break;
                case "alias": file["destination"] = Pending; break;
                case "nested": file["destination"] = Path.Combine(Pending, "child"); break;
                case "relative": file["path"] = "literal.pending"; break;
                case "legacy": node["version"] = TowerProposalWorkReceipt.SidecarVersion; break;
                case "legacy-null": node["version"] = TowerProposalWorkReceipt.SidecarVersion; node["pendingStorageBudget"] = null; break;
                case "no-sidecar": node.Remove("sidecarByteLimits"); break;
                case "no-persistence": node.Remove("publicationPersistencePath"); break;
                case "independentAudit": node["phase"] = fault; break;
                case "receipt": file["destination"] = FileFor("receipt"); break;
                case "binding": file["path"] = Binding; break;
                case "request": file["destination"] = Path.Combine(Study, "request.json"); break;
                case "producer": file["destination"] = TowerProposalWorkReceipt.ProducerPath; break;
            }
        });
        if (fault == "duplicate")
        {
            File.WriteAllText(Binding, File.ReadAllText(Binding).Replace("\"maxLiveBytes\":", "\"maxLiveBytes\":3,\"maxLiveBytes\":", StringComparison.Ordinal));
            pin = HarnessJson.FileHash(Binding);
        }
        var called = false;
        await Assert.ThrowsAnyAsync<Exception>(() => Run(pin, () => { called = true; return Task.FromResult(1); }));
        Assert.False(called);
        foreach (var role in new[] { "receipt", "publication", "persistence" }) Assert.False(File.Exists(FileFor(role)));
    }

    [Theory]
    [InlineData("cap")]
    [InlineData("unresolved")]
    [InlineData("undeclared")]
    [InlineData("swallowed")]
    [InlineData("body")]
    public async Task Pending_failure_is_retained_as_failed_work(string fault)
    {
        var pin = Bind(); var original = new InvalidOperationException("literal body");
        var error = await Assert.ThrowsAnyAsync<Exception>(() => Run(pin, () => {
            if (fault == "body") throw original;
            if (fault == "undeclared") new TowerCompleteReservation.Storage(Study, 100000).PutBytes("other.json", [1]);
            else if (fault == "unresolved") HarnessJson.WriteNew(Pending, 1, scratch: true);
            else
            {
                Put(); Put();
                try { Put(); } catch when (fault == "swallowed") { }
            }
            return Task.FromResult(1);
        }));
        if (fault == "body") Assert.Same(original, error);
        Assert.Equal("Failed", Receipt.GetProperty("outcome").GetString());
        Assert.Equal("FailedOrIncomplete", Receipt.GetProperty("pendingStorage").GetProperty("outcome").GetString());
        Assert.True(Receipt.GetProperty("pendingStorage").GetProperty("failedProgressMayBeUnknown").GetBoolean());
        if (fault == "unresolved") Assert.True(File.Exists(Pending));
        Assert.False(TowerWorkAccounting.Enabled); Export("failed-" + fault);
    }

    [Fact]
    public async Task Pending_failure_remains_primary_when_receipt_cap_also_fails()
    {
        var pin = Bind(edit: node => node["sidecarByteLimits"]!["receipt"] = 0);
        var error = new InvalidOperationException("literal body");
        var caught = await Assert.ThrowsAsync<InvalidOperationException>(() => Run(pin, () => throw error));
        Assert.Same(error, caught); Assert.True(error.Data.Contains("WorkReceiptPublicationError"));
        Assert.Equal(0, new FileInfo(FileFor("receipt")).Length);
    }

    [Fact]
    public async Task Existing_terminal_prevents_pending_body()
    {
        var pin = Bind(); File.WriteAllText(FileFor("persistence"), "preserve"); var called = false;
        await Assert.ThrowsAsync<IOException>(() => Run(pin, () => { called = true; Put(); return Task.FromResult(1); }));
        Assert.False(called); Assert.False(File.Exists(Pending)); Assert.False(File.Exists(Destination));
        Assert.Equal("preserve", File.ReadAllText(FileFor("persistence")));
    }

    [Fact]
    public async Task Post_body_authentication_failure_cannot_report_successful_work()
    {
        var pin = Bind();
        await Assert.ThrowsAsync<InvalidDataException>(() => Run(pin, () => {
            Put(); File.AppendAllText(Path.Combine(Study, "request.json"), " "); return Task.FromResult(1);
        }));
        Assert.Equal("Failed", Receipt.GetProperty("outcome").GetString());
        Assert.Equal("Complete", Receipt.GetProperty("pendingStorage").GetProperty("outcome").GetString());
    }
}
