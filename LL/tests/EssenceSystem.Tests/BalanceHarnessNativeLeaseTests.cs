using BalanceHarness;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessNativeLeaseTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-native-leases-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Lease fixtures cannot fight.")).Activate();
    private string Study => Path.Combine(root, "output");
    private string Registry => Path.Combine(root, "complete-family-allocation");
    private string Racing(int n = 1, string arm = "control") => Path.Combine(Study, $"search/root-{n:D2}/{arm}/racing");
    private string Sidecar(string name) => Path.Combine(root, name + ".json");
    private static NativeLeaseBudget Budget(int cap = 1) => new(TowerNativeLeases.Version, cap);
    public BalanceHarnessNativeLeaseTests() { Directory.CreateDirectory(Study); File.WriteAllText(Path.Combine(Study, "request.json"), "{}"); }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private TowerNativeLeases Scope(int cap = 1) => new(Study, "native", Budget(cap));
    private JsonElement Snapshot(TowerNativeLeases scope) => JsonSerializer.SerializeToElement(scope.Snapshot(), HarnessJson.Options);
    private string Bind(string phase, Action<JsonObject>? edit = null)
    {
        var producer = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath);
        var b = new ProposalWorkerBinding(TowerProposalWorkReceipt.LeaseVersion, phase, Study,
            HarnessJson.FileHash(Path.Combine(Study, "request.json")), producer, producer, Sidecar("receipt")) {
            PublicationPath = Sidecar("publication"), PublicationPersistencePath = Sidecar("persistence"),
            SidecarByteLimits = new(150000, 100000, 100000, 350000),
            PendingFamiliesBudget = new(TowerProposalPendingFamilies.Version, phase == "native" ? 1024 : 0,
                phase == "native" ? 1024 : 0, phase == "native" ? 1024 : 0), NativeLeaseBudget = Budget(phase == "native" ? 1 : 0)
        };
        var node = JsonSerializer.SerializeToNode(b, HarnessJson.Options)!.AsObject(); edit?.Invoke(node);
        File.WriteAllText(Sidecar("binding"), node.ToJsonString()); return HarnessJson.FileHash(Sidecar("binding"));
    }
    private JsonElement Receipt => JsonSerializer.Deserialize<JsonElement>(File.ReadAllBytes(Sidecar("receipt")));
    private Task<int> Run(string phase, string pin, Func<Task<int>> body) => TowerProposalWorkReceipt.Run(Study, phase, Sidecar("binding"), pin, body);
    private void Export(string name)
    {
        if (Environment.GetEnvironmentVariable("LL_NATIVE_LEASE_EXPORT") is not { Length: > 0 } export) return;
        var folder = Path.Combine(export, name); Assert.False(Directory.Exists(folder)); Directory.CreateDirectory(folder);
        foreach (var role in new[] { "binding", "receipt", "publication", "persistence" }) File.Copy(Sidecar(role), Path.Combine(folder, role + ".json"));
        File.Copy(Path.Combine(Study, "request.json"), Path.Combine(folder, "request.json"));
        HarnessJson.WriteNew(Path.Combine(folder, "fixture.json"), new { literalBody = true, actualCombat = 0, productionEntropyDraws = 0,
            scientificReservations = 0, nativeEncounterPreparations = 0, wholeProcessCoverage = false, usableForAdmission = false,
            producerPath = TowerProposalWorkReceipt.ProducerPath, producerSha256 = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath) });
        HarnessJson.WriteNew(Path.Combine(folder, "files.json"), Directory.GetFiles(folder).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
    }
    [Theory]
    [InlineData("native")]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task V8_owns_native_lease_lifetimes_and_preserves_pending_contract(string phase)
    {
        using var registry = TowerCompactBundle.AcquireWriter(Registry);
        using var output = TowerCompactBundle.AcquireWriter(Study);
        var pin = Bind(phase);
        await Run(phase, pin, () => {
            if (phase == "native")
            {
                TowerWorkAccounting.RequireWriterLeaseHeld(Registry); TowerWorkAccounting.RequireWriterLeaseHeld(Study);
                for (var n = 1; n <= 12; n++) foreach (var arm in new[] { "control", "candidate" })
                { using var lease = TowerCompactBundle.AcquireWriter(Racing(n, arm)); Assert.False(lease.CanWrite); Assert.Equal(0, lease.Length); }
            }
            return Task.FromResult(1);
        });
        var r = Receipt; var v = r.GetProperty("nativeLeases");
        Assert.Equal(TowerProposalWorkReceipt.LeaseReceiptVersion, r.GetProperty("version").GetString());
        Assert.Equal("Complete", v.GetProperty("outcome").GetString()); Assert.Equal(0, v.GetProperty("currentOwnedHandles").GetInt32());
        Assert.Equal(phase == "native" ? 26 : 0, v.GetProperty("entries").GetArrayLength());
        Assert.Equal(phase == "native" ? 24 : 0, v.GetProperty("entries").EnumerateArray().Sum(e => e.GetProperty("released").GetInt32()));
        Assert.Equal(phase == "native" ? 2 : 0, v.GetProperty("entries").EnumerateArray().Sum(e => e.GetProperty("heldConflicts").GetInt32()));
        Assert.Equal("Complete", r.GetProperty("pendingStorage").GetProperty("outcome").GetString());
        Export(phase);
    }
    [Theory]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task No_lease_phases_reject_before_parent_creation(string phase)
    {
        var pin = Bind(phase);
        await Assert.ThrowsAsync<InvalidDataException>(() => Run(phase, pin, () => { using var lease = TowerCompactBundle.AcquireWriter(Racing()); return Task.FromResult(0); }));
        Assert.False(Directory.Exists(Path.Combine(Study, "search"))); Export("rejected-" + phase);
    }
    [Fact]
    public async Task Missing_enclosing_ownership_is_not_a_successful_probe()
    {
        var pin = Bind("native");
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => Run("native", pin, () => {
            TowerWorkAccounting.RequireWriterLeaseHeld(Registry); return Task.FromResult(0);
        }));
        Assert.Equal("Missing enclosing registry/output ownership.", error.Message);
        Assert.False(File.Exists(Registry + ".writer.lock")); Export("missing-owner");
    }
    [Fact]
    public async Task Escaped_handle_is_closed_but_scope_cannot_complete()
    {
        var pin = Bind("native"); Microsoft.Win32.SafeHandles.SafeFileHandle? handle = null;
        await Assert.ThrowsAsync<InvalidDataException>(() => Run("native", pin, () => {
            var escaped = TowerCompactBundle.AcquireWriter(Racing()); handle = escaped.SafeFileHandle; return Task.FromResult(0);
        }));
        Assert.True(handle!.IsClosed); Assert.False(File.Exists(Racing() + ".writer.lock")); Export("escaped-handle");
    }
    [Theory]
    [InlineData("array")]
    [InlineData("span")]
    [InlineData("byte")]
    [InlineData("async-array")]
    [InlineData("async-memory")]
    [InlineData("begin")]
    [InlineData("length")]
    [InlineData("empty")]
    public async Task All_managed_write_surfaces_fail_before_growth_even_if_caught(string method)
    {
        var scope = Scope();
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(async () => {
            using var lease = TowerCompactBundle.AcquireWriter(Racing());
            await Assert.ThrowsAsync<NotSupportedException>(async () => {
                switch (method)
                {
                    case "array": lease.Write(new byte[] { 1 }, 0, 1); break;
                    case "span": lease.Write(new byte[] { 1 }.AsSpan()); break;
                    case "byte": lease.WriteByte(1); break;
                    case "async-array": await lease.WriteAsync(new byte[] { 1 }, 0, 1); break;
                    case "async-memory": await lease.WriteAsync(new byte[] { 1 }.AsMemory()); break;
                    case "begin": lease.BeginWrite([1], 0, 1, null, null); break;
                    case "length": lease.SetLength(1); break;
                    default: lease.Write(Array.Empty<byte>(), 0, 0); break;
                }
            });
            Assert.Equal(0, lease.Length); return 0;
        }));
        Assert.False(File.Exists(Racing() + ".writer.lock")); Assert.True(Snapshot(scope).GetProperty("failedOrUnknown").GetBoolean());
    }
    [Fact]
    public async Task Original_handle_has_no_write_access()
    {
        var scope = Scope();
        await scope.RunAsync(() => {
            using var lease = TowerCompactBundle.AcquireWriter(Racing());
            Assert.ThrowsAny<Exception>(() => RandomAccess.Write(lease.SafeFileHandle, new byte[] { 1 }, 0));
            Assert.Equal(0, lease.Length); return Task.FromResult(0);
        });
        Assert.Equal("Complete", Snapshot(scope).GetProperty("outcome").GetString());
    }
    [Theory]
    [InlineData("reacquire")]
    [InlineData("concurrent")]
    [InlineData("zero")]
    [InlineData("undeclared")]
    [InlineData("wrong-role")]
    [InlineData("wrong-probe")]
    public async Task Attempts_handles_and_roles_are_bounded(string fault)
    {
        var scope = Scope(fault == "zero" ? 0 : 1);
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => {
            if (fault == "wrong-probe") { TowerWorkAccounting.RequireWriterLeaseHeld(Racing()); return Task.FromResult(0); }
            if (fault == "reacquire") { using var first = TowerCompactBundle.AcquireWriter(Racing()); first.Dispose(); }
            using var current = fault == "concurrent" ? TowerCompactBundle.AcquireWriter(Racing()) : null;
            using var rejected = TowerCompactBundle.AcquireWriter(fault == "concurrent" ? Racing(2)
                : fault == "undeclared" ? Path.Combine(root, "outside/nested/other") : fault == "wrong-role" ? Study : Racing());
            return Task.FromResult(0);
        }));
        Assert.False(Directory.Exists(Path.Combine(root, "outside")));
    }
    [Theory]
    [InlineData("dispose")]
    [InlineData("close")]
    [InlineData("async")]
    public async Task Release_is_once_and_keeps_original_collector(string mode)
    {
        var scope = Scope(); var first = new TowerWorkAccounting(); var second = new TowerWorkAccounting();
        await scope.RunAsync(async () => {
            FileStream lease; using (first.Activate()) lease = TowerCompactBundle.AcquireWriter(Racing());
            using (second.Activate())
            { if (mode == "async") await lease.DisposeAsync(); else if (mode == "close") lease.Close(); else lease.Dispose(); lease.Dispose(); }
            return 0;
        });
        Assert.Equal(1, first.Snapshot()["writerLeaseReleaseCompleted"]); Assert.Empty(second.Snapshot());
    }
    [Fact]
    public async Task Existing_nonempty_lease_is_preserved_without_open_or_delete()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Racing())!); var path = Racing() + ".writer.lock"; File.WriteAllText(path, "literal");
        var scope = Scope();
        await Assert.ThrowsAsync<InvalidDataException>(() => scope.RunAsync(() => { using var lease = TowerCompactBundle.AcquireWriter(Racing()); return Task.FromResult(0); }));
        Assert.Equal("literal", File.ReadAllText(path));
    }
    [Fact]
    public async Task Failed_release_preserves_original_body_error_and_unknown_state()
    {
        var scope = Scope(); var original = new IOException("literal body");
        var caught = await Assert.ThrowsAsync<IOException>(() => scope.RunAsync<int>(() => {
            var lease = TowerCompactBundle.AcquireWriter(Racing()); lease.SafeFileHandle.Dispose(); throw original;
        }));
        Assert.Same(original, caught); Assert.True(caught.Data.Contains("NativeLeaseCleanupError"));
        Assert.Equal(1, Snapshot(scope).GetProperty("currentOwnedHandles").GetInt32());
        Assert.Equal(1, Snapshot(scope).GetProperty("entries").EnumerateArray().Sum(e => e.GetProperty("releaseFailures").GetInt32()));
    }
    [Theory]
    [InlineData("old")]
    [InlineData("null")]
    [InlineData("missing")]
    [InlineData("negative")]
    [InlineData("too-many")]
    [InlineData("audit")]
    public async Task Invalid_bindings_reject_before_sidecars_and_body(string fault)
    {
        var phase = fault == "audit" ? "nativeAudit" : "native";
        var pin = Bind(phase, b => {
            if (fault == "old") b["version"] = TowerProposalWorkReceipt.FamilyPendingVersion;
            else if (fault == "null") b["nativeLeaseBudget"] = null;
            else if (fault == "missing") b.Remove("nativeLeaseBudget");
            else b["nativeLeaseBudget"]!["maxConcurrentLeases"] = fault == "negative" ? -1 : fault == "too-many" ? 27 : 1;
        });
        var called = false;
        await Assert.ThrowsAnyAsync<Exception>(() => Run(phase, pin, () => { called = true; return Task.FromResult(0); }));
        Assert.False(called); Assert.False(File.Exists(Sidecar("receipt")));
    }
    [Fact]
    public void Protected_path_overlap_is_rejected() => Assert.Throws<InvalidDataException>(() => Scope().ProtectWorkerPaths(Racing() + ".writer.lock"));
}
