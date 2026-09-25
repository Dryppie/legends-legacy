using BalanceHarness;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessWorkerSidecarTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ll-worker-sidecars-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Sidecar tests cannot fight.")).Activate();
    private string Study => Path.Combine(root, "study");
    private string Binding => Path.Combine(root, "binding.json");
    private string FileFor(string role) => Path.Combine(root, role + ".json");
    public BalanceHarnessWorkerSidecarTests()
    {
        Directory.CreateDirectory(Study);
        File.WriteAllText(Path.Combine(Study, "request.json"), "{\"literal\":true}");
    }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private string Bind(string phase = "nativeAudit", Action<JsonObject>? edit = null)
    {
        var producer = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath);
        var binding = new ProposalWorkerBinding(TowerProposalWorkReceipt.SidecarVersion, phase, Study,
            HarnessJson.FileHash(Path.Combine(Study, "request.json")), producer, producer, FileFor("receipt")) {
            PublicationPath = FileFor("publication"), PublicationPersistencePath = FileFor("persistence"),
            SidecarByteLimits = new(100000, 100000, 100000, 300000)
        };
        var node = JsonSerializer.SerializeToNode(binding, HarnessJson.Options)!.AsObject();
        edit?.Invoke(node); File.WriteAllText(Binding, node.ToJsonString());
        return HarnessJson.FileHash(Binding);
    }
    private JsonElement Read(string role) => JsonSerializer.Deserialize<JsonElement>(File.ReadAllBytes(FileFor(role)));

    [Theory]
    [InlineData("native")]
    [InlineData("nativeAudit")]
    [InlineData("publication")]
    public async Task V4_literal_body_closes_all_original_sidecars(string phase)
    {
        var pin = Bind(phase);
        Assert.Equal(7, await TowerProposalWorkReceipt.Run(Study, phase, Binding, pin, () => Task.FromResult(7)));
        foreach (var role in new[] { "receipt", "publication", "persistence" })
        {
            Assert.Equal("Complete", Read(role).GetProperty("outcome").GetString());
            Assert.InRange(new FileInfo(FileFor(role)).Length, 1, 100000);
            using var exclusive = new FileStream(FileFor(role), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        Assert.Equal(new FileInfo(FileFor("receipt")).Length, Read("publication").GetProperty("serializedReceiptBytes").GetInt64());
        Assert.Equal(new FileInfo(FileFor("publication")).Length, Read("persistence").GetProperty("serializedObservationBytes").GetInt64());
        Assert.False(TowerWorkAccounting.Enabled);
        if (Environment.GetEnvironmentVariable("LL_WORKER_SIDECAR_EXPORT") is { Length: > 0 } export)
        {
            var destination = Path.Combine(export, phase);
            Assert.False(Directory.Exists(destination)); Directory.CreateDirectory(destination);
            foreach (var role in new[] { "receipt", "publication", "persistence" }) File.Copy(FileFor(role), Path.Combine(destination, role + ".json"));
            File.Copy(Binding, Path.Combine(destination, "binding.json"));
            File.Copy(Path.Combine(Study, "request.json"), Path.Combine(destination, "request.json"));
            HarnessJson.WriteNew(Path.Combine(destination, "fixture.json"), new {
                realWorkerBoundary = true, literalBody = true, actualCombat = 0, productionEntropyDraws = 0,
                producerPath = TowerProposalWorkReceipt.ProducerPath, producerSha256 = HarnessJson.FileHash(TowerProposalWorkReceipt.ProducerPath),
                wholeProcessCoverage = false, usableForAdmission = false
            });
            HarnessJson.WriteNew(Path.Combine(destination, "files.json"), Directory.GetFiles(destination).ToDictionary(p => Path.GetFileName(p)!, HarnessJson.FileHash));
        }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("negative")]
    [InlineData("extra")]
    [InlineData("partial")]
    [InlineData("bool")]
    [InlineData("overflow")]
    [InlineData("legacy")]
    [InlineData("no-persistence")]
    [InlineData("duplicate")]
    public async Task Invalid_declarations_reject_before_files_and_action(string fault)
    {
        var pin = Bind(edit: node => {
            var limits = node["sidecarByteLimits"]!.AsObject();
            switch (fault)
            {
                case "missing": node.Remove("sidecarByteLimits"); break;
                case "null": node["sidecarByteLimits"] = null; break;
                case "negative": limits["receipt"] = -1; break;
                case "extra": limits["extra"] = 1; break;
                case "partial": limits.Remove("total"); break;
                case "bool": limits["receipt"] = true; break;
                case "overflow": limits["receipt"] = ulong.MaxValue; break;
                case "legacy": node["version"] = TowerProposalWorkReceipt.PersistenceVersion; break;
                case "no-persistence": node.Remove("publicationPersistencePath"); break;
            }
        });
        if (fault == "duplicate")
        {
            File.WriteAllText(Binding, File.ReadAllText(Binding).Replace("\"receipt\":", "\"receipt\":5,\"receipt\":", StringComparison.Ordinal));
            pin = HarnessJson.FileHash(Binding);
        }
        var called = false;
        await Assert.ThrowsAnyAsync<Exception>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { called = true; return Task.FromResult(7); }));
        Assert.False(called);
        foreach (var role in new[] { "receipt", "publication", "persistence" }) Assert.False(File.Exists(FileFor(role)));
        Assert.False(TowerWorkAccounting.Enabled);
    }

    [Theory]
    [InlineData("receipt")]
    [InlineData("publication")]
    [InlineData("persistence")]
    [InlineData("total")]
    public async Task Zero_cap_rejects_publication_before_exceeding_limit(string role)
    {
        var pin = Bind(edit: node => node["sidecarByteLimits"]![role] = 0);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin, () => Task.FromResult(7)));
        var roles = role == "total" ? new[] { "receipt", "publication", "persistence" } : new[] { role };
        foreach (var member in roles) Assert.Equal(0, new FileInfo(FileFor(member)).Length);
        foreach (var member in new[] { "receipt", "publication", "persistence" })
        { using var exclusive = new FileStream(FileFor(member), FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
        Assert.False(TowerWorkAccounting.Enabled);
    }

    [Fact]
    public async Task Worker_error_survives_later_cap_failure()
    {
        var pin = Bind(edit: node => node["sidecarByteLimits"]!["receipt"] = 0);
        var original = new InvalidOperationException("literal body");
        var caught = await Assert.ThrowsAsync<InvalidOperationException>(() => TowerProposalWorkReceipt.Run<int>(Study, "nativeAudit", Binding, pin, () => throw original));
        Assert.Same(original, caught); Assert.True(original.Data.Contains("WorkReceiptPublicationError"));
    }

    [Fact]
    public async Task Existing_terminal_is_preserved_before_body()
    {
        var pin = Bind(); File.WriteAllText(FileFor("persistence"), "preserve"); var called = false;
        await Assert.ThrowsAsync<IOException>(() => TowerProposalWorkReceipt.Run(Study, "nativeAudit", Binding, pin,
            () => { called = true; return Task.FromResult(7); }));
        Assert.False(called); Assert.Equal("preserve", File.ReadAllText(FileFor("persistence")));
        Assert.False(File.Exists(FileFor("receipt"))); Assert.False(File.Exists(FileFor("publication")));
    }

    [Fact]
    public void Combined_limit_applies_across_open_streams()
    {
        var sidecars = new TowerWorkerSidecars(new(10, 10, 10, 5));
        using var a = sidecars.Open("receipt", FileFor("receipt"), () => File.Create(FileFor("receipt")));
        using var b = sidecars.Open("publication", FileFor("publication"), () => File.Create(FileFor("publication")));
        a.Write([1, 2, 3]);
        Assert.Throws<InvalidDataException>(() => b.Write([4, 5, 6]));
        Assert.Equal(0, new FileInfo(FileFor("publication")).Length);
        Assert.Throws<InvalidDataException>(() => a.Write([4]));
    }

    private sealed class PartialFailure : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count) { base.Write(buffer, offset, Math.Min(2, count)); throw new IOException("hidden prefix"); }
    }
    [Fact]
    public void Failed_prefix_poisons_further_writes()
    {
        var sidecars = new TowerWorkerSidecars(new(10, 10, 10, 20)); using var raw = new PartialFailure();
        using var stream = sidecars.Open("receipt", FileFor("receipt"), () => raw);
        Assert.Throws<IOException>(() => stream.Write(new byte[5], 0, 5));
        Assert.Equal(2, raw.Length);
        Assert.Throws<InvalidDataException>(() => stream.Write([1]));
        Assert.Throws<InvalidDataException>(() => sidecars.Finish(null));
    }

    [Fact]
    public void Seek_and_truncate_are_not_exposed()
    {
        var sidecars = new TowerWorkerSidecars(new(10, 10, 10, 20));
        using var stream = sidecars.Open("receipt", FileFor("receipt"), () => File.Create(FileFor("receipt")));
        Assert.False(stream.CanSeek); Assert.False(stream.CanRead);
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
    }
}
