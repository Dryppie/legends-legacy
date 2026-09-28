using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessEquipmentCycleScreenTests
{
    private const string Version = "tower-equipment-cycle-screen-v1";
    private const int Samples = 32, Cases = 19, ScreenFights = Cases * 7 * Samples, QualificationFights = 224;
    private const int MaximumFights = ScreenFights + QualificationFights;
    private sealed record Checkpoint(string Id, string Purpose, string SourceId, string SourceContext,
        TowerSearchBudget Budget, TowerScenario Scenario);
    private sealed record Request(string Version, string Output, string Source, string SourcePin, string Runtime,
        string Profiles, string Budget, IReadOnlyList<Checkpoint> Cases, IReadOnlyList<int> Seeds,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> AssemblyHashes);
    private sealed record Cell(string Case, string Purpose, int Floor, int EssenceSlots, string Profile, TowerScenario Scenario);
    private sealed record Row(string Case, string Purpose, int Floor, int EssenceSlots, string Profile,
        int Wins, int Draws, int Samples, decimal MeanGuardianHealth, double MeanDurationSeconds,
        int GainedWins, int LostWins, int PriorBudgetWins, int GainedFromPriorBudget, int LostFromPriorBudget, string Observation);
    private sealed class ScreenFactAttribute : FactAttribute
    {
        public ScreenFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_EQUIPMENT_CYCLE_SCREEN")))
                Skip = "Set LL_EQUIPMENT_CYCLE_SCREEN for the bounded 4,480-fight historical-seed screen.";
        }
    }

    [ScreenFact]
    public async Task Repeating_equipment_curve_keeps_all_checkpoint_teams_profiles_and_paired_controls()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_EQUIPMENT_CYCLE_SCREEN")!);
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
        var oldCases = Source<JsonElement>("request.json").GetProperty("cases").Deserialize<Checkpoint[]>(HarnessJson.Options)!;
        var oldCells = Source<Cell[]>("cells.json");
        Assert.Equal(Cases, q.Cases.Count); Assert.Equal(Cases, q.Cases.Select(c => c.Id).Distinct().Count());
        Assert.Equal(oldCases.Select(c => c.Id), q.Cases.Select(c => c.Id));
        Assert.Equal(Samples, q.Seeds.Count); Assert.Equal(Samples, q.Seeds.Distinct().Count());
        Assert.Equal(q.Seeds, Source<JsonElement>("request.json").GetProperty("seeds").Deserialize<int[]>(HarnessJson.Options));
        var draft = TowerContractJson.Read<TowerProgressionDraft>(q.Budget);
        TowerProgressionPreview.Validate(draft); Assert.NotNull(draft.EquipmentCycle);
        var profiles = TowerGearProfiles.Read(q.Profiles).Profiles;
        Assert.Equal(new[] { "precision", "ability-haste", "restorer-specialization", "armor-and-health", "resistance-and-health", "health-and-regeneration" }, profiles.Select(p => p.Id));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = deadline.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output); Directory.CreateDirectory(q.Output);
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        var attempts = 0; var completed = 0; var success = false; var matched = 0;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= MaximumFights);
            if (completed % Samples == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1073741824); }
        void Attempt() { Check(); Assert.True(++attempts <= MaximumFights);
            TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n"); }
        TowerBattleReport OldReport(LoadoutTrial trial)
        {
            var name = "study/battles/" + trial.Id + ".json.gz";
            Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name)));
            return TowerLoadoutArchive.ReadBattle(Path.Combine(q.Source, "study"), trial.Id, oldScope.ReportStorage);
        }
        try
        {
            Save("request.json", q); Save("scope.json", scope); Save("profiles.json", profiles); Save("budget.json", draft);
            Save("protocol.json", new { version = Version, cases = Cases, samples = Samples, profiles = 7,
                maximumFights = MaximumFights, screenFights = ScreenFights, qualificationFights = QualificationFights,
                maximumSeconds = 840, maximumBytes = 2L * 1073741824, newSeeds = 0, retries = 0,
                rule = "First reproduce 224 old-budget inputs/full reports, then complete all 133 new-budget cells. Above 16/32 flags a ceiling concern; 4–16/32 is observed viability only. Historical seeds cannot establish balance acceptance. No automatic calibration or extension." });
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
            Assert.Equal(files["study/trials.jsonl"], HarnessJson.FileHash(Path.Combine(q.Source, "study/trials.jsonl")));
            var oldTrials = File.ReadLines(Path.Combine(q.Source, "study/trials.jsonl"))
                .Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!).ToArray();
            Assert.Equal(ScreenFights, oldTrials.Length);
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate())
            {
                foreach (var (c, i) in q.Cases.Select((c, i) => (c, i)))
                {
                    var old = oldCases[i]; var band = TowerProgressionEquipment.ForFloor(draft.EquipmentCycle!, c.Scenario.FloorNumber);
                    Assert.Equal(old.Purpose, c.Purpose); Assert.Equal(old.SourceId, c.SourceId); Assert.Equal(old.SourceContext, c.SourceContext);
                    Assert.Equal(old.Budget with { Rank = band.Rank, Quality = band.Quality }, c.Budget);
                    Assert.Equal(HarnessJson.Hash(TowerProgressionEquipment.Apply(old.Scenario, draft.EquipmentCycle!, content)), HarnessJson.Hash(c.Scenario));
                    Assert.Empty(c.Scenario.Seeds); Assert.Equal(c.Budget.PriorityFloor, c.Scenario.FloorNumber);
                    Assert.True(TowerBossDiscovery.LegalPurpose(c.Budget, c.Purpose));
                    TowerBossDiscovery.ValidateEquipment(c.Scenario.Party, c.Budget, c.Budget.PriorityFloor == 10 ? 15 : 10);
                    cells.Add(new(c.Id, c.Purpose, c.Budget.PriorityFloor, c.Budget.EssenceSlots, "baseline", c.Scenario with { Seeds = q.Seeds }));
                    cells.AddRange(profiles.Select(p => new Cell(c.Id, c.Purpose, c.Budget.PriorityFloor, c.Budget.EssenceSlots,
                        p.Id, TowerGearProfiles.Apply(c.Scenario, p, content) with { Seeds = q.Seeds })));
                }
                Assert.Equal(Cases * 7, cells.Count);
                foreach (var cell in cells.Concat(oldCells.Take(7)))
                    _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario, q.Seeds[0], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks), token);
                for (var i = 0; i < QualificationFights; i++)
                    Assert.Equal(oldTrials[i].InputHash, HarnessJson.Hash(runner.CreateInput(oldCells[i / Samples].Scenario,
                        q.Seeds[i % Samples], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks)));
            }
            Save("cells.json", cells); Save("qualification-cells.json", oldCells.Take(7).ToArray());
            Save("preflight.json", new { status = "PreparedNoFights", cells = cells.Count, qualificationCells = 7, knownInputMatches = QualificationFights });
            VerifyInputs(); Check();
            var qualification = F.Archive(Path.Combine(q.Output, "qualification"), scope, QualificationFights, q.Output, token);
            for (var i = 0; i < QualificationFights; i++)
            {
                Attempt(); var cell = oldCells[i / Samples];
                var trial = await qualification.EvaluateAsync(Version, $"{cell.Case}/{cell.Profile}", cell.Scenario, q.Seeds[i % Samples], token);
                completed++; Assert.Equal(oldTrials[i].InputHash, trial.Trial.InputHash);
                Assert.Equal(HarnessJson.Hash(OldReport(oldTrials[i])), HarnessJson.Hash(trial.Report)); matched++;
            }
            Assert.Equal(0, qualification.CacheHits); F.Seal(Path.Combine(q.Output, "qualification"));
            Assert.Equal(QualificationFights, TowerLoadoutArchive.Verify(Path.Combine(q.Output, "qualification"), token).Count);
            Save("runtime-qualification.json", new { status = "Matched", inputs = matched, fullReports = matched,
                scope = "Old-budget floor-10 authored party and six gear variants; no global equivalence claim." });
            var study = Path.Combine(q.Output, "study"); var archive = F.Archive(study, scope, ScreenFights, q.Output, token);
            var rows = new List<Row>(); var baselines = new Dictionary<string, bool[]>();
            foreach (var (cell, ordinal) in cells.Select((c, i) => (c, i)))
            {
                var reports = new List<TowerBattleReport>();
                foreach (var seed in q.Seeds)
                {
                    Attempt(); var trial = await archive.EvaluateAsync(Version, $"{cell.Case}/{cell.Profile}", cell.Scenario, seed, token);
                    completed++; reports.Add(trial.Report);
                }
                var wins = reports.Select(r => r.Succeeded).ToArray();
                if (cell.Profile == "baseline") baselines.Add(cell.Case, wins);
                var paired = wins.Zip(baselines[cell.Case]).ToArray(); var count = wins.Count(w => w);
                var priorWins = oldTrials.Skip(ordinal * Samples).Take(Samples).Select(t => {
                    Assert.Equal($"{cell.Case}/{cell.Profile}", t.Stage); return OldReport(t).Succeeded;
                }).ToArray();
                var priorPairs = wins.Zip(priorWins).ToArray();
                var row = new Row(cell.Case, cell.Purpose, cell.Floor, cell.EssenceSlots, cell.Profile, count,
                    reports.Count(r => r.Battle.Summary.ContentOutcome == BattleOutcome.Draw), Samples,
                    reports.Average(r => r.GuardianHealthRemainingPercent), reports.Average(r => r.Battle.Summary.DurationSeconds),
                    paired.Count(p => p.First && !p.Second), paired.Count(p => !p.First && p.Second),
                    priorWins.Count(w => w), priorPairs.Count(p => p.First && !p.Second), priorPairs.Count(p => !p.First && p.Second),
                    count > 16 ? "ObservedAboveCeiling" : count >= 4 ? "ObservedViable" : "ObservedBelowMinimum");
                rows.Add(row); Console.WriteLine(JsonSerializer.Serialize(row, HarnessJson.Options));
            }
            Assert.Equal(MaximumFights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(ScreenFights, trials.Count);
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
            Save("result.json", new { status = "EquipmentCycleScreenComplete", fights = completed, screenFights = ScreenFights,
                rows, runtimeParityReports = matched, newSeeds = 0, confirmedTeams = 0, searchRuns = 0, retries = 0,
                balanceAcceptance = "NotAssessedHistoricalSeeds", seconds = watch.Elapsed.TotalSeconds });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
