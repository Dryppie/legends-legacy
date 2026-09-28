using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using C = EssenceSystem.Tests.BalanceHarnessAffinityTeamConfirmationTests;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessCarriedConfirmationTests
{
    private const string Version = "tower-floor11-carried-confirmation-v1";
    private const string PriorPin = "95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944";
    private const int Samples = 256, Cells = 344, QualificationFights = 32 * Cells, ConfirmationFights = Samples * Cells, Fights = QualificationFights + ConfirmationFights;
    private const long MaximumBytes = 4L * 1073741824;
    private sealed record Request(string Version, string Source, string SourcePin, string Output, string Registry,
        string Runtime, string HistorySource, string HistoryPin, int Master,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> AssemblyHashes,
        IReadOnlyDictionary<string, string> RequiredHistory,
        IReadOnlyDictionary<string, string> Recoveries, IReadOnlyDictionary<string, string> RecoveryHashes);
    private sealed record Cell(string Id, string SourceCase, string Kind, string? Addition, string Profile, TowerScenario Scenario);
    private sealed record Row(string Id, int Level, int EssenceSlots, int Wins, int Draws, int Samples, RateEstimate Adjusted);
    private sealed class ConfirmationFactAttribute : FactAttribute
    {
        public ConfirmationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_CARRIED_CONFIRMATION")))
                Skip = "Set LL_CARRIED_CONFIRMATION for one frozen 11,008-fight runtime replay and 88,064-fight fresh confirmation.";
        }
    }

    private static JsonElement SelectSetting(JsonElement calibration)
    {
        if (calibration.GetProperty("status").GetString() != "RefinedCarriedCalibrationComplete")
            throw new InvalidDataException("Completed carried calibration required.");
        decimal[] grid = [2m, 2.25m, 2.5m, 2.75m, 3m, 3.5m];
        var summaries = calibration.GetProperty("summaries").EnumerateArray().ToArray();
        if (summaries.Length != grid.Length || summaries.Where((s, i) =>
                s.GetProperty("variant").GetInt32() != i || s.GetProperty("multiplier").GetDecimal() != grid[i]
                || s.GetProperty("health").GetDecimal() != 6.525m * grid[i]
                || s.GetProperty("offense").GetDecimal() != 8.37m * grid[i]).Any())
            throw new InvalidDataException("Changed fixed calibration grid.");
        var eligible = summaries.Where(s => s.GetProperty("eligible").GetBoolean()).ToArray();
        if (eligible.Length == 0 || HarnessJson.Hash(eligible[0]) != HarnessJson.Hash(calibration.GetProperty("selected")))
            throw new InvalidDataException("Only the lowest eligible setting can receive fresh confirmation.");
        return eligible[0].Clone();
    }

    [Fact]
    public void Confirmation_uses_the_lowest_eligible_setting_without_reselection()
    {
        decimal[] grid = [2m, 2.25m, 2.5m, 2.75m, 3m, 3.5m];
        var summaries = grid.Select((m, i) => new { variant = i, multiplier = m, health = 6.525m * m,
            offense = 8.37m * m, eligible = i is 2 or 3 }).ToArray();
        JsonElement Calibration(int selection, string status = "RefinedCarriedCalibrationComplete") =>
            JsonSerializer.SerializeToElement(new { status, summaries, selected = summaries[selection] });
        Assert.Equal(2.5m, SelectSetting(Calibration(2)).GetProperty("multiplier").GetDecimal());
        Assert.Throws<InvalidDataException>(() => SelectSetting(Calibration(3)));
        Assert.Throws<InvalidDataException>(() => SelectSetting(Calibration(1)));
        Assert.Throws<InvalidDataException>(() => SelectSetting(Calibration(2, "Incomplete")));
        var changed = summaries.ToArray(); changed[2] = changed[2] with { health = 1m };
        Assert.Throws<InvalidDataException>(() => SelectSetting(JsonSerializer.SerializeToElement(new {
            status = "RefinedCarriedCalibrationComplete", summaries = changed, selected = changed[2] })));
        var none = summaries.Select(s => s with { eligible = false }).ToArray();
        Assert.Throws<InvalidDataException>(() => SelectSetting(JsonSerializer.SerializeToElement(new {
            status = "RefinedCarriedCalibrationComplete", summaries = none, selected = (object?)null })));
    }

    private static string Assess(IReadOnlyList<Row> rows)
    {
        if (rows.Count != Cells || rows.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != Cells
            || rows.Count(r => r.EssenceSlots == 7 && r.Level == 60) != 312
            || rows.Count(r => r.EssenceSlots == 6 && r.Level == 50) != 14
            || rows.Count(r => r.EssenceSlots == 6 && r.Level == 60) != 4
            || rows.Count(r => r.EssenceSlots == 4 && r.Level == 30) != 14
            || rows.Any(r => r.Samples != Samples || r.Wins is < 0 or > Samples || r.Draws < 0 || r.Wins + r.Draws > Samples
                || r.Adjusted != TowerBalanceEvaluator.Wilson(r.Wins, Samples, Cells)))
            throw new InvalidDataException("Full fixed family and recomputable simultaneous intervals required.");
        var intended = rows.Where(r => r.EssenceSlots == 7).ToArray();
        var controls = rows.Where(r => r.EssenceSlots != 7).ToArray();
        if (intended.Any(r => r.Wins * 2 > Samples) || intended.All(r => r.Adjusted.Upper < .1)
            || controls.Any(r => r.Wins / (double)Samples >= .1)) return "Fail";
        return intended.All(r => r.Adjusted.Upper <= .5) && intended.Any(r => r.Adjusted.Lower >= .1)
            && controls.All(r => r.Adjusted.Upper < .1) ? "Pass" : "Inconclusive";
    }

    [Theory]
    [InlineData(80, 0, "Pass")]
    [InlineData(128, 0, "Inconclusive")]
    [InlineData(129, 0, "Fail")]
    [InlineData(0, 0, "Fail")]
    [InlineData(80, 25, "Inconclusive")]
    [InlineData(80, 26, "Fail")]
    public void Simultaneous_assessment_keeps_the_strongest_team_and_all_controls(int best, int control, string expected)
    {
        var rows = Enumerable.Range(0, Cells).Select(i => {
            var slots = i < 312 ? 7 : i < 330 ? 6 : 4;
            var level = i < 312 ? 60 : i < 326 ? 50 : i < 330 ? 60 : 30;
            var wins = i == 311 ? best : i == 329 ? control : 0;
            return new Row($"cell-{i}", level, slots, wins, 0, Samples, TowerBalanceEvaluator.Wilson(wins, Samples, Cells)!);
        }).ToArray();
        Assert.Equal(expected, Assess(rows));
        Assert.Throws<InvalidDataException>(() => Assess(rows.Skip(1).ToArray()));
        var duplicate = rows.ToArray(); duplicate[0] = duplicate[1];
        Assert.Throws<InvalidDataException>(() => Assess(duplicate));
        var changedInterval = rows.ToArray(); changedInterval[0] = rows[0] with { Adjusted = TowerBalanceEvaluator.Wilson(0, Samples, 1)! };
        Assert.Throws<InvalidDataException>(() => Assess(changedInterval));
        var missingControl = rows.ToArray(); missingControl[329] = rows[329] with { Level = 50 };
        Assert.Throws<InvalidDataException>(() => Assess(missingControl));
    }

    [ConfirmationFact]
    public async Task Lowest_eligible_carried_setting_receives_one_complete_fresh_family_panel()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_CARRIED_CONFIRMATION")!);
        Assert.Equal(Version, q.Version); Assert.Equal(64, q.SourcePin.Length); Assert.Equal(PriorPin, q.HistoryPin);
        Assert.Equal(2026092817, q.Master);
        Assert.False(Path.Exists(q.Output));
        Assert.Equal(Path.GetFullPath(q.Registry), Path.GetDirectoryName(Path.GetFullPath(q.Output)));
        foreach (var path in q.InputHashes.Keys.Concat(new[] { q.Source, q.Output, q.Registry, q.Runtime, q.HistorySource })) TowerProposalStudy.Unlinked(path);
        void VerifyInputs() { foreach (var pin in q.InputHashes.Concat(q.RecoveryHashes)) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        VerifyInputs(); Assert.Equal(q.SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name) { Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name))); return HarnessJson.Read<T>(Path.Combine(q.Source, name)); }
        var selected = SelectSetting(Source<JsonElement>("result.json"));
        var variant = $"variant-{selected.GetProperty("variant").GetInt32():D2}";
        var priorScope = Source<LoadoutScope>(variant + "/scope.json");
        var scope = priorScope with { Algorithm = Version, Execution = ExecutionIdentity.Current() };
        Assert.Equal(HarnessJson.Hash(q.AssemblyHashes), HarnessJson.Hash(scope.Execution.AssemblyHashes));
        Assert.Equal(HarnessJson.Hash(scope.Settings), HarnessJson.Hash(TowerBundle.ReadSettings(TestContentPaths.FindApiRoot())));
        var sourceCells = Source<Cell[]>("cells.json");
        var cells = sourceCells.Select(c => c with { Scenario = c.Scenario with { Seeds = [] } }).ToArray();
        Assert.Equal(Cells, cells.Length); Assert.Equal(Cells, cells.Select(c => c.Id).Distinct().Count());
        Assert.Equal(112, cells.Count(c => c.Kind == "retained-control"));
        Assert.Equal(228, cells.Count(c => c.Kind == "seventh-addition")); Assert.Equal(4, cells.Count(c => c.Kind == "level-control"));
        Assert.Equal(q.HistoryPin, HarnessJson.FileHash(Path.Combine(q.HistorySource, "files.json")));
        var historyFiles = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.HistorySource, "files.json"));
        Assert.Equal(historyFiles["seed-ledger.json"], HarnessJson.FileHash(Path.Combine(q.HistorySource, "seed-ledger.json")));
        var priorResultPath = Path.Combine(q.HistorySource, "result.json");
        Assert.Equal(historyFiles["result.json"], HarnessJson.FileHash(priorResultPath));
        var priorResult = HarnessJson.Read<JsonElement>(priorResultPath);
        Assert.Equal("Floor10FamilyConfirmationComplete", priorResult.GetProperty("status").GetString());
        Assert.Equal("Pass", priorResult.GetProperty("assessment").GetString());
        Assert.Equal(5376, priorResult.GetProperty("fights").GetInt32());
        var historical = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(q.HistorySource, "seed-ledger.json")));
        Assert.Equal(835063, historical.Length);
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(4200)); var token = stop.Token;
        using var registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.Registry, "complete-family-allocation"));
        using var lease = TowerCompactBundle.AcquireWriter(q.Output); Directory.CreateDirectory(q.Output);
        var clock = System.Diagnostics.Stopwatch.StartNew(); var attempts = 0; var completed = 0; var success = false; var lastStorageCheck = -1;
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % Samples == 0 && lastStorageCheck != completed) {
                Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < MaximumBytes); lastStorageCheck = completed; } }
        try
        {
            Save("request.json", q); Save("scope.json", scope); Save("selected.json", selected); Save("cells.json", cells);
            Save("prior-result.json", priorResult); Save("qualification-cells.json", sourceCells);
            Save("protocol.json", new { version = Version, cells = Cells, samples = Samples, maximumFights = Fights,
                qualificationFights = QualificationFights, confirmationFights = ConfirmationFights, maximumSeconds = 4200, maximumBytes = MaximumBytes, retries = 0, freshSeeds = Samples,
                rule = "Approximate simultaneous 95% Bonferroni-Wilson intervals across all 344 cells. Every seven-Essence upper bound <= .50, some seven-Essence lower bound >= .10, every four-/six-Essence upper bound < .10. Observed intended > .50, observed control >= .10, or all intended upper bounds < .10 fail; otherwise unresolved evidence is Inconclusive. No pooling, reselection, extension or application." });
            var history = TowerRefinementComparisonLaunch.Refresh(q.Registry, q.Output, q.RequiredHistory, historical, token, q.Recoveries);
            Save("history-files.json", history.Files);
            var root = Path.Combine(q.Output, "content");
            Assert.Equal(HarnessJson.Hash(scope.ContentHashes), HarnessJson.Hash(TowerBundle.CopyContent(Path.Combine(q.Source, variant, "content"), root, token)));
            TowerBundle.WriteSettings(Path.Combine(root, "appsettings.json"), scope.Settings);
            foreach (var pin in scope.Execution.AssemblyHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var path in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, path); if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(path, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var runner = new TowerBattleRunner(root, OfflineContent.ForTower(root, scope.Settings));
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate())
                foreach (var cell in cells)
                    _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario with { Seeds = [0] }, 0, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks), token);
            Save("preflight.json", new { status = "PreparedNoFights", cells = Cells, historyCount = historical.Length, currentRuntimeRequiresFullReportQualification = true });
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token); VerifyInputs(); Check();
            var qualificationSeeds = sourceCells[0].Scenario.Seeds.Take(32).ToArray();
            Assert.Equal(32, qualificationSeeds.Distinct().Count());
            Assert.All(sourceCells, c => Assert.Equal(qualificationSeeds, c.Scenario.Seeds.Take(32)));
            var oldLedger = variant + "/study/trials.jsonl";
            Assert.Equal(files[oldLedger], HarnessJson.FileHash(Path.Combine(q.Source, oldLedger)));
            var oldTrials = File.ReadLines(Path.Combine(q.Source, oldLedger))
                .Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!).ToArray();
            Assert.Equal(QualificationFights, oldTrials.Length);
            var qualification = Path.Combine(q.Output, "qualification");
            var replay = F.Archive(qualification, scope, QualificationFights, q.Output, token);
            for (var n = 0; n < oldTrials.Length; n++)
            {
                Check(); var cell = sourceCells[n / 32]; var seed = qualificationSeeds[n % 32]; var old = oldTrials[n];
                Assert.Equal(cell.Id, old.Stage); Assert.Equal(seed, old.Seed);
                var input = runner.CreateInput(cell.Scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                Assert.Equal(old.InputHash, HarnessJson.Hash(input));
                Assert.True(++attempts <= Fights);
                TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), JsonSerializer.Serialize(new {
                    attempt = attempts, phase = "qualification", stage = cell.Id, seed }) + "\n");
                var trial = await replay.EvaluateAsync(Version, cell.Id, cell.Scenario, seed, token); completed++;
                var oldReport = variant + "/study/battles/" + old.Id + ".json.gz";
                Assert.Equal(files[oldReport], HarnessJson.FileHash(Path.Combine(q.Source, oldReport)));
                Assert.Equal(old.InputHash, trial.Trial.InputHash);
                Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(Path.Combine(q.Source, variant, "study"), old.Id, priorScope.ReportStorage)),
                    HarnessJson.Hash(trial.Report));
            }
            Assert.Equal(0, replay.CacheHits); F.Seal(qualification);
            var replays = TowerLoadoutArchive.Verify(qualification, token); Assert.Equal(QualificationFights, replays.Count);
            foreach (var (trial, n) in replays.Select((t, n) => (t, n)))
            {
                Check(); var cell = sourceCells[n / 32]; Assert.Equal(qualificationSeeds[n % 32], trial.Seed);
                Assert.Equal(cell.Id, trial.Stage); Assert.Equal(HarnessJson.Hash(cell.Scenario), trial.Recipe);
                var input = runner.CreateInput(cell.Scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(scope, Version, input), trial.CacheKey);
            }
            Save("runtime-qualification.json", new { status = "Matched", inputs = QualificationFights, fullReports = QualificationFights,
                completedBeforeReservation = true });
            Console.WriteLine("Current runtime matched all 11,008 selected-setting inputs and full reports; reserving fresh panel.");
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token); VerifyInputs(); Check();
            var allocator = C.Allocator(q.Master, 0, Version);
            var reserved = TowerCompleteReservation.Reserve(q.Output, allocator, historical, MaximumBytes, token);
            TowerCompleteReservation.Verify(q.Output, reserved.Seeds, allocator, token);
            var seeds = reserved.Seeds.First.Concat(reserved.Seeds.Second).ToArray(); Assert.Equal(Samples, seeds.Length);
            Save("confirmation-seeds.json", seeds);
            var study = Path.Combine(q.Output, "study"); var archive = F.Archive(study, scope, ConfirmationFights, q.Output, token);
            var rows = new List<Row>();
            foreach (var cell in cells)
            {
                var scenario = cell.Scenario with { Seeds = seeds }; var wins = 0; var draws = 0;
                foreach (var seed in seeds)
                {
                    Check(); Assert.True(++attempts <= Fights);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), JsonSerializer.Serialize(new {
                        attempt = attempts, phase = "confirmation", stage = cell.Id, seed }) + "\n");
                    var trial = await archive.EvaluateAsync(Version, cell.Id, scenario, seed, token); completed++;
                    if (trial.Report.Succeeded) wins++;
                    if (trial.Report.Battle.Summary.ContentOutcome == BattleOutcome.Draw) draws++;
                }
                rows.Add(new(cell.Id, scenario.Party[0].Build.CharacterLevel, scenario.Party[0].Build.EssenceIds.Count,
                    wins, draws, Samples, TowerBalanceEvaluator.Wilson(wins, Samples, Cells)!));
            }
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(ConfirmationFights, trials.Count);
            foreach (var (trial, n) in trials.Select((t, n) => (t, n)))
            {
                Check(); var cell = cells[n / Samples]; var scenario = cell.Scenario with { Seeds = seeds };
                Assert.Equal(seeds[n % Samples], trial.Seed); Assert.Equal(cell.Id, trial.Stage); Assert.Equal(HarnessJson.Hash(scenario), trial.Recipe);
                var input = runner.CreateInput(scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(scope, Version, input), trial.CacheKey);
            }
            TowerCompleteReservation.Verify(q.Output, reserved.Seeds, allocator, token);
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token); VerifyInputs();
            foreach (var pin in scope.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            var assessment = Assess(rows);
            Save("result.json", new { status = "CarriedConfirmationComplete", fights = completed, confirmationFights = ConfirmationFights,
                runtimeParityReports = QualificationFights, rows, assessment,
                freshSeeds = Samples, historicalSeeds = historical.Length, exclusionUnion = historical.Length + Samples,
                selectedFactor = selected.GetProperty("multiplier").GetDecimal(), historyAssessment = "Pass", historyManifest = q.HistoryPin,
                retries = 0, seconds = clock.Elapsed.TotalSeconds,
                scope = "Fixed 344-cell carried-equipment family at the lowest eligible finer-grid factor. Historical runtime replays are excluded from confirmation counts. Per-study coverage, not a campaign-wide guarantee. Approximate simultaneous coverage; no guarantee about unsearched recipes. No production application or search-algorithm claim." });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
