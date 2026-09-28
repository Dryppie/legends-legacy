using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessFloor11CalibrationTests
{
    private const string Version = "tower-floor11-linked-calibration-v1";
    private const int Samples = 32, Cells = 112, VariantFights = Cells * Samples, Fights = 6 * VariantFights;
    private static readonly decimal[] Multipliers = [1m, 1.125m, 1.25m, 1.5m, 2m, 3m];
    private sealed record Request(string Version, string Output, string Source, string SourcePin, string Runtime,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> AssemblyHashes);
    private sealed record Cell(string Case, string Purpose, int Floor, int EssenceSlots, string Profile, TowerScenario Scenario);
    private sealed record Row(int Variant, decimal Multiplier, string Case, string Purpose, int EssenceSlots, string Profile,
        int Wins, int Draws, int Samples, decimal MeanGuardianHealth, double MeanDurationSeconds, int GainedWins, int LostWins);
    private sealed record Summary(int Variant, decimal Multiplier, decimal Health, decimal Offense,
        int BestIntendedWins, int BestSixWins, int BestFourWins, int IntendedAboveCeiling,
        IReadOnlyList<string> BestIntendedCells, bool ObservedIntendedBand, bool ObservedLowerBudgetSeparation, bool Eligible);
    private sealed class CalibrationFactAttribute : FactAttribute
    {
        public CalibrationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_FLOOR11_CALIBRATION")))
                Skip = "Set LL_FLOOR11_CALIBRATION for the frozen 21,504-fight calibration.";
        }
    }

    private static JsonNode VariantDocument(JsonNode original, decimal multiplier)
    {
        if (!Multipliers.Contains(multiplier)) throw new InvalidDataException("Multiplier outside frozen grid.");
        var copy = original.DeepClone();
        var scaling = copy["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 11)!["guardianScaling"]!;
        if (scaling["health"]!.GetValue<decimal>() != 2.90m || scaling["offense"]!.GetValue<decimal>() != 3.72m)
            throw new InvalidDataException("Changed floor-11 baseline.");
        scaling["health"] = 2.90m * multiplier; scaling["offense"] = 3.72m * multiplier;
        return copy;
    }

    private static Summary Summarize(int variant, IReadOnlyList<Row> rows)
    {
        if (variant < 0 || variant >= Multipliers.Length || rows.Count != Cells
            || rows.Select(r => (r.Case, r.Profile)).Distinct().Count() != Cells
            || rows.Any(r => r.Variant != variant || r.Multiplier != Multipliers[variant] || r.Samples != Samples
                || r.Wins is < 0 or > Samples || r.Draws < 0 || r.Draws + r.Wins > Samples
                || r.Purpose != (r.EssenceSlots == 7 ? "intended-progression" : "diagnostic"))
            || rows.Count(r => r.EssenceSlots == 7) != 84 || rows.Count(r => r.EssenceSlots == 6) != 14
            || rows.Count(r => r.EssenceSlots == 4) != 14)
            throw new InvalidDataException("Incomplete or changed calibration family.");
        var intended = rows.Where(r => r.EssenceSlots == 7).ToArray();
        var best = intended.Max(r => r.Wins); var six = rows.Where(r => r.EssenceSlots == 6).Max(r => r.Wins);
        var four = rows.Where(r => r.EssenceSlots == 4).Max(r => r.Wins);
        var band = best is >= 4 and <= 16; var separated = Math.Max(six, four) < 4;
        return new(variant, Multipliers[variant], 2.90m * Multipliers[variant], 3.72m * Multipliers[variant],
            best, six, four, intended.Count(r => r.Wins > 16),
            intended.Where(r => r.Wins == best).Select(r => $"{r.Case}/{r.Profile}").ToArray(), band, separated, band && separated);
    }

    [Theory]
    [InlineData(3, 0, 0, false)] [InlineData(4, 3, 3, true)] [InlineData(16, 0, 0, true)]
    [InlineData(17, 0, 0, false)] [InlineData(32, 0, 0, false)]
    [InlineData(12, 4, 0, false)] [InlineData(12, 0, 4, false)]
    public void Screen_requires_the_strongest_intended_team_in_band_and_both_lower_budgets_below_it(int best, int six, int four, bool eligible)
    {
        var rows = Enumerable.Range(0, Cells).Select(i => {
            var slots = i < 84 ? 7 : i < 98 ? 6 : 4;
            return new Row(0, 1m, $"case-{i / 7}", slots == 7 ? "intended-progression" : "diagnostic", slots,
                $"profile-{i % 7}", slots == 7 ? i == 0 ? best : 0 : slots == 6 ? six : four, 0, Samples, 0, 0, 0, 0);
        }).ToArray();
        Assert.Equal(eligible, Summarize(0, rows).Eligible);
        Assert.Equal(best, Summarize(0, rows).BestIntendedWins);
        Assert.Throws<InvalidDataException>(() => Summarize(0, rows.Skip(1).ToArray()));
        rows[0] = rows[0] with { Samples = 31 };
        Assert.Throws<InvalidDataException>(() => Summarize(0, rows));
    }

    [Fact]
    public void Linked_variants_change_only_floor11_health_and_offense()
    {
        var original = JsonNode.Parse(File.ReadAllText(Path.Combine(TestContentPaths.FindApiRoot(), "Data", TowerBattleRunner.FloorFile)))!;
        var before = original.ToJsonString();
        foreach (var multiplier in Multipliers)
        {
            var copy = VariantDocument(original, multiplier);
            var scaling = copy["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 11)!["guardianScaling"]!;
            Assert.Equal(2.90m * multiplier, scaling["health"]!.GetValue<decimal>());
            Assert.Equal(3.72m * multiplier, scaling["offense"]!.GetValue<decimal>());
            scaling["health"] = 2.90m; scaling["offense"] = 3.72m;
            Assert.True(JsonNode.DeepEquals(original, copy));
        }
        Assert.Equal(before, original.ToJsonString());
        Assert.Throws<InvalidDataException>(() => VariantDocument(original, 4m));
        Assert.Throws<InvalidDataException>(() => VariantDocument(VariantDocument(original, 2m), 3m));
    }

    [CalibrationFact]
    public async Task Frozen_linked_grid_keeps_every_floor11_team_and_gear_control()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_FLOOR11_CALIBRATION")!);
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
        var cells = Source<Cell[]>("cells.json").Where(c => c.Floor == 11).ToArray();
        Assert.Equal(Cells, cells.Length); Assert.Equal(16, cells.Select(c => c.Case).Distinct().Count());
        Assert.Equal(84, cells.Count(c => c.EssenceSlots == 7)); Assert.Equal(14, cells.Count(c => c.EssenceSlots == 6)); Assert.Equal(14, cells.Count(c => c.EssenceSlots == 4));
        var seeds = Source<JsonElement>("request.json").GetProperty("seeds").Deserialize<int[]>(HarnessJson.Options)!;
        Assert.Equal(Samples, seeds.Length); Assert.Equal(Samples, seeds.Distinct().Count());
        Assert.Equal(files["study/trials.jsonl"], HarnessJson.FileHash(Path.Combine(q.Source, "study/trials.jsonl")));
        var oldTrials = File.ReadLines(Path.Combine(q.Source, "study/trials.jsonl"))
            .Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!)
            .Where(t => t.Stage.StartsWith("floor11-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(VariantFights, oldTrials.Length);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = deadline.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output); Directory.CreateDirectory(q.Output);
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        var attempts = 0; var completed = 0; var matched = 0; var success = false; var lastStorageCheck = -1;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % Samples == 0 && lastStorageCheck != completed) {
                Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1073741824); lastStorageCheck = completed; } }
        try
        {
            Save("request.json", q); Save("source-scope.json", scope); Save("cells.json", cells);
            Save("protocol.json", new { version = Version, multipliers = Multipliers, samples = Samples, cells = Cells,
                maximumFights = Fights, maximumSeconds = 840, maximumBytes = 2L * 1073741824, newSeeds = 0, retries = 0,
                rule = "Lowest multiplier with strongest intended team at 4–16/32 and every four-/six-Essence control below 4/32. Historical-seed screen only; no balance acceptance. Complete all six settings without extension." });
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
                    Assert.Equal($"{cells[n / Samples].Case}/{cells[n / Samples].Profile}", oldTrials[n].Stage);
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
                    var reports = new List<TowerBattleReport>(); var stage = $"{cell.Case}/{cell.Profile}";
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
                    rows.Add(new(i, Multipliers[i], cell.Case, cell.Purpose, cell.EssenceSlots, cell.Profile,
                        wins.Count(w => w), reports.Count(r => r.Battle.Summary.ContentOutcome == BattleOutcome.Draw), Samples,
                        reports.Average(r => r.GuardianHealthRemainingPercent), reports.Average(r => r.Battle.Summary.DurationSeconds),
                        pairs.Count(p => p.First && !p.Second), pairs.Count(p => !p.First && p.Second)));
                }
                Assert.Equal(0, archive.CacheHits); F.Seal(study);
                var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(VariantFights, trials.Count);
                foreach (var (trial, n) in trials.Select((t, n) => (t, n)))
                {
                    Check(); var cell = cells[n / Samples]; Assert.Equal(seeds[n % Samples], trial.Seed);
                    Assert.Equal($"{cell.Case}/{cell.Profile}", trial.Stage); Assert.Equal(HarnessJson.Hash(cell.Scenario), trial.Recipe);
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
            Save("result.json", new { status = "Floor11CalibrationComplete", fights = completed, rows, summaries, selected,
                decision = selected is null ? "NoSeparatingSettingInGrid" : "DiagnosticCalibrationCandidate", runtimeParityReports = matched,
                newSeeds = 0, confirmedTeams = 0, searchRuns = 0, retries = 0, seconds = watch.Elapsed.TotalSeconds,
                balanceAcceptance = "NotAssessedHistoricalSeeds" });
            Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1073741824); Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
