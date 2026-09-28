using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProgressionCheckpointTests
{
    private const string Version = "tower-progression-checkpoint-screen-v1";
    private const int Samples = 32, Cases = 19, Fights = Cases * 7 * Samples;
    private sealed record Checkpoint(string Id, string Purpose, string SourceId, string SourceContext,
        TowerSearchBudget Budget, TowerScenario Scenario);
    private sealed record Request(string Version, string Output, string Source, string SourcePin, string Runtime,
        string Profiles, IReadOnlyList<Checkpoint> Cases, IReadOnlyList<int> Seeds,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> AssemblyHashes);
    private sealed record Cell(string Case, string Purpose, int Floor, int EssenceSlots, string Profile, TowerScenario Scenario);
    private sealed record Row(string Case, string Purpose, int Floor, int EssenceSlots, string Profile,
        int Wins, int Draws, int Samples, decimal MeanGuardianHealth, double MeanDurationSeconds,
        int GainedWins, int LostWins, string Observation);
    private sealed class ScreenFactAttribute : FactAttribute
    {
        public ScreenFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_PROGRESSION_CHECKPOINTS")))
                Skip = "Set LL_PROGRESSION_CHECKPOINTS for the bounded 4,256-fight historical-seed screen.";
        }
    }

    [ScreenFact]
    public async Task Intended_checkpoints_and_lower_budget_controls_keep_every_frozen_team_and_gear_profile()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_PROGRESSION_CHECKPOINTS")!);
        Assert.Equal(Version, q.Version); Assert.False(Path.Exists(q.Output));
        foreach (var path in q.InputHashes.Keys.Append(q.Output).Append(q.Source).Append(q.Runtime)) TowerProposalStudy.Unlinked(path);
        void VerifyInputs() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        VerifyInputs(); Assert.Equal(q.SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name) { Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name))); return HarnessJson.Read<T>(Path.Combine(q.Source, name)); }
        var oldScope = Source<LoadoutScope>("scope.json");
        var scope = oldScope with { Algorithm = Version, Execution = ExecutionIdentity.Current() };
        Assert.Equal(HarnessJson.Hash(q.AssemblyHashes), HarnessJson.Hash(scope.Execution.AssemblyHashes));
        Assert.Equal(new TowerBalanceSelection(18, 4, "healing-v1"), scope.Settings.Balance);
        Assert.Equal(HarnessJson.Hash(scope.Settings), HarnessJson.Hash(TowerBundle.ReadSettings(TestContentPaths.FindApiRoot())));
        Assert.Equal(Cases, q.Cases.Count); Assert.Equal(Cases, q.Cases.Select(c => c.Id).Distinct().Count());
        Assert.Equal(Samples, q.Seeds.Count); Assert.Equal(Samples, q.Seeds.Distinct().Count());
        Assert.Equal(q.Seeds, Source<JsonElement>("request.json").GetProperty("seeds").Deserialize<int[]>(HarnessJson.Options));
        Assert.Equal(3, q.Cases.Count(c => c.Budget.PriorityFloor == 10 && c.Budget.EssenceSlots == 6 && c.Purpose == "intended-progression"));
        Assert.Equal(12, q.Cases.Count(c => c.Budget.PriorityFloor == 11 && c.Budget.EssenceSlots == 7 && c.Purpose == "intended-progression"));
        Assert.Equal(2, q.Cases.Count(c => c.Budget.PriorityFloor == 11 && c.Budget.EssenceSlots == 6 && c.Purpose == "diagnostic"));
        Assert.Equal(2, q.Cases.Count(c => c.Budget.PriorityFloor == 11 && c.Budget.EssenceSlots == 4 && c.Purpose == "diagnostic"));
        var profiles = TowerGearProfiles.Read(q.Profiles).Profiles;
        Assert.Equal(new[] { "precision", "ability-haste", "restorer-specialization", "armor-and-health", "resistance-and-health", "health-and-regeneration" }, profiles.Select(p => p.Id));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = deadline.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output); Directory.CreateDirectory(q.Output);
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        var attempts = 0; var completed = 0; var success = false; var matched = 0;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % Samples == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1073741824); }
        try
        {
            Save("request.json", q); Save("scope.json", scope); Save("profiles.json", profiles);
            Save("protocol.json", new { version = Version, cases = Cases, samples = Samples, profiles = 7,
                maximumFights = Fights, maximumSeconds = 840, maximumBytes = 2L * 1073741824,
                newSeeds = 0, retries = 0, qualificationFightsIncluded = 224,
                rule = "All 133 cells required. Above 16/32 flags a ceiling concern; 4–16/32 is observed viability only. Historical seeds cannot establish fresh balance acceptance. No automatic calibration or extension." });
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
            var oldCells = Source<JsonElement>("cells.json").EnumerateArray().Where(c => c.GetProperty("floor").GetInt32() == 10).ToArray();
            var oldTrials = File.ReadLines(Path.Combine(q.Source, "study/trials.jsonl"))
                .Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!)
                .Where(t => t.Stage.StartsWith("10/", StringComparison.Ordinal)).ToArray();
            Assert.Equal(files["study/trials.jsonl"], HarnessJson.FileHash(Path.Combine(q.Source, "study/trials.jsonl")));
            Assert.Equal(224, oldTrials.Length);
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate())
            {
                foreach (var c in q.Cases)
                {
                    Assert.Empty(c.Scenario.Seeds); Assert.Equal(c.Budget.PriorityFloor, c.Scenario.FloorNumber);
                    Assert.True(TowerBossDiscovery.LegalPurpose(c.Budget, c.Purpose));
                    TowerBossDiscovery.ValidateEquipment(c.Scenario.Party, c.Budget, c.Budget.PriorityFloor == 10 ? 15 : 10);
                    cells.Add(new(c.Id, c.Purpose, c.Budget.PriorityFloor, c.Budget.EssenceSlots, "baseline", c.Scenario with { Seeds = q.Seeds }));
                    cells.AddRange(profiles.Select(p => new Cell(c.Id, c.Purpose, c.Budget.PriorityFloor, c.Budget.EssenceSlots,
                        p.Id, TowerGearProfiles.Apply(c.Scenario, p, content) with { Seeds = q.Seeds })));
                }
                Assert.Equal(Cases * 7, cells.Count);
                foreach (var cell in cells)
                    _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario, q.Seeds[0], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks), token);
                for (var i = 0; i < 7; i++)
                {
                    Assert.Equal(HarnessJson.Hash(oldCells[i].GetProperty("scenario")), HarnessJson.Hash(cells[i].Scenario));
                    for (var j = 0; j < Samples; j++)
                        Assert.Equal(oldTrials[i * Samples + j].InputHash, HarnessJson.Hash(runner.CreateInput(cells[i].Scenario,
                            q.Seeds[j], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks)));
                }
            }
            Save("cells.json", cells); Save("preflight.json", new { status = "PreparedNoFights", cells = cells.Count, knownInputMatches = 224 });
            VerifyInputs(); Check();
            var study = Path.Combine(q.Output, "study"); var archive = F.Archive(study, scope, Fights, q.Output, token);
            var rows = new List<Row>(); var baselines = new Dictionary<string, bool[]>();
            foreach (var cell in cells)
            {
                var reports = new List<TowerBattleReport>();
                foreach (var seed in q.Seeds)
                {
                    Check(); Assert.True(++attempts <= Fights);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                    var trial = await archive.EvaluateAsync(Version, $"{cell.Case}/{cell.Profile}", cell.Scenario, seed, token);
                    completed++; reports.Add(trial.Report);
                    if (completed <= 224)
                    {
                        var oldTrial = oldTrials[completed - 1];
                        var name = "study/battles/" + oldTrial.Id + ".json.gz";
                        Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name)));
                        Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(Path.Combine(q.Source, "study"), oldTrial.Id, oldScope.ReportStorage)), HarnessJson.Hash(trial.Report));
                        matched++;
                        if (matched == 224) Save("runtime-qualification.json", new { status = "Matched", inputs = 224, fullReports = 224, scope = "Floor-10 retained baseline and all six gear variants; no global equivalence claim." });
                    }
                    else Assert.Equal(224, matched);
                }
                var wins = reports.Select(r => r.Succeeded).ToArray();
                if (cell.Profile == "baseline") baselines.Add(cell.Case, wins);
                var paired = wins.Zip(baselines[cell.Case]).ToArray(); var count = wins.Count(w => w);
                var row = new Row(cell.Case, cell.Purpose, cell.Floor, cell.EssenceSlots, cell.Profile, count,
                    reports.Count(r => r.Battle.Summary.ContentOutcome == BattleOutcome.Draw), Samples,
                    reports.Average(r => r.GuardianHealthRemainingPercent), reports.Average(r => r.Battle.Summary.DurationSeconds),
                    paired.Count(p => p.First && !p.Second), paired.Count(p => !p.First && p.Second),
                    count > 16 ? "ObservedAboveCeiling" : count >= 4 ? "ObservedViable" : "ObservedBelowMinimum");
                rows.Add(row); Console.WriteLine(JsonSerializer.Serialize(row, HarnessJson.Options));
            }
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(Fights, trials.Count);
            foreach (var (trial, ordinal) in trials.Select((t, i) => (t, i)))
            {
                Check(); var cell = cells[ordinal / Samples]; Assert.Equal(q.Seeds[ordinal % Samples], trial.Seed);
                Assert.Equal($"{cell.Case}/{cell.Profile}", trial.Stage); Assert.Equal(HarnessJson.Hash(cell.Scenario), trial.Recipe);
                var input = runner.CreateInput(cell.Scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(scope, Version, input), trial.CacheKey);
            }
            VerifyInputs();
            foreach (var pin in scope.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            Save("result.json", new { status = "CheckpointScreenComplete", fights = completed, rows, runtimeParityReports = matched,
                newSeeds = 0, confirmedTeams = 0, searchRuns = 0, retries = 0, balanceAcceptance = "NotAssessedHistoricalSeeds",
                seconds = watch.Elapsed.TotalSeconds });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
