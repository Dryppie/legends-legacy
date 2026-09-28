using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Items;
using C = EssenceSystem.Tests.BalanceHarnessAffinityTeamConfirmationTests;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessFloor10FamilyConfirmationTests
{
    private const string Version = "tower-floor10-fixed-family-confirmation-v1";
    private const string SourcePin = "b866bb74e5aa897dcd241423208fe53a47bdf5a583846434a07c460f6f6a015f";
    private const string HistoryPin = "41a93659a05583d0cf55d91b98c2ef4442bfeba117ab1fea7d9daa0131827378";
    private const int Samples = 256, Cells = 21, Fights = Samples * Cells;
    private const long MaximumBytes = 2L * 1073741824;
    private static readonly string[] Cases = ["floor10-authored", "floor10-retained-1", "floor10-retained-2"];
    private static readonly string[] Profiles = ["baseline", "precision", "ability-haste", "restorer-specialization",
        "armor-and-health", "resistance-and-health", "health-and-regeneration"];
    private static IEnumerable<string> CellIds => Cases.SelectMany(c => Profiles.Select(p => c + "/" + p));
    private sealed record Request(string Version, string Source, string SourcePin, string Output, string Registry,
        string Runtime, string HistorySource, string HistoryPin, int Master,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> RequiredHistory,
        IReadOnlyDictionary<string, string> Recoveries, IReadOnlyDictionary<string, string> RecoveryHashes);
    private sealed record Cell(string Case, string Purpose, int Floor, int EssenceSlots, string Profile, TowerScenario Scenario);
    private sealed record Row(string Id, int Level, int EssenceSlots, int Wins, int Draws, int Samples, RateEstimate Adjusted);
    private sealed class ConfirmationFactAttribute : FactAttribute
    {
        public ConfirmationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_FLOOR10_FAMILY_CONFIRMATION")))
                Skip = "Set LL_FLOOR10_FAMILY_CONFIRMATION for one frozen 5,376-fight fresh confirmation.";
        }
    }

    private static JsonElement SelectSetting(JsonElement calibration)
    {
        if (calibration.GetProperty("status").GetString() != "RefinedFloor10CalibrationComplete")
            throw new InvalidDataException("Completed finer calibration required.");
        var matches = calibration.GetProperty("summaries").EnumerateArray()
            .Where(s => s.GetProperty("multiplier").GetDecimal() == 7.75m).ToArray();
        if (matches.Length != 1 || matches[0].GetProperty("variant").GetInt32() != 7
            || !matches[0].GetProperty("eligible").GetBoolean()
            || matches[0].GetProperty("health").GetDecimal() != 12.71m
            || matches[0].GetProperty("offense").GetDecimal() != 7.13m
            || HarnessJson.Hash(matches[0]) != HarnessJson.Hash(calibration.GetProperty("selected")))
            throw new InvalidDataException("The exact selected 7.75 setting is required.");
        return matches[0].Clone();
    }

    [Fact]
    public void Confirmation_requires_the_exact_selected_setting_and_rejects_substitutions()
    {
        const string valid = """
            {"status":"RefinedFloor10CalibrationComplete",
             "selected":{"variant":7,"multiplier":7.75,"eligible":true,"health":12.71,"offense":7.13},
             "summaries":[{"variant":6,"multiplier":7.5,"eligible":false,"health":12.3,"offense":6.9},
                          {"variant":7,"multiplier":7.75,"eligible":true,"health":12.71,"offense":7.13}]}
            """;
        JsonElement Parse(string json) => JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal(7.75m, SelectSetting(Parse(valid)).GetProperty("multiplier").GetDecimal());
        foreach (var changed in new[] { valid.Replace("7.75", "7.625"), valid.Replace("12.71", "12.7"),
            valid.Replace("7.13", "7.14"), valid.Replace("true", "false"), valid.Replace("Complete", "Incomplete"),
            valid.Replace("7.5", "7.75"), valid.Replace("\"selected\":{\"variant\":7", "\"selected\":{\"variant\":6") })
            Assert.Throws<InvalidDataException>(() => SelectSetting(Parse(changed)));
    }

    private static void ValidateCells(IReadOnlyList<Cell> cells)
    {
        Assert.Equal(CellIds, cells.Select(c => c.Case + "/" + c.Profile));
        foreach (var cell in cells)
        {
            Assert.Equal(10, cell.Floor); Assert.Equal(10, cell.Scenario.FloorNumber);
            Assert.Equal(6, cell.EssenceSlots); Assert.Equal("intended-progression", cell.Purpose);
            Assert.Equal(15, cell.Scenario.Party.Count);
            foreach (var member in cell.Scenario.Party)
            {
                Assert.Equal(50, member.Build.CharacterLevel); Assert.Equal(2, member.Build.Tier);
                Assert.Equal(5, member.Build.Rank); Assert.Equal(ItemQuality.Masterpiece, member.Build.Quality);
                Assert.Equal(6, member.Build.EssenceIds.Count);
                Assert.All(member.Build.Equipment, e => Assert.EndsWith(".rarity.legendary", e.DefinitionId));
            }
        }
    }

    private static string Assess(IReadOnlyList<Row> rows)
    {
        if (!rows.Select(r => r.Id).SequenceEqual(CellIds)
            || rows.Any(r => r.EssenceSlots != 6 || r.Level != 50 || r.Samples != Samples
                || r.Wins is < 0 or > Samples || r.Draws < 0 || r.Wins + r.Draws > Samples
                || r.Adjusted != TowerBalanceEvaluator.Wilson(r.Wins, Samples, Cells)))
            throw new InvalidDataException("Full fixed family and recomputable simultaneous intervals required.");
        if (rows.Any(r => r.Wins * 2 > Samples) || rows.All(r => r.Adjusted.Upper < .1)) return "Fail";
        return rows.All(r => r.Adjusted.Upper <= .5) && rows.Any(r => r.Adjusted.Lower >= .1) ? "Pass" : "Inconclusive";
    }

    [Theory]
    [InlineData(80, "Pass")]
    [InlineData(128, "Inconclusive")]
    [InlineData(129, "Fail")]
    [InlineData(0, "Fail")]
    [InlineData(26, "Inconclusive")]
    public void Simultaneous_assessment_requires_every_combination_and_keeps_uncertainty(int best, string expected)
    {
        var rows = CellIds.Select((id, i) => {
            var wins = i == Cells - 1 ? best : 0;
            return new Row(id, 50, 6, wins, 0, Samples, TowerBalanceEvaluator.Wilson(wins, Samples, Cells)!);
        }).ToArray();
        Assert.Equal(expected, Assess(rows));
        Assert.Throws<InvalidDataException>(() => Assess(rows.Skip(1).ToArray()));
        var duplicate = rows.ToArray(); duplicate[0] = duplicate[1];
        Assert.Throws<InvalidDataException>(() => Assess(duplicate));
        foreach (var changed in new[] { rows[0] with { Level = 60 }, rows[0] with { EssenceSlots = 7 },
            rows[0] with { Samples = 255 }, rows[0] with { Wins = 256, Draws = 1 },
            rows[0] with { Adjusted = TowerBalanceEvaluator.Wilson(0, Samples, 1)! } })
        {
            var invalid = rows.ToArray(); invalid[0] = changed;
            Assert.Throws<InvalidDataException>(() => Assess(invalid));
        }
    }

    [ConfirmationFact]
    public async Task Selected_floor10_setting_receives_one_complete_fresh_family_panel()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_FLOOR10_FAMILY_CONFIRMATION")!);
        Assert.Equal(Version, q.Version); Assert.Equal(SourcePin, q.SourcePin); Assert.Equal(HistoryPin, q.HistoryPin);
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
        Assert.Equal(HarnessJson.Hash(priorScope.Execution), HarnessJson.Hash(ExecutionIdentity.Current()));
        var scope = priorScope with { Algorithm = Version };
        var cells = Source<Cell[]>("cells.json").Select(c => c with { Scenario = c.Scenario with { Seeds = [] } }).ToArray();
        ValidateCells(cells);
        Assert.Equal(q.HistoryPin, HarnessJson.FileHash(Path.Combine(q.HistorySource, "files.json")));
        var historyFiles = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.HistorySource, "files.json"));
        Assert.Equal(historyFiles["seed-ledger.json"], HarnessJson.FileHash(Path.Combine(q.HistorySource, "seed-ledger.json")));
        var priorResultPath = Path.Combine(q.HistorySource, "result.json");
        Assert.Equal(historyFiles["result.json"], HarnessJson.FileHash(priorResultPath));
        var priorResult = HarnessJson.Read<JsonElement>(priorResultPath);
        Assert.Equal("Floor11FamilyConfirmationComplete", priorResult.GetProperty("status").GetString());
        Assert.Equal("Pass", priorResult.GetProperty("assessment").GetString());
        Assert.Equal(58368, priorResult.GetProperty("fights").GetInt32());
        Assert.Equal(834807, priorResult.GetProperty("exclusionUnion").GetInt32());
        var historical = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(q.HistorySource, "seed-ledger.json")));
        Assert.Equal(834807, historical.Length);
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = stop.Token;
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
            Save("history-source-result.json", priorResult);
            Save("protocol.json", new { version = Version, cells = Cells, samples = Samples, maximumFights = Fights,
                maximumSeconds = 840, maximumBytes = MaximumBytes, retries = 0, freshSeeds = Samples,
                rule = "Approximate simultaneous 95% Bonferroni-Wilson intervals across all 21 cells. Every upper bound <= .50 and some lower bound >= .10. Any observed rate > .50 or all upper bounds < .10 fail; otherwise unresolved evidence is Inconclusive. No pooling, reselection, extension or application." });
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
                    var trial = await archive.EvaluateAsync(Version, cell.Case + "/" + cell.Profile, scenario, seed, token); completed++;
                    if (trial.Report.Succeeded) wins++;
                    if (trial.Report.Battle.Summary.ContentOutcome == BattleOutcome.Draw) draws++;
                }
                rows.Add(new(cell.Case + "/" + cell.Profile, scenario.Party[0].Build.CharacterLevel, scenario.Party[0].Build.EssenceIds.Count,
                    wins, draws, Samples, TowerBalanceEvaluator.Wilson(wins, Samples, Cells)!));
            }
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(Fights, trials.Count);
            foreach (var (trial, n) in trials.Select((t, n) => (t, n)))
            {
                Check(); var cell = cells[n / Samples]; var scenario = cell.Scenario with { Seeds = seeds };
                Assert.Equal(seeds[n % Samples], trial.Seed); Assert.Equal(cell.Case + "/" + cell.Profile, trial.Stage); Assert.Equal(HarnessJson.Hash(scenario), trial.Recipe);
                var input = runner.CreateInput(scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(scope, Version, input), trial.CacheKey);
            }
            TowerCompleteReservation.Verify(q.Output, reserved.Seeds, allocator, token);
            TowerRefinementComparisonLaunch.Recheck(q.Registry, q.Output, history.Files, token); VerifyInputs();
            foreach (var pin in scope.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            var assessment = Assess(rows);
            Save("result.json", new { status = "Floor10FamilyConfirmationComplete", fights = completed, rows, assessment,
                freshSeeds = Samples, historicalSeeds = historical.Length, exclusionUnion = historical.Length + Samples,
                selectedFactor = 7.75m, historyManifest = q.HistoryPin,
                retries = 0, seconds = clock.Elapsed.TotalSeconds,
                scope = "Fixed 21-cell six-Essence floor-10 family at selected factor 7.75. No historical pooling. Approximate simultaneous coverage for this study, not the campaign or unsearched recipes. No lower-Essence controls, necessity claim, production application or search-algorithm claim." });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
