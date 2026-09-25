using System.Diagnostics;
using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

// Explicit engineering replay of an existing root, never a fresh scientific run.
[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityRuntimeTests
{
    private sealed class ProfileFactAttribute : FactAttribute
    {
        public ProfileFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_AFFINITY_PROFILE_SOURCE")))
                Skip = "Set LL_AFFINITY_PROFILE_SOURCE, LL_AFFINITY_PROFILE_PIN and LL_AFFINITY_PROFILE_OUTPUT for an engineering replay.";
        }
    }

    [ProfileFact]
    public async Task Profile_saved_supported_search_and_match_every_battle()
    {
        var source = Environment.GetEnvironmentVariable("LL_AFFINITY_PROFILE_SOURCE");
        Assert.False(string.IsNullOrWhiteSpace(source));
        var pin = Environment.GetEnvironmentVariable("LL_AFFINITY_PROFILE_PIN");
        var output = Environment.GetEnvironmentVariable("LL_AFFINITY_PROFILE_OUTPUT")
            ?? throw new InvalidDataException("An explicit new profiling output is required.");
        Assert.False(Path.Exists(output));
        Assert.False(Path.GetFullPath(output).StartsWith(Path.GetFullPath(source!) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(pin, HarnessJson.FileHash(Path.Combine(source, "files.json")));
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var token = deadline.Token;
        var savedTrials = TowerLoadoutArchive.Verify(source, token);
        Assert.Equal(528, savedTrials.Count);
        var original = HarnessJson.Read<TowerProposalRacingPlan>(Path.Combine(source, "racing/plan.json"));
        TowerAffinitySearch.Validate(original);
        var captured = HarnessJson.Read<LoadoutScope>(Path.Combine(source, "scope.json"));
        var execution = ExecutionIdentity.Current();
        // Rebind only the engineering execution identity; content, recipes and seeds stay fixed.
        var plan = original with { Racing = original.Racing with { Scope = original.Racing.Scope with {
            ExecutionHash = HarnessJson.Hash(execution) } } };
        Directory.CreateDirectory(output);
        // Same-process allocation/time comparison; no combat and no speed assertions.
        T LegacyCopy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, HarnessJson.Options), HarnessJson.Options)!;
        Assert.Equal(HarnessJson.Hash(LegacyCopy(original)), HarnessJson.Hash(TowerBatchRacing.Copy(original)));
        var copies = new List<object>();
        for (var round = 0; round < 4; round++)
            foreach (var compact in round % 2 == 0 ? new[] { false, true } : new[] { true, false })
            {
                var before = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
                for (var i = 0; i < 5; i++)
                    _ = compact ? TowerBatchRacing.Copy(original) : LegacyCopy(original);
                timer.Stop();
                copies.Add(new { round, compact, copies = 5, seconds = timer.Elapsed.TotalSeconds,
                    allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before });
            }
        HarnessJson.WriteNew(Path.Combine(output, "copy-comparison.json"), copies);
        for (var pass = 0; pass < 2; pass++)
        {
            var target = Path.Combine(output, "pass-" + pass);
            Directory.CreateDirectory(target);
            var hashes = TowerBundle.CopyContent(Path.Combine(source, "content"), Path.Combine(target, "content"), token);
            Assert.Equal(HarnessJson.Hash(captured.ContentHashes), HarnessJson.Hash(hashes));
            var scope = captured with { Algorithm = TowerProposalRacingNative.ArchiveAlgorithm(plan), Execution = execution };
            HarnessJson.WriteNew(Path.Combine(target, "scope.json"), scope);
            Directory.CreateDirectory(Path.Combine(target, "recipes"));
            Directory.CreateDirectory(Path.Combine(target, "battles"));
            var archive = new TowerLoadoutArchive(target, scope, 528);
            var attempts = 0; var completed = 0;
            var startedBattles = 0; var finishedBattles = 0;
            var trace = new TowerPerformanceTrace(done => { if (done) finishedBattles++; else startedBattles++; });
            TowerAffinitySearchSummary? summary = null;
            var clock = Stopwatch.StartNew();
            try
            {
                using (trace.Activate())
                    summary = await TowerAffinitySearch.RunAsync(plan, archive, 64 * 1048576, token.ThrowIfCancellationRequested,
                        token, done => {
                            if (done) completed++; else attempts++;
                            if (completed % 64 == 0 && done)
                                Assert.True(Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories)
                                    .Sum(p => new FileInfo(p).Length) < 256 * 1048576);
                        });
            }
            finally
            {
                clock.Stop();
                HarnessJson.WriteNew(Path.Combine(target, "timing.json"), new { seconds = clock.Elapsed.TotalSeconds,
                    attempts, completed, startedBattles, finishedBattles, summary, timings = trace.Snapshot() });
            }
            Assert.Equal((528, 528, 528, 528), (attempts, completed, startedBattles, finishedBattles));
            Assert.Equal(528, archive.Trials.Count);
            for (var i = 0; i < savedTrials.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var before = savedTrials[i]; var after = archive.Trials[i];
                Assert.Equal((before.Id, before.Stage, before.Recipe, before.Seed, before.InputHash),
                    (after.Id, after.Stage, after.Recipe, after.Seed, after.InputHash));
                Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(source, before.Id, captured.ReportStorage)),
                    HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(target, after.Id, scope.ReportStorage)));
            }
            Assert.Equal(pin, HarnessJson.FileHash(Path.Combine(source, "files.json")));
            HarnessJson.WriteNew(Path.Combine(target, "parity.json"), new { matchedBattles = 528, newSeeds = 0,
                sourceManifestSha256 = pin, execution, note = "Engineering replay; no new strength evidence. Setup and parity checks excluded from search timing." });
        }
    }
}
