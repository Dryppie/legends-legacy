using BalanceHarness;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessPendingFamilyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-pending-families-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Family fixtures cannot fight.")).Activate();
    private string Study => Path.Combine(root, "study");
    private string Binding => Path.Combine(root, "binding.json");
    private string Sidecar(string role) => Path.Combine(root, role + ".json");
    private static string Heldout(int n, char digest) => $"study/heldout-{n:D2}-{new string(digest, 64)}.json";
    private static ProposalPendingFamiliesBudget Budget(long file = 1024, long live = 1024, long total = 100000)
        => new(TowerProposalPendingFamilies.Version, file, live, total);
    public BalanceHarnessPendingFamilyTests() { Directory.CreateDirectory(Study); File.WriteAllText(Path.Combine(Study, "request.json"), "{}"); }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private string Bind(string phase, Action<JsonObject>? edit = null)
    {
        var producer = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath);
        var b = new ProposalWorkerBinding(TowerProposalWorkReceipt.FamilyPendingVersion, phase, Study,
            HarnessJson.FileHash(Path.Combine(Study, "request.json")), producer, producer, Sidecar("receipt")) {
            PublicationPath = Sidecar("publication"), PublicationPersistencePath = Sidecar("persistence"),
            SidecarByteLimits = new(100000, 100000, 100000, 300000),
            PendingFamiliesBudget = phase == "native" ? Budget() : Budget(0, 0, 0)
        };
        var node = JsonSerializer.SerializeToNode(b, HarnessJson.Options)!.AsObject(); edit?.Invoke(node);
        File.WriteAllText(Binding, node.ToJsonString()); return HarnessJson.FileHash(Binding);
    }
    private JsonElement Receipt => JsonSerializer.Deserialize<JsonElement>(File.ReadAllBytes(Sidecar("receipt")));
    private TowerPendingStorage Scope(ProposalPendingFamiliesBudget? budget = null) => TowerPendingStorage.ForFamilies(new(Study, "native", budget ?? Budget()));
    private void Put(string name, byte[]? bytes = null, bool replace = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Study, name))!);
        new TowerCompleteReservation.Storage(Study, 1000000).PutBytes(name, bytes ?? [1], replace);
    }
    private void Export(string name)
    {
        if (Environment.GetEnvironmentVariable("LL_PENDING_FAMILIES_EXPORT") is not { Length: > 0 } export) return;
        var folder = Path.Combine(export, name); Assert.False(Directory.Exists(folder)); Directory.CreateDirectory(folder);
        foreach (var role in new[] { "receipt", "publication", "persistence" }) File.Copy(Sidecar(role), Path.Combine(folder, role + ".json"));
        File.Copy(Binding, Path.Combine(folder, "binding.json")); File.Copy(Path.Combine(Study, "request.json"), Path.Combine(folder, "request.json"));
        HarnessJson.WriteNew(Path.Combine(folder, "fixture.json"), new { literalBody = true, actualCombat = 0, productionEntropyDraws = 0,
            scientificReservations = 0, nativeEncounterPreparations = 0, wholeProcessCoverage = false, usableForAdmission = false,
            producerPath = TowerProposalWorkReceipt.ProducerPath, producerSha256 = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath) });
        HarnessJson.WriteNew(Path.Combine(folder, "files.json"), Directory.GetFiles(folder).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
    }
    [Theory]
    [InlineData("native")]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task V7_covers_finite_catalogue_and_dynamic_names_or_no_writer_phase(string phase)
    {
        var pin = Bind(phase);
        await TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, () => {
            if (phase == "native")
            {
                Assert.Equal(36, TowerProposalPendingFamilies.FixedNames().Count());
                foreach (var name in TowerProposalPendingFamilies.FixedNames()) Put(name);
                Put("history-input.json", replace: true);
                for (var n = 1; n <= 12; n++) foreach (var hash in "abc")
                    TowerProposalStudy.Save(Study, Heldout(n, hash), new { literal = true }, () => { });
            }
            return Task.FromResult(7);
        });
        var r = Receipt; var pending = r.GetProperty("pendingStorage");
        Assert.Equal(TowerProposalWorkReceipt.FamilyPendingReceiptVersion, r.GetProperty("version").GetString());
        Assert.Equal("Complete", pending.GetProperty("outcome").GetString());
        Assert.Equal(pin, r.GetProperty("bindingSha256").GetString());
        Assert.Equal(0, pending.GetProperty("trackedLiveBytes").GetInt64());
        Assert.Equal(phase == "native" ? 72 : 0, pending.GetProperty("contracts").GetArrayLength());
        Assert.Equal(phase == "native" ? 73 : 0, pending.GetProperty("creates").EnumerateArray().Sum(x => x.GetProperty("count").GetInt32()));
        Export(phase);
    }
    [Theory]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task No_writer_phase_rejects_create_before_factory(string phase)
    {
        var pin = Bind(phase); var created = false;
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, () => {
            using var stream = TowerWorkAccounting.OpenWrite(Path.Combine(Study, Heldout(1, 'a')) + ".pending",
                () => { created = true; return new MemoryStream(); }, scratch: true);
            return Task.FromResult(1);
        }));
        Assert.False(created); Assert.Equal("Failed", Receipt.GetProperty("outcome").GetString()); Export("rejected-" + phase);
    }
    [Theory]
    [InlineData("fourth")]
    [InlineData("recreate-published")]
    [InlineData("recreate-deleted")]
    public async Task Released_paths_never_refund_member_or_create_allowances(string fault)
    {
        var pin = Bind("native"); var created = false;
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalWorkReceipt.Run(Study, "native", Binding, pin, () => {
            var name = Heldout(1, 'a'); var path = Path.Combine(Study, name) + ".pending";
            if (fault == "recreate-deleted")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using (var stream = TowerWorkAccounting.OpenWrite(path, () => File.Create(path), scratch: true)) stream.WriteByte(1);
                TowerPendingStorage.Delete(path);
            }
            else Put(name);
            if (fault == "fourth") { Put(Heldout(1, 'b')); Put(Heldout(1, 'c')); path = Path.Combine(Study, Heldout(1, 'd')) + ".pending"; }
            using var rejected = TowerWorkAccounting.OpenWrite(path, () => { created = true; return new MemoryStream(); }, scratch: true);
            return Task.FromResult(0);
        }));
        Assert.False(created); Assert.Equal("Failed", Receipt.GetProperty("outcome").GetString());
        if (fault == "fourth") Export("exhausted-native");
    }
    [Theory]
    [InlineData("root00")]
    [InlineData("root13")]
    [InlineData("uppercase")]
    [InlineData("short-hash")]
    [InlineData("nonhex")]
    [InlineData("wrong-extension")]
    [InlineData("nested")]
    [InlineData("outside")]
    [InlineData("input")]
    [InlineData("traversal")]
    public async Task Undeclared_names_fail_before_factory(string fault)
    {
        var name = fault switch {
            "root00" => Heldout(0, 'a'), "root13" => Heldout(13, 'a'), "uppercase" => Heldout(1, 'A'),
            "short-hash" => "study/heldout-01-a.json", "nonhex" => Heldout(1, 'z'),
            "wrong-extension" => Heldout(1, 'a') + ".gz", "nested" => "study/sub/" + Path.GetFileName(Heldout(1, 'a')),
            "outside" => Path.Combine(root, "heldout-01-" + new string('a', 64) + ".json"),
            "input" => "request.json", _ => "study/../" + Heldout(1, 'a') };
        var created = false; var scope = Scope();
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            using var stream = TowerWorkAccounting.OpenWrite(Path.Combine(Study, name) + ".pending",
                () => { created = true; return new MemoryStream(); }, scratch: true);
            return Task.FromResult(0);
        }));
        Assert.False(created);
    }
    [Theory]
    [InlineData("file")]
    [InlineData("live")]
    [InlineData("lifetime")]
    public async Task Dynamic_writes_obey_byte_caps(string cap)
    {
        var scope = Scope(cap == "file" ? Budget(1, 10, 10) : cap == "live" ? Budget(10, 1, 10) : Budget(10, 10, 1));
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            if (cap == "lifetime") { Put(Heldout(1, 'a')); Put(Heldout(1, 'b')); }
            else Put(Heldout(1, 'a'), [1, 2]);
            return Task.FromResult(0);
        }));
        var snapshot = JsonSerializer.SerializeToElement(scope.Snapshot(), HarnessJson.Options);
        Assert.Equal("FailedOrIncomplete", snapshot.GetProperty("outcome").GetString());
        Assert.Equal(cap == "lifetime" ? 1 : 0, snapshot.GetProperty("acceptedWriteBytes").GetInt64());
    }
    [Theory]
    [InlineData("v6")]
    [InlineData("explicit-null")]
    [InlineData("family-null")]
    [InlineData("missing")]
    [InlineData("unknown")]
    [InlineData("negative")]
    [InlineData("audit-nonzero")]
    public async Task Invalid_contracts_reject_before_sidecars_and_body(string fault)
    {
        var phase = fault == "audit-nonzero" ? "nativeAudit" : "native";
        var pin = Bind(phase, node => {
            if (fault == "v6") node["version"] = TowerProposalWorkReceipt.PhasePendingVersion;
            else if (fault == "explicit-null") node["pendingStorageBudget"] = null;
            else if (fault == "family-null") node["pendingFamiliesBudget"] = null;
            else if (fault == "missing") node.Remove("pendingFamiliesBudget");
            else if (fault == "unknown") node["pendingFamiliesBudget"]!["version"] = "unknown";
            else node["pendingFamiliesBudget"]!["maxFileBytes"] = fault == "negative" ? -1 : 1;
        });
        var called = false;
        await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, () => { called = true; return Task.FromResult(0); }));
        Assert.False(called); foreach (var role in new[] { "receipt", "publication", "persistence" }) Assert.False(File.Exists(Sidecar(role)));
    }
    [Fact]
    public void Dynamic_directory_cannot_overlap_protected_worker_input()
    {
        var scope = Scope();
        Assert.Throws<InvalidDataException>(() => scope.ProtectWorkerPaths(Path.Combine(Study, Heldout(1, 'a'))));
    }
}
