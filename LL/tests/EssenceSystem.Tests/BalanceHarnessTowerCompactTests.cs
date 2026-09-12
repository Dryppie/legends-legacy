using System.IO.Compression;
using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCompactTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Fixtures => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));
    private static TowerPerformanceDefinition Performance => TowerContractJson.Read<TowerPerformanceDefinition>(Path.Combine(Fixtures, TowerPerformanceBenchmark.Fixture));
    private static TowerCompactDefinition Definition(int seeds = 2) => new(1, "compact-tests", seeds * 2, 3,
        Performance.Cases.Take(2).Select(c => new TowerCompactCase(c.Id, c.Scenario with { Seeds = c.Scenario.Seeds.Take(seeds).ToArray() })).ToArray());

    [Theory]
    [InlineData("budget")]
    [InlineData("chunk-zero")]
    [InlineData("chunk-large")]
    [InlineData("duplicate-case")]
    [InlineData("duplicate-seed")]
    [InlineData("path")]
    [InlineData("assumptions")]
    public void Invalid_definitions_are_rejected(string defect)
    {
        var d = Definition(); var c = d.Cases[0];
        d = defect switch
        {
            "budget" => d with { MaximumBattles = 3 },
            "chunk-zero" => d with { ChunkSize = 0 },
            "chunk-large" => d with { ChunkSize = 33 },
            "duplicate-case" => d with { Cases = [c, c] },
            "duplicate-seed" => d with { Cases = [c with { Scenario = c.Scenario with { Seeds = [1, 1] } }] },
            "path" => d with { Cases = [c with { Id = "../escape" }] },
            _ => d with { Cases = [c with { Scenario = c.Scenario with { Assumptions = [] } }] }
        };
        Assert.Throws<InvalidDataException>(() => TowerCompactBundle.Validate(d));
    }

    [Fact]
    public async Task Multi_case_chunks_preserve_full_reports_search_ranking_and_balance_inputs()
    {
        using var temp = new Temp(); var d = Definition(); var compact = Path.Combine(temp.Path, "compact");
        await TowerCompactBundle.CreateAsync(Root, d, compact, retainExecutable: true);
        var saved = TowerCompactBundle.ReadSaved(compact);
        Assert.True(File.Exists(Path.Combine(compact, "executable-files.json")));
        Assert.Equal(4, saved.Plan.PlannedBattles);
        Assert.Equal(2, Directory.GetDirectories(Path.Combine(compact, "chunks")).Length);
        var legacyRank = new List<BossMeasurement>(); var compactRank = new List<BossMeasurement>();
        foreach (var item in d.Cases)
        {
            var scenarioPath = Path.Combine(temp.Path, item.Id + ".json"); HarnessJson.WriteNew(scenarioPath, item.Scenario);
            var legacy = Path.Combine(temp.Path, item.Id);
            var original = await TowerBundle.CreateAsync(Root, scenarioPath, legacy);
            var trials = saved.Cases[item.Id];
            Assert.Equal(HarnessJson.Hash(original.Trials), HarnessJson.Hash(trials));
            Assert.Equal(HarnessJson.Hash(HarnessJson.Read<Dictionary<string, string>>(Path.Combine(legacy, "tower-results.json"))), saved.ResultDigests[item.Id]);
            var before = TowerBalanceRuns.Read(item.Id, legacy); var after = TowerBalanceRuns.Read(item.Id, compact, compactCaseId: item.Id);
            Assert.Equal(HarnessJson.Hash(before with { ArtifactHash = "" }), HarnessJson.Hash(after with { ArtifactHash = "" }));
            BossMeasurement Measure(IReadOnlyList<TowerTrial> values)
            {
                var reports = values.Select(t => t.Report).ToArray();
                var cell = TowerPartySelection.Cell("context", 5, reports, values.Select(t => t.Id).ToArray());
                var wins = reports.Where(r => r.Succeeded).ToArray();
                var duration = wins.Length == 0 ? double.MaxValue : wins.Average(r => r.Battle.Summary.DurationSeconds);
                return new(item.Id, TowerBossOptimization.Fitness([cell], [cell], new Dictionary<int, double> { [5] = 1 }, duration),
                    [cell], TowerBossSearch.Observe(reports, true));
            }
            legacyRank.Add(Measure(original.Trials)); compactRank.Add(Measure(trials));
            var replay = await TowerCompactBundle.ReplayAsync(compact, item.Id, "tower.0001", true);
            Assert.Equal(TowerBossStudy.ReplayHash(trials[0].Report), TowerBossStudy.ReplayHash(replay));
            Assert.NotEmpty(replay.Battle.EventLog!);
        }
        Assert.Equal(HarnessJson.Hash(TowerBossOptimization.Rank(legacyRank)), HarnessJson.Hash(TowerBossOptimization.Rank(compactRank)));
        Assert.Equal("Invalid", TowerBalanceRuns.Read("ambiguous", compact).Status);
        Assert.Equal("Invalid", TowerBalanceRuns.Read("missing", compact, compactCaseId: "missing").Status);
        Assert.False(File.Exists(Path.Combine(compact, "scorecard.json")));
        Assert.False(File.Exists(Path.Combine(compact, "tower-input.json")));
    }

    [Fact]
    public async Task Equal_recipes_and_preparation_are_shared_without_merging_declared_trials()
    {
        using var temp = new Temp(); var d = Definition(); var first = d.Cases[0];
        d = d with { Cases = [first, first with { Id = "second" }] };
        var output = Path.Combine(temp.Path, "run"); await TowerCompactBundle.CreateAsync(Root, d, output);
        var saved = TowerCompactBundle.ReadSaved(output);
        Assert.Single(Directory.GetFiles(Path.Combine(output, "recipes")));
        Assert.Single(Directory.GetFiles(Path.Combine(output, "inputs")));
        Assert.Single(Directory.GetFiles(Path.Combine(output, "prepared")));
        Assert.Equal(4, saved.Cases.Values.Sum(v => v.Count));
        Assert.Equal(saved.ResultDigests[first.Id], saved.ResultDigests["second"]);
    }

    [Theory]
    [InlineData("chunk")]
    [InlineData("receipt")]
    [InlineData("recipe")]
    [InlineData("input")]
    [InlineData("prepared")]
    [InlineData("scope")]
    [InlineData("missing-chunk")]
    [InlineData("missing-receipt")]
    [InlineData("missing-manifest")]
    [InlineData("extra")]
    public async Task Modified_or_missing_evidence_is_rejected(string defect)
    {
        using var temp = new Temp(); var output = Path.Combine(temp.Path, "run");
        await TowerCompactBundle.CreateAsync(Root, Definition(1), output);
        var path = defect switch
        {
            "chunk" or "missing-chunk" => Path.Combine(output, "chunks/000000/records.json.gz"),
            "receipt" or "missing-receipt" => Path.Combine(output, "chunks/000000/receipt.json"),
            "recipe" => Directory.GetFiles(Path.Combine(output, "recipes"))[0],
            "input" => Directory.GetFiles(Path.Combine(output, "inputs"))[0],
            "prepared" => Directory.GetFiles(Path.Combine(output, "prepared"))[0],
            "scope" => Path.Combine(output, "bulk-scope.json"),
            "missing-manifest" => Path.Combine(output, TowerCompactBundle.ManifestFile),
            _ => Path.Combine(output, "extra.json")
        };
        if (defect.StartsWith("missing", StringComparison.Ordinal)) File.Delete(path); else File.AppendAllText(path, " ");
        Assert.Equal("Invalid", TowerBalanceRuns.Read("case", output, compactCaseId: "weak").Status);
    }

    [Theory]
    [InlineData("order")]
    [InlineData("seed")]
    [InlineData("case")]
    [InlineData("count")]
    [InlineData("summary")]
    [InlineData("outcome")]
    public async Task Rehashed_chunk_still_has_to_match_schedule_and_original_report(string defect)
    {
        using var temp = new Temp(); var output = Path.Combine(temp.Path, "run");
        await TowerCompactBundle.CreateAsync(Root, Definition(1), output);
        var trustedHash = HarnessJson.FileHash(Path.Combine(output, TowerCompactBundle.ManifestFile));
        var path = Path.Combine(output, "chunks/000000/records.json.gz");
        TowerCompactRecord[] rows;
        using (var file = File.OpenRead(path)) using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            rows = JsonSerializer.Deserialize<TowerCompactRecord[]>(gzip, HarnessJson.Options)!;
        rows[0] = defect switch
        {
            "order" => rows[0] with { Index = 1 },
            "seed" => rows[0] with { Seed = 123456 },
            "case" => rows[0] with { CaseId = "different" },
            "summary" => rows[0] with { Summary = rows[0].Summary with { Statistics = [] } },
            "outcome" => rows[0] with { Succeeded = !rows[0].Succeeded },
            _ => rows[0]
        };
        if (defect == "count") rows = [rows[0]];
        using (var file = File.Create(path)) using (var gzip = new GZipStream(file, CompressionLevel.Fastest)) JsonSerializer.Serialize(gzip, rows, HarnessJson.Options);
        var receiptPath = Path.Combine(output, "chunks/000000/receipt.json");
        var receipt = HarnessJson.Read<TowerCompactChunk>(receiptPath);
        File.WriteAllText(receiptPath, JsonSerializer.Serialize(receipt with { DataHash = HarnessJson.FileHash(path) }, HarnessJson.Options));
        Reseal(output);
        Assert.Throws<InvalidDataException>(() => TowerCompactBundle.ReadSaved(output));
        Assert.Throws<InvalidDataException>(() => TowerCompactBundle.ReadSaved(output, expectedManifestHash: trustedHash));
    }

    [Fact]
    public async Task Cancellation_preserves_committed_chunks_but_cannot_be_accepted()
    {
        using var temp = new Temp(); using var cancel = new CancellationTokenSource();
        var d = Definition() with { ChunkSize = 1 }; var output = Path.Combine(temp.Path, "run");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TowerCompactBundle.CreateAsync(Root, d, output, cancel.Token, _ => cancel.Cancel()));
        Assert.True(File.Exists(Path.Combine(output, "chunks/000000/receipt.json")));
        Assert.Single(Directory.GetDirectories(Path.Combine(output, "chunks")));
        Assert.False(File.Exists(Path.Combine(output, TowerCompactBundle.ManifestFile)));
        Assert.Equal("Invalid", TowerBalanceRuns.Read("partial", output, compactCaseId: "weak").Status);
        await Assert.ThrowsAsync<IOException>(() => TowerCompactBundle.CreateAsync(Root, d, output));
    }

    [Fact]
    public async Task Invalid_late_case_runs_no_combat_and_unsafe_manifest_paths_are_rejected()
    {
        using var temp = new Temp(); var d = Definition(1); var started = 0;
        var bad = d with { Cases = [d.Cases[0], d.Cases[1] with { Scenario = d.Cases[1].Scenario with { FloorNumber = 999 } }] };
        var trace = new TowerPerformanceTrace(_ => started++);
        using (trace.Activate()) await Assert.ThrowsAsync<InvalidDataException>(() => TowerCompactBundle.CreateAsync(Root, bad, Path.Combine(temp.Path, "bad")));
        Assert.Equal(0, started);
        var output = Path.Combine(temp.Path, "valid"); await TowerCompactBundle.CreateAsync(Root, d, output);
        var path = Path.Combine(output, TowerCompactBundle.ManifestFile); var manifest = HarnessJson.Read<TowerCompactManifest>(path);
        File.WriteAllText(path, JsonSerializer.Serialize(manifest with { Files = new Dictionary<string, string> { ["../outside.json"] = new('a', 64) } }, HarnessJson.Options));
        Assert.Throws<InvalidDataException>(() => TowerCompactBundle.ReadSaved(output));
    }

    [Fact]
    public async Task Compact_performance_mode_accounts_for_all_repeats_and_replays()
    {
        using var temp = new Temp(); var d = Performance;
        d = d with { Cases = d.Cases.Take(2).Select(c => c with { Scenario = c.Scenario with { Seeds = [c.Scenario.Seeds[0]] } }).ToArray(),
            WorkerCounts = [1, 2], Repetitions = 2, MaxBattles = 12 };
        var path = Path.Combine(temp.Path, "definition.json"); HarnessJson.WriteNew(path, d);
        var output = Path.Combine(temp.Path, "run");
        var result = await TowerPerformanceBenchmark.RunAsync(Root, path, output, archiveFormat: TowerCompactBundle.Format);
        Assert.True(result.Status == "Complete", result.Error);
        Assert.Equal(12, result.CompletedBattles); Assert.Equal(12, result.StartedBattles);
        Assert.Equal(TowerCompactBundle.Format, result.ArchiveFormat);
        Assert.All(result.Workers, w => Assert.Equal(2, w.CompletedReplays));
        var legacy = Path.Combine(temp.Path, "legacy");
        Assert.Equal("Complete", (await TowerPerformanceBenchmark.RunAsync(Root, path, legacy)).Status);
        var comparison = TowerPerformanceComparison.Compare(legacy, output, Path.Combine(temp.Path, "comparison"));
        Assert.Equal("Compared", comparison.Status); Assert.Equal(8, comparison.RepeatedTrialPairs);
        File.AppendAllText(Path.Combine(output, "workers-1/pass-00/weak/chunks/000000/records.json.gz"), " ");
        Assert.Throws<InvalidDataException>(() => TowerPerformanceComparison.Compare(legacy, output, Path.Combine(temp.Path, "bad-comparison")));
    }

    [Fact]
    public async Task Balance_sources_share_one_verification_but_never_cache_across_evaluations()
    {
        using var temp = new Temp(); var output = Path.Combine(temp.Path, "run"); var d = Definition(1);
        await TowerCompactBundle.CreateAsync(Root, d, output);
        TowerBalanceRunSource[] sources = [new("first", output, d.Cases[0].Id), new("bad-selection", output, "missing"),
            new("last", Path.Combine(output, "."), d.Cases[1].Id)];
        var trace = new TowerPerformanceTrace(); IReadOnlyList<TowerBalanceEvidence> evidence;
        using (trace.Activate()) evidence = TowerBalanceRuns.ReadSources(sources);
        Assert.Equal(new[] { "first", "bad-selection", "last" }, evidence.Select(e => e.CellId));
        Assert.Equal(new[] { "Complete", "Invalid", "Complete" }, evidence.Select(e => e.Status));
        Assert.Equal(1, trace.Snapshot().Single(s => s.Path == "compact.read-verify").Calls);
        File.AppendAllText(Path.Combine(output, "chunks/000000/records.json.gz"), " ");
        var second = new TowerPerformanceTrace();
        using (second.Activate()) evidence = TowerBalanceRuns.ReadSources(sources);
        Assert.All(evidence, e => Assert.Equal("Invalid", e.Status));
        Assert.Equal(1, second.Snapshot().Single(s => s.Path == "compact.read-verify").Calls);
    }

    private static void Reseal(string root)
    {
        var path = Path.Combine(root, TowerCompactBundle.ManifestFile);
        var manifest = HarnessJson.Read<TowerCompactManifest>(path);
        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Where(p => p != path)
            .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash);
        File.WriteAllText(path, JsonSerializer.Serialize(manifest with { Files = files }, HarnessJson.Options));
    }
    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-compact-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
