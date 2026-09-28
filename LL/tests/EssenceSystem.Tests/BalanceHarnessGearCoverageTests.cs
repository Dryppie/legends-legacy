using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessGearCoverageTests
{
    private const string Version = "tower-gear-encounter-coverage-v1";
    private const int Fights = 1344;
    private sealed record CoverageCase(int Floor, string SourceId, TowerSearchBudget Budget, TowerScenario Scenario);
    private sealed record Request(string Version, string Output, string Source, string SourcePin, string Runtime,
        string Profiles, IReadOnlyList<CoverageCase> Cases, IReadOnlyList<int> Seeds,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> AssemblyHashes);
    private sealed record Cell(int Floor, string Profile, TowerScenario Scenario);
    private sealed record Row(int Floor, string Profile, int Wins, int Samples, decimal MeanGuardianHealth,
        int GainedWins, int LostWins, string Classification);
    private sealed class CoverageFactAttribute : FactAttribute
    {
        public CoverageFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_GEAR_COVERAGE")))
                Skip = "Set LL_GEAR_COVERAGE for the fixed 1,344-fight diagnostic using historical seeds.";
        }
    }

    [CoverageFact]
    public async Task Fixed_encounters_compare_profiles_with_current_content_and_historical_seeds()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_GEAR_COVERAGE")!);
        Assert.Equal(Version, q.Version); Assert.False(Path.Exists(q.Output));
        foreach (var path in q.InputHashes.Keys.Append(q.Output).Append(q.Source).Append(q.Runtime)) TowerProposalStudy.Unlinked(path);
        void VerifyInputs() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        VerifyInputs();
        Assert.Equal(q.SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        Assert.Equal(files["scope.json"], HarnessJson.FileHash(Path.Combine(q.Source, "scope.json")));
        Assert.Equal(files["variants.json"], HarnessJson.FileHash(Path.Combine(q.Source, "variants.json")));
        var originalScope = HarnessJson.Read<LoadoutScope>(Path.Combine(q.Source, "scope.json"));
        var scope = originalScope with { Algorithm = Version, Execution = ExecutionIdentity.Current() };
        Assert.Equal(new TowerBalanceSelection(18, 4, "healing-v1"), scope.Settings.Balance);
        Assert.Equal(HarnessJson.Hash(q.AssemblyHashes), HarnessJson.Hash(scope.Execution.AssemblyHashes));
        Assert.Equal(new[] { 3, 7, 8, 10, 13, 15 }, q.Cases.Select(c => c.Floor));
        Assert.Equal(32, q.Seeds.Count); Assert.Equal(32, q.Seeds.Distinct().Count());
        var profiles = TowerGearProfiles.Read(q.Profiles).Profiles;
        Assert.Equal(new[] { "precision", "ability-haste", "restorer-specialization", "armor-and-health", "resistance-and-health", "health-and-regeneration" }, profiles.Select(p => p.Id));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = deadline.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output);
        Directory.CreateDirectory(q.Output);
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        var attempts = 0; var completed = 0; var success = false;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % 32 == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 1073741824); }
        try
        {
            Save("request.json", q); Save("scope.json", scope); Save("profiles.json", profiles);
            Save("protocol.json", new { version = Version, samples = 32, cases = 6, profiles = 7, maximumFights = Fights,
                maximumSeconds = 840, maximumBytes = 1073741824, newSeeds = 0, confirmedTeams = 0,
                rule = "Only the preselected armor-and-health profile determines follow-up eligibility: 4 through 28 wins of 32. All rows are descriptive; no promotion or confirmation." });
            var root = Path.Combine(q.Output, "content");
            Assert.Equal(HarnessJson.Hash(scope.ContentHashes), HarnessJson.Hash(TowerBundle.CopyContent(Path.Combine(q.Source, "content"), root, token)));
            TowerBundle.WriteSettings(Path.Combine(root, "appsettings.json"), scope.Settings);
            foreach (var pin in scope.Execution.AssemblyHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var path in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, path);
                if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(path, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var content = OfflineContent.ForTower(root, scope.Settings); var runner = new TowerBattleRunner(root, content);
            var cells = new List<Cell>();
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Coverage preparation cannot fight.")).Activate())
            {
                foreach (var c in q.Cases)
                {
                    Assert.Equal(c.Floor, c.Scenario.FloorNumber); Assert.Empty(c.Scenario.Seeds);
                    TowerBossDiscovery.ValidateEquipment(c.Scenario.Party, c.Budget, c.Scenario.Party.Count);
                    cells.Add(new(c.Floor, "baseline", c.Scenario with { Seeds = q.Seeds }));
                    cells.AddRange(profiles.Select(p => new Cell(c.Floor, p.Id, TowerGearProfiles.Apply(c.Scenario, p, content) with { Seeds = q.Seeds })));
                }
                Assert.Equal(42, cells.Count);
                foreach (var cell in cells)
                    _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario, q.Seeds[0], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks), token);
                var known = HarnessJson.Read<JsonElement>(Path.Combine(q.Source, "variants.json")).EnumerateArray()
                    .Single(v => v.GetProperty("id").GetString() == "armor-and-health").GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)!;
                Assert.Equal(HarnessJson.Hash(known), HarnessJson.Hash(cells.Single(c => c.Floor == 15 && c.Profile == "armor-and-health").Scenario with { Seeds = [] }));
            }
            Save("cells.json", cells); Save("preflight.json", new { status = "PreparedNoFights", cells = 42, knownFloor15VariantMatched = true });
            VerifyInputs(); Check();
            var study = Path.Combine(q.Output, "study"); var archive = F.Archive(study, scope, Fights, q.Output, token);
            var rows = new List<Row>(); var outcomes = new Dictionary<int, bool[]>();
            foreach (var cell in cells)
            {
                var reports = new List<TowerBattleReport>();
                foreach (var seed in q.Seeds)
                {
                    Check(); Assert.True(++attempts <= Fights);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                    var trial = await archive.EvaluateAsync(Version, $"{cell.Floor}/{cell.Profile}", cell.Scenario, seed, token);
                    completed++; reports.Add(trial.Report);
                }
                var wins = reports.Select(r => r.Succeeded).ToArray();
                if (cell.Profile == "baseline") outcomes.Add(cell.Floor, wins);
                var paired = wins.Zip(outcomes[cell.Floor]).ToArray();
                var count = wins.Count(w => w);
                var row = new Row(cell.Floor, cell.Profile, count, 32, reports.Average(r => r.GuardianHealthRemainingPercent),
                    paired.Count(p => p.First && !p.Second), paired.Count(p => !p.First && p.Second),
                    count < 4 ? "LowWinReference" : count > 28 ? "CeilingReference" : "FollowUpCandidate");
                rows.Add(row); Console.WriteLine(JsonSerializer.Serialize(row, HarnessJson.Options));
            }
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(Fights, trials.Count);
            foreach (var (trial, ordinal) in trials.Select((t, i) => (t, i)))
            {
                Check(); var cell = cells[ordinal / 32]; Assert.Equal(q.Seeds[ordinal % 32], trial.Seed);
                Assert.Equal($"{cell.Floor}/{cell.Profile}", trial.Stage); Assert.Equal(HarnessJson.Hash(cell.Scenario), trial.Recipe);
                var input = runner.CreateInput(cell.Scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(scope, Version, input), trial.CacheKey);
                var report = TowerLoadoutArchive.ReadBattle(study, trial.Id, scope.ReportStorage);
                Assert.Equal(trial.Seed, report.Battle.Seed); Assert.Equal(cell.Scenario.Id, report.Battle.ScenarioId);
                Assert.Equal(report.Battle.Summary.ContentOutcome == BattleOutcome.Victory, report.Succeeded);
            }
            VerifyInputs();
            foreach (var pin in scope.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            Save("result.json", new { status = "CoverageComplete", fights = completed, rows,
                followUpFloors = rows.Where(r => r.Profile == "armor-and-health" && r.Classification == "FollowUpCandidate").Select(r => r.Floor),
                newSeeds = 0, confirmedTeams = 0, searchRuns = 0, retries = 0, seconds = watch.Elapsed.TotalSeconds });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
