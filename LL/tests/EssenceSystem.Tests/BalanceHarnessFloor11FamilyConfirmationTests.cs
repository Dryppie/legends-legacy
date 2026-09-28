using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using C = EssenceSystem.Tests.BalanceHarnessAffinityTeamConfirmationTests;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessFloor11FamilyConfirmationTests
{
    private const string Version = "tower-floor11-fixed-family-confirmation-v1";
    private const int Samples = 256, Cells = 228, Fights = Samples * Cells;
    private const long MaximumBytes = 2L * 1073741824;
    private sealed record Request(string Version, string Source, string SourcePin, string Output, string Registry,
        string Runtime, string HistorySource, string HistoryPin, int Master,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> RequiredHistory,
        IReadOnlyDictionary<string, string> Recoveries, IReadOnlyDictionary<string, string> RecoveryHashes);
    private sealed record Cell(string Id, string SourceCase, string Kind, string? Addition, string Profile, TowerScenario Scenario);
    private sealed record Row(string Id, int Level, int EssenceSlots, int Wins, int Draws, int Samples, RateEstimate Adjusted);
    private sealed class ConfirmationFactAttribute : FactAttribute
    {
        public ConfirmationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_FLOOR11_FAMILY_CONFIRMATION")))
                Skip = "Set LL_FLOOR11_FAMILY_CONFIRMATION for one frozen 58,368-fight fresh confirmation.";
        }
    }

    private static string Assess(IReadOnlyList<Row> rows)
    {
        if (rows.Count != Cells || rows.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != Cells
            || rows.Count(r => r.EssenceSlots == 7 && r.Level == 60) != 198
            || rows.Count(r => r.EssenceSlots == 6 && r.Level == 50) != 14
            || rows.Count(r => r.EssenceSlots == 6 && r.Level == 60) != 2
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
            var slots = i < 198 ? 7 : i < 214 ? 6 : 4;
            var level = i < 198 ? 60 : i < 212 ? 50 : i < 214 ? 60 : 30;
            var wins = i == 197 ? best : i == 213 ? control : 0;
            return new Row($"cell-{i}", level, slots, wins, 0, Samples, TowerBalanceEvaluator.Wilson(wins, Samples, Cells)!);
        }).ToArray();
        Assert.Equal(expected, Assess(rows));
        Assert.Throws<InvalidDataException>(() => Assess(rows.Skip(1).ToArray()));
        var duplicate = rows.ToArray(); duplicate[0] = duplicate[1];
        Assert.Throws<InvalidDataException>(() => Assess(duplicate));
        var changedInterval = rows.ToArray(); changedInterval[0] = rows[0] with { Adjusted = TowerBalanceEvaluator.Wilson(0, Samples, 1)! };
        Assert.Throws<InvalidDataException>(() => Assess(changedInterval));
        var missingControl = rows.ToArray(); missingControl[213] = rows[213] with { Level = 50 };
        Assert.Throws<InvalidDataException>(() => Assess(missingControl));
    }

    [ConfirmationFact]
    public async Task Audited_calibration_candidate_receives_one_complete_fresh_family_panel()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_FLOOR11_FAMILY_CONFIRMATION")!);
        Assert.Equal(Version, q.Version); Assert.False(Path.Exists(q.Output));
        Assert.Equal(Path.GetFullPath(q.Registry), Path.GetDirectoryName(Path.GetFullPath(q.Output)));
        foreach (var path in q.InputHashes.Keys.Concat(new[] { q.Source, q.Output, q.Registry, q.Runtime, q.HistorySource })) TowerProposalStudy.Unlinked(path);
        void VerifyInputs() { foreach (var pin in q.InputHashes.Concat(q.RecoveryHashes)) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        VerifyInputs(); Assert.Equal(q.SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name) { Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name))); return HarnessJson.Read<T>(Path.Combine(q.Source, name)); }
        var selected = Source<JsonElement>("result.json").GetProperty("selected");
        Assert.Equal(JsonValueKind.Object, selected.ValueKind); Assert.True(selected.GetProperty("eligible").GetBoolean());
        var variant = $"variant-{selected.GetProperty("variant").GetInt32():D2}";
        var priorScope = Source<LoadoutScope>(variant + "/scope.json");
        Assert.Equal(HarnessJson.Hash(priorScope.Execution), HarnessJson.Hash(ExecutionIdentity.Current()));
        var scope = priorScope with { Algorithm = Version };
        var cells = Source<Cell[]>("cells.json").Select(c => c with { Scenario = c.Scenario with { Seeds = [] } }).ToArray();
        Assert.Equal(Cells, cells.Length); Assert.Equal(Cells, cells.Select(c => c.Id).Distinct().Count());
        Assert.Equal(112, cells.Count(c => c.Kind == "retained-control"));
        Assert.Equal(114, cells.Count(c => c.Kind == "seventh-addition")); Assert.Equal(2, cells.Count(c => c.Kind == "level-control"));
        Assert.Equal(q.HistoryPin, HarnessJson.FileHash(Path.Combine(q.HistorySource, "files.json")));
        var historyFiles = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.HistorySource, "files.json"));
        Assert.Equal(historyFiles["seed-ledger.json"], HarnessJson.FileHash(Path.Combine(q.HistorySource, "seed-ledger.json")));
        var historical = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(q.HistorySource, "seed-ledger.json")));
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(2400)); var token = stop.Token;
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
            Save("protocol.json", new { version = Version, cells = Cells, samples = Samples, maximumFights = Fights,
                maximumSeconds = 2400, maximumBytes = MaximumBytes, retries = 0, freshSeeds = Samples,
                rule = "Approximate simultaneous 95% Bonferroni-Wilson intervals across all 228 cells. Every seven-Essence upper bound <= .50, some seven-Essence lower bound >= .10, every four-/six-Essence upper bound < .10. Observed intended > .50, observed control >= .10, or all intended upper bounds < .10 fail; otherwise unresolved evidence is Inconclusive. No pooling, reselection, extension or application." });
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
            Save("preflight.json", new { status = "PreparedNoFights", cells = Cells, historyCount = historical.Length, identicalProducingRuntime = true });
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token); VerifyInputs(); Check();
            var allocator = C.Allocator(q.Master, 0, Version);
            var reserved = TowerCompleteReservation.Reserve(q.Output, allocator, historical, MaximumBytes, token);
            TowerCompleteReservation.Verify(q.Output, reserved.Seeds, allocator, token);
            var seeds = reserved.Seeds.First.Concat(reserved.Seeds.Second).ToArray(); Assert.Equal(Samples, seeds.Length);
            Save("confirmation-seeds.json", seeds);
            var study = Path.Combine(q.Output, "study"); var archive = F.Archive(study, scope, Fights, q.Output, token);
            var rows = new List<Row>();
            foreach (var cell in cells)
            {
                var scenario = cell.Scenario with { Seeds = seeds }; var wins = 0; var draws = 0;
                foreach (var seed in seeds)
                {
                    Check(); Assert.True(++attempts <= Fights);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                    var trial = await archive.EvaluateAsync(Version, cell.Id, scenario, seed, token); completed++;
                    if (trial.Report.Succeeded) wins++;
                    if (trial.Report.Battle.Summary.ContentOutcome == BattleOutcome.Draw) draws++;
                }
                rows.Add(new(cell.Id, scenario.Party[0].Build.CharacterLevel, scenario.Party[0].Build.EssenceIds.Count,
                    wins, draws, Samples, TowerBalanceEvaluator.Wilson(wins, Samples, Cells)!));
            }
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(Fights, trials.Count);
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
            Save("result.json", new { status = "Floor11FamilyConfirmationComplete", fights = completed, rows, assessment,
                freshSeeds = Samples, historicalSeeds = historical.Length, exclusionUnion = historical.Length + Samples,
                retries = 0, seconds = clock.Elapsed.TotalSeconds,
                scope = "Fixed 228-cell family under the selected captured floor-11 setting. Approximate simultaneous coverage; no guarantee about unsearched recipes. No production application or search-algorithm claim." });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
