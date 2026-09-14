using System.Diagnostics;

namespace BalanceHarness;

public sealed record TowerDiscoveryParityDefinition(int SchemaVersion, int MaximumFights, int CombatRetries,
    int MaximumSeconds, long MaximumBytes, int[] FixtureScales, int SamplesPerScale,
    string ReferenceDirectory, string ContentRoot, TowerPerformanceDefinition Combat,
    IReadOnlyDictionary<string, string> FrozenFiles);

internal sealed record TowerStorageParityFixture(int ExistingArchives, int Samples, long Bytes,
    double PreparationSeconds, double InitializationSeconds, IReadOnlyList<double> SampleMilliseconds,
    double FinalizationSeconds, long SampleFileVisits, long SampleDirectoryVisits, IReadOnlyList<TowerStageTiming> Timings);

/// <summary>Execute once against an already verified reference. Allocates no seeds and never launches reference combat.</summary>
public static class TowerDiscoveryParity
{
    internal static void ValidateBudget(TowerDiscoveryParityDefinition d)
    {
        if (d.SchemaVersion != 1 || d.MaximumFights != 34 || d.CombatRetries != 0
            || d.MaximumSeconds is < 1 or > 900 || d.MaximumBytes is < 1048576 or > 268435456
            || !d.FixtureScales.SequenceEqual(new[] { 0, 9216 }) || d.SamplesPerScale != 16
            || TowerPerformanceBenchmark.Validate(d.Combat) != d.MaximumFights || d.Combat.MaxBattles != d.MaximumFights
            || d.Combat.MaxOutputBytes > d.MaximumBytes || d.Combat.MaxSeconds > d.MaximumSeconds
            || !d.Combat.WorkerCounts.SequenceEqual(new[] { 1 }) || d.Combat.Repetitions != 2
            || d.Combat.Cases.Count != 2 || d.Combat.Cases.Any(c => c.Scenario.Seeds.Count != 8))
            throw new InvalidDataException("Parity closure requires 34 fights, no retries, two eight-seed cases, two passes/replays, and fixed zero-combat fixtures.");
    }

    private static void FrozenInputs(TowerDiscoveryParityDefinition d, CancellationToken token)
    {
        foreach (var (path, hash) in d.FrozenFiles)
        {
            token.ThrowIfCancellationRequested();
            if (HarnessJson.FileHash(path) != hash) throw new InvalidDataException("Frozen parity input changed: " + path);
        }
        // The snapshot must bind every reused reference artifact and every actual runtime assembly.
        foreach (var path in TowerBulkCampaign.Paths(d.ReferenceDirectory).Concat(TowerBulkCampaign.Paths(d.ContentRoot))
            .Concat(Directory.GetFiles(AppContext.BaseDirectory, "*.dll")))
            if (!d.FrozenFiles.ContainsKey(path)) throw new InvalidDataException("Unbound parity input: " + path);
    }

