using BalanceHarness;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPhasePendingTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-phase-pending-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Phase fixtures cannot fight.")).Activate();
    private string Study => Path.Combine(root, "study");
    private string Binding => Path.Combine(root, "binding.json");
    private string Target => Path.Combine(Study, "study/literal.json");
    private string Sidecar(string role) => Path.Combine(root, role + ".json");
    public BalanceHarnessPhasePendingTests() { Directory.CreateDirectory(Study); File.WriteAllText(Path.Combine(Study, "request.json"), "{}"); }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private string Bind(string phase, Action<JsonObject>? edit = null)
    {
        var producer = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath);
        var b = new ProposalWorkerBinding(TowerProposalWorkReceipt.PhasePendingVersion, phase, Study,
            HarnessJson.FileHash(Path.Combine(Study, "request.json")), producer, producer, Sidecar("receipt")) {
            PublicationPath = Sidecar("publication"), PublicationPersistencePath = Sidecar("persistence"),
            SidecarByteLimits = new(100000, 100000, 100000, 300000),
            PendingStorageBudget = phase == "native" ? new([new(Target + ".pending", Target, 1024, 1)], 1024, 1024) : new([], 0, 0)
        };
        var node = JsonSerializer.SerializeToNode(b, HarnessJson.Options)!.AsObject(); edit?.Invoke(node);
        File.WriteAllText(Binding, node.ToJsonString()); return HarnessJson.FileHash(Binding);
    }
    private JsonElement Receipt => JsonSerializer.Deserialize<JsonElement>(File.ReadAllBytes(Sidecar("receipt")));
    private void Export(string name)
    {
        if (Environment.GetEnvironmentVariable("LL_PHASE_PENDING_EXPORT") is not { Length: > 0 } export) return;
        var folder = Path.Combine(export, name); Assert.False(Directory.Exists(folder)); Directory.CreateDirectory(folder);
        foreach (var role in new[] { "receipt", "publication", "persistence" }) File.Copy(Sidecar(role), Path.Combine(folder, role + ".json"));
        File.Copy(Binding, Path.Combine(folder, "binding.json")); File.Copy(Path.Combine(Study, "request.json"), Path.Combine(folder, "request.json"));
        if (File.Exists(Target)) File.Copy(Target, Path.Combine(folder, "literal.json"));
        HarnessJson.WriteNew(Path.Combine(folder, "fixture.json"), new { literalBody = true, actualCombat = 0, productionEntropyDraws = 0,
            scientificReservations = 0, nativeEncounterPreparations = 0, wholeProcessCoverage = false, usableForAdmission = false,
            producerPath = TowerProposalWorkReceipt.ProducerPath, producerSha256 = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath) });
        HarnessJson.WriteNew(Path.Combine(folder, "files.json"), Directory.GetFiles(folder).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
    }
    [Theory]
    [InlineData("native")]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task V6_runs_declared_Save_or_explicit_no_writer_phase(string phase)
    {
        var pin = Bind(phase);
        await TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, () => {
            if (phase == "native") TowerProposalStudy.Save(Study, "study/literal.json", new { literal = true }, () => { });
            return Task.FromResult(7);
        });
        var r = Receipt; var pending = r.GetProperty("pendingStorage");
        Assert.Equal(TowerProposalWorkReceipt.PhasePendingReceiptVersion, r.GetProperty("version").GetString());
        Assert.Equal("Complete", pending.GetProperty("outcome").GetString());
        Assert.Equal(pin, r.GetProperty("bindingSha256").GetString());
        Assert.Equal(0, pending.GetProperty("trackedLiveBytes").GetInt64());
        if (phase == "native") Assert.Equal(new FileInfo(Target).Length, pending.GetProperty("acceptedWriteBytes").GetInt64());
        else { Assert.Equal(0, pending.GetProperty("contracts").GetArrayLength()); Assert.Equal(0, pending.GetProperty("acceptedWriteBytes").GetInt64()); }
        Export(phase);
    }
    [Theory]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task No_writer_phase_rejects_even_empty_pending_create_before_factory(string phase)
    {
        var pin = Bind(phase); var created = false;
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, () => {
            using var stream = TowerWorkAccounting.OpenWrite(Target + ".pending", () => { created = true; return new MemoryStream(); }, scratch: true);
            return Task.FromResult(1);
        }));
        Assert.False(created); Assert.False(File.Exists(Target)); Assert.Equal("Failed", Receipt.GetProperty("outcome").GetString());
        Assert.Equal("FailedOrIncomplete", Receipt.GetProperty("pendingStorage").GetProperty("outcome").GetString()); Export("rejected-" + phase);
    }
    [Theory]
    [InlineData("native-empty")]
    [InlineData("audit-files")]
    [InlineData("empty-nonzero")]
    [InlineData("outside")]
    [InlineData("not-adjacent")]
    [InlineData("source")]
    [InlineData("content")]
    [InlineData("executable")]
    [InlineData("launch.json")]
    [InlineData("v5-empty")]
    public async Task Invalid_phase_declaration_fails_before_files_and_body(string fault)
    {
        var phase = fault is "empty-nonzero" or "v5-empty" ? "nativeAudit" : "native";
        var pin = Bind(phase, node => {
            var budget = node["pendingStorageBudget"]!;
            if (fault == "native-empty") budget["files"] = new JsonArray();
            else if (fault == "audit-files") node["phase"] = "nativeAudit";
            else if (fault == "empty-nonzero") budget["maxLiveBytes"] = 1;
            else if (fault == "v5-empty") node["version"] = TowerProposalWorkReceipt.PendingVersion;
            else
            {
                var file = budget["files"]![0]!;
                var target = fault == "outside" ? Path.Combine(root, "outside.json")
                    : fault == "not-adjacent" ? Target : Path.Combine(Study, fault);
                file["destination"] = target; file["path"] = target + (fault == "not-adjacent" ? ".other" : ".pending");
            }
        });
        if (fault == "audit-files") phase = "nativeAudit";
        var called = false;
        await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, () => { called = true; return Task.FromResult(1); }));
        Assert.False(called); foreach (var role in new[] { "receipt", "publication", "persistence" }) Assert.False(File.Exists(Sidecar(role)));
    }
    [Fact]
    public async Task Undeclared_nested_Save_is_rejected_and_retained()
    {
        var pin = Bind("native");
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalWorkReceipt.Run(Study, "native", Binding, pin, () => {
            TowerProposalStudy.Save(Study, "study/heldout-01-undeclared.json", new { literal = true }, () => { }); return Task.FromResult(1);
        }));
        Assert.Equal("Failed", Receipt.GetProperty("outcome").GetString());
        Assert.False(File.Exists(Path.Combine(Study, "study/heldout-01-undeclared.json.pending"))); Export("undeclared-native");
    }
}
