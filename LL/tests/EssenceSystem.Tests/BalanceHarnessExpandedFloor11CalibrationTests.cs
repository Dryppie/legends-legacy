using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessExpandedFloor11CalibrationTests
{
    private const string Version = "tower-floor11-expanded-calibration-v1";
    private const int Samples = 32, Cells = 228, VariantFights = Cells * Samples, Fights = 6 * VariantFights;
    private static readonly decimal[] Multipliers = [1.5m, 1.625m, 1.75m, 1.875m, 2m, 2.5m];
    private sealed record Request(string Version, string Output, string Source, string SourcePin, string Runtime,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> AssemblyHashes);
    private sealed record Cell(string Id, string SourceCase, string Kind, string? Addition, string Profile, TowerScenario Scenario);
    private sealed record Row(int Variant, decimal Multiplier, string Id, string SourceCase, string Kind, string? Addition,
        int Level, int EssenceSlots, string Profile, int Wins, int Draws, int Samples,
        decimal MeanGuardianHealth, double MeanDurationSeconds, int GainedWins, int LostWins);
    private sealed record Summary(int Variant, decimal Multiplier, decimal Health, decimal Offense,
        int BestIntendedWins, int BestSixLevel50Wins, int BestSixLevel60Wins, int BestFourWins, int IntendedAboveCeiling,
        IReadOnlyList<string> BestIntendedCells, bool ObservedIntendedBand, bool ObservedLowerBudgetSeparation, bool Eligible);
    private sealed class CalibrationFactAttribute : FactAttribute
    {
        public CalibrationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_EXPANDED_FLOOR11_CALIBRATION")))
                Skip = "Set LL_EXPANDED_FLOOR11_CALIBRATION for the frozen 43,776-fight calibration.";
        }
    }

    private static JsonNode VariantDocument(JsonNode original, decimal multiplier)
    {
        if (!Multipliers.Contains(multiplier)) throw new InvalidDataException("Multiplier outside frozen grid.");
        var copy = original.DeepClone();
        var scaling = copy["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 11)!["guardianScaling"]!;
        if (scaling["health"]!.GetValue<decimal>() != 4.35m || scaling["offense"]!.GetValue<decimal>() != 5.58m)
            throw new InvalidDataException("Changed floor-11 baseline.");
        scaling["health"] = 2.90m * multiplier; scaling["offense"] = 3.72m * multiplier;
        return copy;
    }

    private static Summary Summarize(int variant, IReadOnlyList<Row> rows)
    {
        if (variant < 0 || variant >= Multipliers.Length || rows.Count != Cells
            || rows.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != Cells
            || rows.Any(r => r.Variant != variant || r.Multiplier != Multipliers[variant] || r.Samples != Samples
                || r.Wins is < 0 or > Samples || r.Draws < 0 || r.Draws + r.Wins > Samples)
            || rows.Count(r => r.EssenceSlots == 7 && r.Level == 60) != 198
            || rows.Count(r => r.EssenceSlots == 6 && r.Level == 50) != 14
            || rows.Count(r => r.EssenceSlots == 6 && r.Level == 60) != 2
            || rows.Count(r => r.EssenceSlots == 4 && r.Level == 30) != 14)
            throw new InvalidDataException("Incomplete or changed expanded calibration family.");
        var intended = rows.Where(r => r.EssenceSlots == 7).ToArray();
        var best = intended.Max(r => r.Wins);
        var six50 = rows.Where(r => r.EssenceSlots == 6 && r.Level == 50).Max(r => r.Wins);
        var six60 = rows.Where(r => r.EssenceSlots == 6 && r.Level == 60).Max(r => r.Wins);
        var four = rows.Where(r => r.EssenceSlots == 4).Max(r => r.Wins);
        var band = best is >= 4 and <= 16; var separated = Math.Max(Math.Max(six50, six60), four) < 4;
        return new(variant, Multipliers[variant], 2.90m * Multipliers[variant], 3.72m * Multipliers[variant],
            best, six50, six60, four, intended.Count(r => r.Wins > 16),
            intended.Where(r => r.Wins == best).Select(r => r.Id).ToArray(), band, separated, band && separated);
    }

    [Theory]
    [InlineData(3, 0, 0, 0, false)] [InlineData(4, 3, 3, 3, true)] [InlineData(16, 0, 0, 0, true)]
    [InlineData(17, 0, 0, 0, false)] [InlineData(32, 0, 0, 0, false)]
    [InlineData(12, 4, 0, 0, false)] [InlineData(12, 0, 4, 0, false)] [InlineData(12, 0, 0, 4, false)]
    public void Selection_keeps_every_control_including_level_matched_six(int best, int six50, int six60, int four, bool eligible)
    {
        var rows = Enumerable.Range(0, Cells).Select(i => {
            var slots = i < 198 ? 7 : i < 214 ? 6 : 4;
            var level = i < 198 ? 60 : i < 212 ? 50 : i < 214 ? 60 : 30;
            return new Row(0, 1.5m, $"cell-{i}", "source", "retained-control", null, level, slots, "profile",
                slots == 7 ? i == 197 ? best : 0 : slots == 6 ? level == 50 ? six50 : six60 : four,
                0, Samples, 0, 0, 0, 0);
        }).ToArray();
        Assert.Equal(eligible, Summarize(0, rows).Eligible);
        Assert.Equal(best, Summarize(0, rows).BestIntendedWins);
        Assert.Throws<InvalidDataException>(() => Summarize(0, rows.Skip(1).ToArray()));
        var duplicate = rows.ToArray(); duplicate[0] = duplicate[1];
        Assert.Throws<InvalidDataException>(() => Summarize(0, duplicate));
        var missingLevelControl = rows.ToArray(); missingLevelControl[212] = rows[212] with { Level = 50 };
        Assert.Throws<InvalidDataException>(() => Summarize(0, missingLevelControl));
        rows[0] = rows[0] with { Samples = 31 };
        Assert.Throws<InvalidDataException>(() => Summarize(0, rows));
    }

    [Fact]
    public void Linked_variants_change_only_floor11_health_and_offense()
    {
        var original = JsonNode.Parse(File.ReadAllText(Path.Combine(TestContentPaths.FindApiRoot(), "Data", TowerBattleRunner.FloorFile)))!;
        var initialScaling = original["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 11)!["guardianScaling"]!;
        initialScaling["health"] = 4.35m; initialScaling["offense"] = 5.58m;
        var before = original.ToJsonString();
        foreach (var multiplier in Multipliers)
        {
            var copy = VariantDocument(original, multiplier);
            var scaling = copy["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 11)!["guardianScaling"]!;
            Assert.Equal(2.90m * multiplier, scaling["health"]!.GetValue<decimal>());
            Assert.Equal(3.72m * multiplier, scaling["offense"]!.GetValue<decimal>());
            scaling["health"] = 4.35m; scaling["offense"] = 5.58m;
            Assert.True(JsonNode.DeepEquals(original, copy));
        }
        Assert.Equal(before, original.ToJsonString());
        Assert.Throws<InvalidDataException>(() => VariantDocument(original, 4m));
        Assert.Throws<InvalidDataException>(() => VariantDocument(VariantDocument(original, 2m), 2m));
    }

    [CalibrationFact]
    public async Task Frozen_expanded_grid_keeps_all_upgrades_and_level_matched_controls()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_EXPANDED_FLOOR11_CALIBRATION")!);
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
        var cells = Source<Cell[]>("cells.json");
        Assert.Equal(Cells, cells.Length); Assert.Equal(112, cells.Count(c => c.Kind == "retained-control"));
        Assert.Equal(114, cells.Count(c => c.Kind == "seventh-addition")); Assert.Equal(2, cells.Count(c => c.Kind == "level-control"));
        Assert.All(cells, c => Assert.Equal(11, c.Scenario.FloorNumber));
        var seeds = cells[0].Scenario.Seeds.ToArray();
        Assert.Equal(Samples, seeds.Length); Assert.Equal(Samples, seeds.Distinct().Count());
        Assert.Equal(files["study/trials.jsonl"], HarnessJson.FileHash(Path.Combine(q.Source, "study/trials.jsonl")));
        var oldTrials = File.ReadLines(Path.Combine(q.Source, "study/trials.jsonl"))
            .Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!)
            .ToArray();
        Assert.Equal(VariantFights, oldTrials.Length);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2100)); var token = deadline.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output); Directory.CreateDirectory(q.Output);
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        var attempts = 0; var completed = 0; var matched = 0; var success = false; var lastStorageCheck = -1;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % (Samples * 8) == 0 && lastStorageCheck != completed) {
                Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1073741824); lastStorageCheck = completed; } }
        try
        {
            Save("request.json", q); Save("source-scope.json", scope); Save("cells.json", cells);
            Save("protocol.json", new { version = Version, multipliers = Multipliers, samples = Samples, cells = Cells,
                maximumFights = Fights, maximumSeconds = 2100, maximumBytes = 2L * 1073741824, newSeeds = 0, retries = 0,
                rule = "Lowest multiplier with strongest seven-Essence team at 4–16/32 and every four-/six-Essence control below 4/32, including both level-60 six-Essence controls. Historical-seed separation screen only; no balance acceptance. Complete all six settings without extension." });
            foreach (var pin in scope.Execution.AssemblyHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var path in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, path); if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(path, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var roots = new List<(string Root, LoadoutScope Scope, TowerBattleRunner Runner)>();
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate())
            {
                for (var i = 0; i < Multipliers.Length; i++)
                {
                    Check(); var root = Path.Combine(q.Output, $"variant-{i:D2}"); var contentRoot = Path.Combine(root, "content");
                    var hashes = new SortedDictionary<string, string>(TowerBundle.CopyContent(Path.Combine(q.Source, "content"), contentRoot, token).ToDictionary(p => p.Key, p => p.Value), StringComparer.Ordinal);
                    Assert.Equal(HarnessJson.Hash(scope.ContentHashes), HarnessJson.Hash(hashes));
                    var path = Path.Combine(contentRoot, "Data", TowerBattleRunner.FloorFile);
                    var variant = VariantDocument(JsonNode.Parse(File.ReadAllText(path))!, Multipliers[i]);
                    if (i > 0) { File.WriteAllText(path, variant.ToJsonString(HarnessJson.Options)); hashes[TowerBattleRunner.FloorFile] = HarnessJson.FileHash(path); }
                    var variantScope = scope with { ContentHashes = hashes };
                    TowerBundle.WriteSettings(Path.Combine(contentRoot, "appsettings.json"), scope.Settings);
                    HarnessJson.WriteNew(Path.Combine(root, "scope.json"), variantScope);
                    var runner = new TowerBattleRunner(contentRoot, OfflineContent.ForTower(contentRoot, scope.Settings));
                    foreach (var cell in cells)
                    {
                        Assert.Equal(seeds, cell.Scenario.Seeds);
                        _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario, seeds[0], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks), token);
                    }
                    roots.Add((root, variantScope, runner));
                }
                for (var n = 0; n < oldTrials.Length; n++)
                {
                    Assert.Equal(cells[n / Samples].Id, oldTrials[n].Stage);
                    Assert.Equal(seeds[n % Samples], oldTrials[n].Seed);
                    Assert.Equal(oldTrials[n].InputHash, HarnessJson.Hash(roots[0].Runner.CreateInput(cells[n / Samples].Scenario,
                        seeds[n % Samples], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks)));
                }
            }
            Save("preflight.json", new { status = "PreparedNoFights", variants = 6, cells = Cells * 6, historicalInputsMatched = VariantFights });
            VerifyInputs(); Check(); var rows = new List<Row>(); var baselines = new Dictionary<string, bool[]>();
            for (var i = 0; i < roots.Count; i++)
            {
                var (root, variantScope, runner) = roots[i]; var study = Path.Combine(root, "study");
                var archive = F.Archive(study, variantScope, VariantFights, root, token);
                foreach (var (cell, cellIndex) in cells.Select((c, n) => (c, n)))
                {
                    var reports = new List<TowerBattleReport>(); var stage = cell.Id;
                    foreach (var (seed, seedIndex) in seeds.Select((s, n) => (s, n)))
                    {
                        Check(); Assert.True(++attempts <= Fights);
                        TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                        var trial = await archive.EvaluateAsync(Version, stage, cell.Scenario, seed, token);
                        completed++; reports.Add(trial.Report);
                        if (i == 0)
                        {
                            var old = oldTrials[cellIndex * Samples + seedIndex]; var name = "study/battles/" + old.Id + ".json.gz";
                            Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name)));
                            Assert.Equal(old.InputHash, trial.Trial.InputHash);
                            Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(Path.Combine(q.Source, "study"), old.Id, oldScope.ReportStorage)), HarnessJson.Hash(trial.Report)); matched++;
                        }
                        else Assert.Equal(VariantFights, matched);
                    }
                    var wins = reports.Select(r => r.Succeeded).ToArray(); if (i == 0) baselines.Add(stage, wins);
                    var pairs = wins.Zip(baselines[stage]).ToArray();
                    rows.Add(new(i, Multipliers[i], cell.Id, cell.SourceCase, cell.Kind, cell.Addition,
                        cell.Scenario.Party[0].Build.CharacterLevel, cell.Scenario.Party[0].Build.EssenceIds.Count, cell.Profile,
                        wins.Count(w => w), reports.Count(r => r.Battle.Summary.ContentOutcome == BattleOutcome.Draw), Samples,
                        reports.Average(r => r.GuardianHealthRemainingPercent), reports.Average(r => r.Battle.Summary.DurationSeconds),
                        pairs.Count(p => p.First && !p.Second), pairs.Count(p => !p.First && p.Second)));
                }
                Assert.Equal(0, archive.CacheHits); F.Seal(study);
                var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(VariantFights, trials.Count);
                foreach (var (trial, n) in trials.Select((t, n) => (t, n)))
                {
                    Check(); var cell = cells[n / Samples]; Assert.Equal(seeds[n % Samples], trial.Seed);
                    Assert.Equal(cell.Id, trial.Stage); Assert.Equal(HarnessJson.Hash(cell.Scenario), trial.Recipe);
                    var input = runner.CreateInput(cell.Scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                    Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(variantScope, Version, input), trial.CacheKey);
                }
                foreach (var pin in variantScope.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "content", "Data", pin.Key)));
                var summary = Summarize(i, rows.Where(r => r.Variant == i).ToArray());
                HarnessJson.WriteNew(Path.Combine(root, "summary.json"), summary); Console.WriteLine(JsonSerializer.Serialize(summary, HarnessJson.Options));
            }
            Assert.Equal(Fights, completed); VerifyInputs();
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            var summaries = Enumerable.Range(0, 6).Select(i => Summarize(i, rows.Where(r => r.Variant == i).ToArray())).ToArray();
            var selected = summaries.FirstOrDefault(s => s.Eligible);
            Save("result.json", new { status = "ExpandedFloor11CalibrationComplete", fights = completed, rows, summaries, selected,
                decision = selected is null ? "NoSeparatingSettingInGrid" : "DiagnosticCalibrationCandidate", runtimeParityReports = matched,
                newSeeds = 0, confirmedTeams = 0, searchRuns = 0, retries = 0, seconds = watch.Elapsed.TotalSeconds,
                balanceAcceptance = "NotAssessedHistoricalSeeds" });
            Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1073741824); Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
