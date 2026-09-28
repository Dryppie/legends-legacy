using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessGearReferenceTests
{
    private const string Version = "tower-floor13-geared-reference-screen-v1";
    private const int Fights = 192;
    private sealed record Request(string Version, string Output, string Source, string SourcePin, string Runtime,
        string Handoff, string Plan, string Profiles, IReadOnlyDictionary<string, string> InputHashes);
    private sealed record Cell(string Form, int Reference, string SourceId, TowerScenario Scenario);
    private sealed record Row(string Form, int Reference, string SourceId, int Wins, int Samples, decimal MeanGuardianHealth);
    private sealed class ScreenFactAttribute : FactAttribute
    {
        public ScreenFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_GEAR_REFERENCES")))
                Skip = "Set LL_GEAR_REFERENCES for the fixed 192-fight floor-13 reference screen.";
        }
    }

    private static int? Benchmark(IReadOnlyList<Row> rows)
    {
        var schedule = new[] { "retained", "projected" }.SelectMany(f => Enumerable.Range(1, 3).Select(i => (f, i)));
        if (!rows.Select(r => (r.Form, r.Reference)).SequenceEqual(schedule) || rows.Any(r => r.Samples != 32 || r.Wins is < 0 or > 32))
            throw new InvalidDataException("The complete six-cell screen is required.");
        if (rows.Max(r => r.Wins) > 28) return null;
        var best = rows.Where(r => r.Form == "projected").OrderByDescending(r => r.Wins).ThenBy(r => r.Reference).First();
        return best.Wins >= 4 ? best.Reference : null;
    }

    [Theory]
    [InlineData(28, 28, 1)] [InlineData(29, 28, null)] [InlineData(28, 29, null)]
    [InlineData(22, 3, null)] [InlineData(22, 4, 1)]
    public void Retained_ceiling_blocks_search_and_projected_benchmark_must_be_viable(int retained, int projected, int? expected)
    {
        var rows = new[] { "retained", "projected" }.SelectMany(f => Enumerable.Range(1, 3)
            .Select(i => new Row(f, i, "source-" + i, f == "retained" ? retained : projected, 32, 0))).ToArray();
        Assert.Equal(expected, Benchmark(rows));
        Assert.Throws<InvalidDataException>(() => Benchmark(rows.Skip(1).ToArray()));
    }

    [ScreenFact]
    public async Task Retained_and_projected_geared_references_are_screened_before_any_search()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_GEAR_REFERENCES")!);
        Assert.Equal(Version, q.Version); Assert.False(Path.Exists(q.Output));
        foreach (var path in q.InputHashes.Keys.Append(q.Output).Append(q.Source).Append(q.Runtime)) TowerProposalStudy.Unlinked(path);
        void VerifyInputs() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        VerifyInputs(); Assert.Equal(q.SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name) { var path = Path.Combine(q.Source, name); Assert.Equal(files[name], HarnessJson.FileHash(path)); return HarnessJson.Read<T>(path); }
        Assert.Equal(1, Source<JsonElement>("result.json").GetProperty("selected").GetProperty("variant").GetInt32());
        var captured = Source<LoadoutScope>("variant-01/scope.json") with { Algorithm = Version };
        Assert.Equal(HarnessJson.Hash(captured.Execution), HarnessJson.Hash(ExecutionIdentity.Current()));
        Assert.Equal(new TowerBalanceSelection(18, 4, "healing-v1"), captured.Settings.Balance);
        var seeds = Source<JsonElement>("proposal.json").GetProperty("seeds").Deserialize<int[]>(HarnessJson.Options)!;
        Assert.Equal(32, seeds.Length); Assert.Equal(32, seeds.Distinct().Count());
        var handoff = HarnessJson.Read<JsonElement>(q.Handoff).GetProperty("cases").EnumerateArray()
            .Single(c => c.GetProperty("case").GetProperty("floor").GetInt32() == 13);
        var definition = handoff.GetProperty("case");
        var budget = definition.GetProperty("budget").Deserialize<TowerSearchBudget>(HarnessJson.Options)!;
        var sourceIds = definition.GetProperty("referenceIds").Deserialize<string[]>(HarnessJson.Options)!;
        var originals = handoff.GetProperty("scenarios").Deserialize<TowerScenario[]>(HarnessJson.Options)!;
        Assert.Equal(3, originals.Length);
        var source = HarnessJson.Read<TowerProposalRacingPlan>(q.Plan);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = deadline.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output); Directory.CreateDirectory(q.Output);
        var attempts = 0; var completed = 0; var success = false; var watch = System.Diagnostics.Stopwatch.StartNew();
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % 32 == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 1073741824); }
        try
        {
            Save("request.json", q); Save("scope.json", captured);
            Save("protocol.json", new { version = Version, maximumFights = Fights, maximumSeconds = 840, maximumBytes = 1073741824,
                samples = 32, newSeeds = 0, retries = 0, forms = new[] { "retained", "projected" }, references = 3,
                rule = "Reject if any retained/projected reference exceeds 28/32. Otherwise select highest-win projected reference, tie lowest index; it must reach 4/32." });
            var root = Path.Combine(q.Output, "content");
            Assert.Equal(HarnessJson.Hash(captured.ContentHashes), HarnessJson.Hash(TowerBundle.CopyContent(Path.Combine(q.Source, "variant-01/content"), root, token)));
            TowerBundle.WriteSettings(Path.Combine(root, "appsettings.json"), captured.Settings);
            foreach (var pin in captured.Execution.AssemblyHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var path in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, path);
                if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(path, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var content = OfflineContent.ForTower(root, captured.Settings); var runner = new TowerBattleRunner(root, content);
            var profile = TowerGearProfiles.Select(TowerGearProfiles.Read(q.Profiles), "resistance-and-health");
            var cells = new List<Cell>();
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Reference preparation cannot fight.")).Activate())
            {
                var retained = originals.Select(s => TowerGearProfiles.Apply(s with { Seeds = [] }, profile, content)).ToArray();
                var known = Source<JsonElement>("controls.json").EnumerateArray().Single(c => c.GetProperty("profile").GetString() == profile.Id)
                    .GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)!;
                Assert.Equal(HarnessJson.Hash(known with { Seeds = [] }), HarnessJson.Hash(retained[0]));
                var inventory = TowerBossInventory.CreateForTower(root, captured.Settings); Save("inventory.json", inventory);
                var plan = F.Project(source, retained, inventory, captured, Enumerable.Range(1000, 109).ToArray(), [], [], budget, 10, 1);
                Save("preview-plan.json", plan);
                var projected = plan.Racing.Scope.References.Select(r => r.Scenario).ToArray();
                // Search projection of these exported references must be stable.
                var again = F.Project(source, projected, inventory, captured, Enumerable.Range(1000, 109).ToArray(), [], [], budget, 10, 1);
                Assert.Equal(HarnessJson.Hash(projected), HarnessJson.Hash(again.Racing.Scope.References.Select(r => r.Scenario)));
                for (var i = 0; i < 3; i++)
                {
                    Assert.Equal(F.Composition(retained[i]).Id, F.Composition(projected[i]).Id);
                    Assert.Equal(TowerBossDiscovery.EquipmentBudgetHash(retained[i].Party), TowerBossDiscovery.EquipmentBudgetHash(projected[i].Party));
                    cells.Add(new("retained", i + 1, sourceIds[i], retained[i] with { Seeds = seeds }));
                }
                for (var i = 0; i < 3; i++) cells.Add(new("projected", i + 1, sourceIds[i], projected[i] with { Seeds = seeds }));
                foreach (var cell in cells)
                foreach (var seed in seeds)
                    _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario, seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks), token);
                Directory.CreateDirectory(Path.Combine(q.Output, "scenarios"));
                for (var i = 0; i < 3; i++) Save($"scenarios/floor-13-reference-{i + 1}.json", projected[i]);
                Save("follow-up-inputs.json", new { version = Version, cases = new[] { new { @case = definition, scenarios = projected } } });
                var sourceTrialsPath = Path.Combine(q.Source, "variant-01/study/trials.jsonl");
                Assert.Equal(files["variant-01/study/trials.jsonl"], HarnessJson.FileHash(sourceTrialsPath));
                var matches = File.ReadLines(sourceTrialsPath).Select(l => JsonSerializer.Deserialize<LoadoutTrial>(l, HarnessJson.Options)!)
                    .Where(t => t.Stage == profile.Id).ToArray();
                Assert.Equal(32, matches.Length);
                foreach (var trial in matches) Assert.Equal(trial.InputHash,
                    HarnessJson.Hash(runner.CreateInput(cells[0].Scenario, trial.Seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks)));
            }
            Save("cells.json", cells); Save("preflight.json", new { status = "PreparedNoFights", inputs = 192, historicalInputsMatched = 32, projectionStable = true });
            Save("freeze.json", new { version = Version, balance = captured.Settings.Balance, seeds, captured = F.Inventory(q.Output) });
            VerifyInputs(); Check();
            var study = Path.Combine(q.Output, "study"); var archive = F.Archive(study, captured, Fights, q.Output, token);
            var rows = new List<Row>();
            foreach (var cell in cells)
            {
                var reports = new List<TowerBattleReport>();
                foreach (var seed in seeds)
                {
                    Check(); Assert.True(++attempts <= Fights);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                    var trial = await archive.EvaluateAsync(Version + "/" + cell.Form, $"{cell.Form}/{cell.Reference}", cell.Scenario, seed, token);
                    completed++; reports.Add(trial.Report);
                }
                rows.Add(new(cell.Form, cell.Reference, cell.SourceId, reports.Count(r => r.Succeeded), 32, reports.Average(r => r.GuardianHealthRemainingPercent)));
            }
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(Fights, trials.Count);
            for (var i = 0; i < trials.Count; i++)
            {
                Check(); var trial = trials[i]; var cell = cells[i / 32]; Assert.Equal(seeds[i % 32], trial.Seed);
                Assert.Equal($"{cell.Form}/{cell.Reference}", trial.Stage); Assert.Equal(HarnessJson.Hash(cell.Scenario), trial.Recipe);
                var input = runner.CreateInput(cell.Scenario, trial.Seed, captured.Settings.Threat, captured.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(captured, Version + "/" + cell.Form, input), trial.CacheKey);
                var report = TowerLoadoutArchive.ReadBattle(study, trial.Id, captured.ReportStorage);
                Assert.Equal(trial.Seed, report.Battle.Seed); Assert.Equal(cell.Scenario.Id, report.Battle.ScenarioId);
                Assert.Equal(report.Battle.Summary.ContentOutcome == BattleOutcome.Victory, report.Succeeded);
            }
            VerifyInputs(); var benchmark = Benchmark(rows);
            foreach (var pin in captured.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            Save("result.json", new { status = "ScreenComplete", version = Version, freezeSha256 = HarnessJson.FileHash(Path.Combine(q.Output, "freeze.json")),
                fights = completed, execution = captured.Execution, rows, benchmarkReference = benchmark,
                eligible = benchmark.HasValue, newSeeds = 0, retries = 0, searchRuns = 0, seconds = watch.Elapsed.TotalSeconds });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