    public static string Check(string definitionPath, CancellationToken token = default)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Parity preflight cannot fight.")).Activate();
        var d = HarnessJson.Read<TowerDiscoveryParityDefinition>(definitionPath); ValidateBudget(d); FrozenInputs(d, token);
        var reference = TowerPerformanceComparison.Read(d.ReferenceDirectory, token);
        var execution = ExecutionIdentity.Current();
        if (HarnessJson.Hash(d.Combat) != HarnessJson.Hash(reference.Definition)
            || reference.Scope.ArchiveFormat != TowerCompactBundle.Format || reference.Scope.ExecutionMode != TowerPreparedBattle.Mode
            || reference.Scope.Execution.Runtime != execution.Runtime || reference.Scope.Execution.OperatingSystem != execution.OperatingSystem
            || reference.Scope.Execution.Architecture != execution.Architecture
            || !reference.Scope.Execution.AssemblyHashes.Where(p => p.Key != "BalanceHarness").OrderBy(p => p.Key)
                .SequenceEqual(execution.AssemblyHashes.Where(p => p.Key != "BalanceHarness").OrderBy(p => p.Key))
            || HarnessJson.Hash(TowerBundle.ReadSettings(d.ContentRoot)) != HarnessJson.Hash(reference.Scope.Settings)
            || HarnessJson.Hash(TowerCompactBundle.ContentHashes(d.ContentRoot, token)) != HarnessJson.Hash(reference.Scope.ContentHashes))
            throw new InvalidDataException("Parity needs identical frozen gameplay, content, recipes, settings, seeds and prepared mode.");
        return HarnessJson.Hash(d);
    }

    public static async Task RunAsync(string definitionPath, string output, CancellationToken token = default, Action<string>? progress = null)
    {
        token.ThrowIfCancellationRequested();
        var d = HarnessJson.Read<TowerDiscoveryParityDefinition>(definitionPath); ValidateBudget(d);
        output = Path.GetFullPath(output);
        if (Path.Exists(output)) throw new IOException("Parity output already exists; no retry or resume is permitted.");
        using var lease = TowerCompactBundle.AcquireWriter(output);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(d.MaximumSeconds));
        var ct = timeout.Token; var clock = Stopwatch.StartNew(); var process = Process.GetCurrentProcess();
        var cpu = process.TotalProcessorTime; var allocated = GC.GetTotalAllocatedBytes();
        Directory.CreateDirectory(output); File.Copy(definitionPath, Path.Combine(output, "definition.json"));
        var stages = new List<TowerStorageParityFixture>(); var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fixture cannot fight."));
        var status = "Failed"; string? error = null;
        try
        {
            using (trace.Activate())
            {
                using (TowerPerformanceTrace.Measure("reference.preflight")) Check(definitionPath, ct);
                foreach (var scale in d.FixtureScales)
                {
                    var row = Fixture(Path.Combine(output, "fixture-" + scale), scale, d.SamplesPerScale, ct);
                    stages.Add(row); HarnessJson.WriteNew(Path.Combine(output, "fixture-" + scale + ".json"), row);
                    CheckSize(); progress?.Invoke($"Zero-combat nested fixture {scale}: {row.Samples} writes, exact final bytes/inventory and closed leases.");
                }
            }
            var reference = TowerPerformanceComparison.Read(d.ReferenceDirectory, ct);
            progress?.Invoke("Candidate only: 32 fixed trials and two detailed replays against retained reference evidence.");
            await TowerDiscoveryPerformance.Combat(d.Combat, d.ContentRoot, output, reference.Scope, reference.Report, ct, d.ReferenceDirectory);
            using (trace.Activate())
            using (TowerPerformanceTrace.Measure("final.inputs-and-storage")) { FrozenInputs(d, ct); CheckSize(); }
            status = "Exact";
        }
        catch (Exception e) { error = e.ToString(); throw; }
        finally
        {
            HarnessJson.WriteNew(Path.Combine(output, "parity.json"), new { status, error, seconds = clock.Elapsed.TotalSeconds,
                cpuSeconds = (process.TotalProcessorTime - cpu).TotalSeconds, allocatedBytes = GC.GetTotalAllocatedBytes() - allocated,
                peakWorkingSetBytes = process.PeakWorkingSet64, fixtures = stages, timings = trace.Snapshot(),
                referenceFightsExecuted = 0, scope = "Performance/parity repetitions only; zero fresh seeds and no balance inference." });
        }
        // This marker seals diagnostic evidence; it is not a balance study completion/adoption marker.
        var manifest = Path.Combine(output, "parity-files.json");
        var files = TowerBulkCampaign.Paths(output).ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash);
        HarnessJson.WriteNew(manifest + ".pending", files); CheckSize(); File.Move(manifest + ".pending", manifest);
        TowerBulkCampaign.VerifyFiles(output, "parity-files.json", true, ct);
        void CheckSize() { if (TowerBulkCampaign.StorageBytes(output, ct) > d.MaximumBytes) throw new InvalidDataException("Parity output cap exceeded."); }
    }

    internal static TowerStorageParityFixture Fixture(string root, int archived, int samples, CancellationToken token)
    {
        if (Path.Exists(root)) throw new IOException("Choose a new fixture directory.");
        Directory.CreateDirectory(root); var watch = Stopwatch.StartNew();
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fixture cannot fight."));
        using var active = trace.Activate();
        var parent = new TowerStorageAccountant(root, 67108864, ["neutral-journal.bin", "final.json", "final.json.pending"], token);
        var childPath = Path.Combine(root, "campaign"); parent.BeginDirectory(childPath, token);
        var sampleTimes = new List<double>(); TowerStorageAccountant child;
        double preparation, initialization;
        using (var campaignLease = TowerCompactBundle.AcquireWriter(childPath))
        {
            Directory.CreateDirectory(Path.Combine(childPath, "batches")); campaignLease.SetLength(3); campaignLease.Flush(true);
            using (TowerPerformanceTrace.Measure("fixture.prepare"))
                for (var i = 0; i < archived; i++) TowerDiscoveryPerformance.WriteShape(Path.Combine(childPath, "batches", "base-" + i.ToString("D6")), token);
            preparation = watch.Elapsed.TotalSeconds; watch.Restart();
            child = new(childPath, 67108864, ["neutral-journal.bin", "final.json", "final.json.pending"], token); parent.Attach(child);
            initialization = watch.Elapsed.TotalSeconds;
            for (var i = 0; i < samples; i++)
            {
                watch.Restart();
                using (TowerPerformanceTrace.Measure("fixture.candidate"))
                {
                    var batch = Path.Combine(childPath, "batches", "sample-" + i.ToString("D6")); child.BeginDirectory(batch, token);
                    using (var batchLease = TowerCompactBundle.AcquireWriter(batch))
                    {
                        batchLease.SetLength(2); batchLease.Flush(true);
                        TowerDiscoveryPerformance.WriteShape(batch, token);
                        using (var journal = new FileStream(Path.Combine(childPath, "neutral-journal.bin"), FileMode.OpenOrCreate)) journal.SetLength(8 * (i + 1));
                        using (var journal = new FileStream(Path.Combine(root, "neutral-journal.bin"), FileMode.OpenOrCreate)) journal.SetLength(2 * (i + 1));
                        var expected = TowerDiscoveryPerformance.FixtureArchiveBytes * (archived + i + 1) + 10 * (i + 1) + 5;
                        if (parent.Check(token) != expected) throw new InvalidDataException("Nested live lease/journal bytes were lost or double counted.");
                        child.Check(token);
                    }
                    child.SealDirectory(token); parent.Check(token);
                }
                sampleTimes.Add(watch.Elapsed.TotalMilliseconds);
            }
            watch.Restart();
            HarnessJson.WriteNew(Path.Combine(childPath, "final.json.pending"), new { archives = archived + samples });
            child.Check(token); child.Audit(token);
            File.Move(Path.Combine(childPath, "final.json.pending"), Path.Combine(childPath, "final.json"));
        }
        parent.SealDirectory(token); parent.Audit(token);
        HarnessJson.WriteNew(Path.Combine(root, "final.json.pending"), new { status = "Exact", freshFights = 0 });
        parent.Audit(token); File.Move(Path.Combine(root, "final.json.pending"), Path.Combine(root, "final.json"));
        var bytes = parent.Check(token);
        if (bytes != TowerBulkCampaign.StorageBytes(root, token) || TowerBulkCampaign.Paths(root).Any(p => p.EndsWith(".writer.lock", StringComparison.Ordinal)))
            throw new InvalidDataException("Final fixture accounting or lease cleanup differs.");
        var timings = trace.Snapshot();
        long Count(string suffix) => timings.Where(t => t.Path.StartsWith("fixture.candidate/", StringComparison.Ordinal) && t.Path.EndsWith(suffix, StringComparison.Ordinal)).Sum(t => t.Calls);
        return new(archived, samples, bytes, preparation, initialization, sampleTimes, watch.Elapsed.TotalSeconds,
            Count("storage.files-visited"), Count("storage.directories-visited"), timings);
    }
}
