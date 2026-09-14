using System.IO.Compression;
using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCompactPublicationTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static TowerCompactDefinition Definition => new(1, "publication-test", 4, 2,
        [new("party", BalanceHarnessTowerBossDiscoveryContractTests.UserScenario with { Seeds = [930001, 930002, 930003, 930004] })]);

    private static async Task<string> Pending(DiscoveryTemp temp)
    {
        var path = Path.Combine(temp.Path, "run"); using var cancel = new CancellationTokenSource(); var completed = 0;
        using (new TowerPerformanceTrace(done => { if (done && ++completed == 2) cancel.Cancel(); }).Activate())
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerCompactBundle.CreateAsync(Root, Definition, path,
                cancel.Token, executionMode: "prepared-v1"));
        Assert.Equal(2, TowerCompactBundle.AttemptCount(path));
        Assert.True(Directory.Exists(Path.Combine(path, "chunks/.pending-000000")));
        return path;
    }

    [Fact]
    public async Task Recovery_preserves_completed_results_and_resume_executes_only_unstarted_fights()
    {
        using var temp = new DiscoveryTemp(); var path = await Pending(temp);
        var pending = Path.Combine(path, "chunks/.pending-000000/records.json.gz"); var hash = HarnessJson.FileHash(pending);
        var journal = HarnessJson.FileHash(Path.Combine(path, "bulk-attempts.jsonl"));
        using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Recovery must not fight")).Activate())
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => TowerCompactBundle.CreateAsync(Root, Definition, path, executionMode: "prepared-v1", resume: true));
            Assert.Equal(0, await BalanceHarness.Program.Main(["tower-compact-recover-publication", "--run", path]));
        }
        Assert.Equal(journal, HarnessJson.FileHash(Path.Combine(path, "bulk-attempts.jsonl")));
        Assert.Equal(hash, HarnessJson.FileHash(Path.Combine(path, "chunks/000000/records.json.gz")));
        var started = 0;
        using (new TowerPerformanceTrace(done => { if (!done) started++; }).Activate())
            await TowerCompactBundle.CreateAsync(Root, Definition, path, executionMode: "prepared-v1", resume: true);
        Assert.Equal(2, started); Assert.Equal(4, TowerCompactBundle.AttemptCount(path));
        var normal = Path.Combine(temp.Path, "normal");
        await TowerCompactBundle.CreateAsync(Root, Definition, normal, executionMode: "prepared-v1");
        Assert.Equal(HarnessJson.Hash(TowerCompactBundle.ReadSaved(normal).Cases), HarnessJson.Hash(TowerCompactBundle.ReadSaved(path).Cases));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerCompactBundle.RecoverPublicationAsync(path));
    }

    [Theory]
    [InlineData("transient")]
    [InlineData("persistent")]
    [InlineData("cancel")]
    [InlineData("changed-between-attempts")]
    public async Task Rename_retries_are_bounded_cancellable_and_never_overwrite_or_repeat_combat(string fault)
    {
        using var temp = new DiscoveryTemp(); var path = await Pending(temp); using var cancel = new CancellationTokenSource();
        var receiptPath = Path.Combine(path, "chunks/.pending-000000/receipt.json");
        var receipt = HarnessJson.Read<TowerCompactChunk>(receiptPath); var calls = 0;
        var attemptsHash = HarnessJson.FileHash(Path.Combine(path, "bulk-attempts.jsonl"));
        void Move(string source, string destination)
        {
            calls++;
            if (fault == "cancel") cancel.Cancel();
            if (fault == "changed-between-attempts") File.WriteAllText(receiptPath, "{}");
            if (fault != "transient" || calls < 3) throw new IOException("Injected publication denial");
            Directory.Move(source, destination);
        }
        using var noCombat = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Publication must not fight")).Activate();
        var task = TowerCompactBundle.PublishChunkAsync(path, 0, receipt, cancel.Token, Move);
        if (fault == "transient") { await task; Assert.Equal(3, calls); }
        else
        {
            await Assert.ThrowsAnyAsync<Exception>(() => task);
            Assert.Equal(fault == "persistent" ? 4 : 1, calls);
            Assert.True(Directory.Exists(Path.Combine(path, "chunks/.pending-000000")));
        }
        Assert.Equal(attemptsHash, HarnessJson.FileHash(Path.Combine(path, "bulk-attempts.jsonl")));
    }

    [Theory]
    [InlineData("digest")]
    [InlineData("receipt")]
    [InlineData("wrong-seed-rehashed")]
    [InlineData("missing-record-rehashed")]
    [InlineData("journal")]
    [InlineData("torn-journal")]
    [InlineData("scope")]
    [InlineData("destination")]
    [InlineData("extra-pending")]
    [InlineData("extra-file")]
    public async Task Explicit_recovery_rejects_invalid_evidence_before_renaming(string defect)
    {
        using var temp = new DiscoveryTemp(); var path = await Pending(temp);
        var data = Path.Combine(path, "chunks/.pending-000000/records.json.gz");
        var receiptPath = Path.Combine(path, "chunks/.pending-000000/receipt.json");
        if (defect == "digest") File.AppendAllText(data, "broken");
        else if (defect == "receipt") File.WriteAllText(receiptPath, "{}");
        else if (defect.EndsWith("-rehashed", StringComparison.Ordinal))
        {
            TowerCompactRecord[] rows;
            using (var file = File.OpenRead(data)) using (var gzip = new GZipStream(file, CompressionMode.Decompress))
                rows = JsonSerializer.Deserialize<TowerCompactRecord[]>(gzip, HarnessJson.Options)!;
            if (defect == "wrong-seed-rehashed") rows[0] = rows[0] with { Seed = rows[0].Seed + 10 };
            else rows = rows.Take(1).ToArray();
            using (var file = File.Create(data)) using (var gzip = new GZipStream(file, CompressionLevel.Fastest))
                JsonSerializer.Serialize(gzip, rows, HarnessJson.Options);
            File.WriteAllText(receiptPath, JsonSerializer.Serialize(HarnessJson.Read<TowerCompactChunk>(receiptPath)
                with { DataHash = HarnessJson.FileHash(data) }, HarnessJson.Options));
        }
        else if (defect == "journal") File.WriteAllText(Path.Combine(path, "bulk-attempts.jsonl"), "");
        else if (defect == "torn-journal") File.AppendAllText(Path.Combine(path, "bulk-attempts.jsonl"), "{");
        else if (defect == "scope") File.AppendAllText(Path.Combine(path, "bulk-scope.json"), " ");
        else if (defect == "destination") Directory.CreateDirectory(Path.Combine(path, "chunks/000000"));
        else if (defect == "extra-pending") Directory.CreateDirectory(Path.Combine(path, "chunks/.pending-000001"));
        else File.WriteAllText(Path.Combine(path, "chunks/.pending-000000/extra.json"), "{}");
        var before = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToDictionary(p => p, HarnessJson.FileHash);
        using var noCombat = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Recovery must not fight")).Activate();
        await Assert.ThrowsAnyAsync<Exception>(() => TowerCompactBundle.RecoverPublicationAsync(path));
        Assert.Equal(before, Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToDictionary(p => p, HarnessJson.FileHash));
    }
}
