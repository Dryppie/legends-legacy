using BalanceHarness;
using S = BalanceHarness.TowerProposalStudy;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProposalRuntimeRetentionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "proposal-runtime-retention-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Runtime retention cannot fight.")).Activate();
    public BalanceHarnessProposalRuntimeRetentionTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    private (string Manifest, Dictionary<string, string> Files, long Bytes) Manifest(bool symbols)
    {
        var capture = Path.Combine(root, "capture");
        var files = TowerBossStudy.RetainExecutable(capture, ExecutionIdentity.Current(), 512L * 1048576, default)
            .ToDictionary(p => p.Key, p => p.Value);
        var bytes = TowerBulkCampaign.StorageBytes(Path.Combine(capture, "executable"), default);
        if (symbols)
        {
            var pdb = Path.Combine(Path.GetDirectoryName(typeof(S).Assembly.Location)!, "BalanceHarness.pdb");
            files.Add("BalanceHarness.pdb", HarnessJson.FileHash(pdb)); bytes += new FileInfo(pdb).Length;
        }
        var manifest = Path.Combine(root, "runtime.json"); HarnessJson.WriteNew(manifest, files);
        return (manifest, files, bytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Retains_exact_admitted_inventory_with_or_without_producing_symbols(bool symbols)
    {
        var (manifest, files, bytes) = Manifest(symbols); var output = Path.Combine(root, "retained");
        var retained = S.RetainRuntime(manifest, output, bytes, default);
        Assert.Equal(HarnessJson.Hash(files), HarnessJson.Hash(retained));
        Assert.Equal(bytes, TowerBulkCampaign.StorageBytes(Path.Combine(output, "executable"), default));
        Assert.Equal(symbols, File.Exists(Path.Combine(output, "executable/BalanceHarness.pdb")));
        S.ValidateRuntime(manifest, Path.Combine(output, "executable"), true);
        Assert.Throws<InvalidDataException>(() => S.RetainRuntime(manifest, output, bytes, default));
    }

    [Fact]
    public void Producing_symbols_share_the_same_copy_allowance()
    {
        var (manifest, _, bytes) = Manifest(true); var output = Path.Combine(root, "retained");
        Assert.Throws<InvalidDataException>(() => S.RetainRuntime(manifest, output, bytes - 1, default));
        Assert.True(TowerBulkCampaign.StorageBytes(Path.Combine(output, "executable"), default) <= bytes - 1);
        Assert.False(File.Exists(Path.Combine(output, "entropy.bin")));
        Assert.False(File.Exists(Path.Combine(output, "allocation.json")));
    }

    [Fact]
    public void Changed_symbols_are_rejected_before_copying()
    {
        var (manifest, files, bytes) = Manifest(true); files["BalanceHarness.pdb"] = new string('0', 64);
        var changed = Path.Combine(root, "changed.json"); HarnessJson.WriteNew(changed, files);
        var output = Path.Combine(root, "retained");
        Assert.Throws<InvalidDataException>(() => S.RetainRuntime(changed, output, bytes, default));
        Assert.False(Directory.Exists(Path.Combine(output, "executable")));
    }

    [Fact]
    public void Inventory_cannot_omit_required_executable_metadata()
    {
        var (manifest, files, bytes) = Manifest(true); files.Remove("BalanceHarness.deps.json");
        var changed = Path.Combine(root, "changed.json"); HarnessJson.WriteNew(changed, files);
        Assert.Throws<InvalidDataException>(() => S.RetainRuntime(changed, Path.Combine(root, "retained"), bytes, default));
    }

    [Fact]
    public void Linked_or_escaping_inventory_paths_and_cancelled_copy_are_rejected()
    {
        var (manifest, files, bytes) = Manifest(true); files.Add("../escape.pdb", new string('a', 64));
        var changed = Path.Combine(root, "changed.json"); HarnessJson.WriteNew(changed, files);
        var output = Path.Combine(root, "retained");
        Assert.Throws<InvalidDataException>(() => S.RetainRuntime(changed, output, bytes, default));
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Throws<OperationCanceledException>(() => S.RetainRuntime(manifest, output, bytes, stop.Token));
        Assert.False(Directory.Exists(Path.Combine(output, "executable")));
    }
}
