using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Items;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessRefinedFloor10CalibrationTests
{
    private const string Version = "tower-floor10-refined-calibration-v1";
    private const string SourcePin = "c67912fac6fa2f72b82676859c9f2cb525585d0cd6200f604b86d808c432bcf1";
    private const int Samples = 32, Cells = 21, VariantFights = Cells * Samples, Fights = 9 * VariantFights;
    private static readonly decimal[] Multipliers = [6m, 6.25m, 6.5m, 6.75m, 7m, 7.25m, 7.5m, 7.75m, 8m];
    private static readonly int[] ExecutionOrder = [0, 8, 1, 2, 3, 4, 5, 6, 7];
    private static int BoundarySource(int variant) => variant switch { 0 => 5, 8 => 6, _ => throw new InvalidDataException("Not a boundary setting.") };
    private static void RequirePriorParity(int variant, int matched)
    {
        if (variant < 0 || variant >= Multipliers.Length || matched != (variant == 0 ? 0 : variant == 8 ? VariantFights : 2 * VariantFights))
            throw new InvalidDataException("Complete both boundary replays before intermediate settings.");
    }
    private sealed record Request(string Version, string Output, string Source, string SourcePin, string Runtime,
        string ApiRoot, IReadOnlyDictionary<string, string> CurrentContentHashes,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> AssemblyHashes);
    private sealed record Cell(string Case, string Purpose, int Floor, int EssenceSlots, string Profile, TowerScenario Scenario);
    private sealed record Row(int Variant, decimal Multiplier, string Case, string Purpose, int EssenceSlots, string Profile,
        int Wins, int Draws, int Samples, decimal MeanGuardianHealth, double MeanDurationSeconds, int GainedWins, int LostWins);
    private sealed record Summary(int Variant, decimal Multiplier, decimal Health, decimal Offense,
        int BestIntendedWins, int IntendedAboveCeiling, IReadOnlyList<string> BestIntendedCells, bool Eligible);
    private sealed class CalibrationFactAttribute : FactAttribute
    {
        public CalibrationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_REFINED_FLOOR10_CALIBRATION")))
                Skip = "Set LL_REFINED_FLOOR10_CALIBRATION for the frozen 6,048-fight historical-seed calibration.";
        }
    }

    private static JsonNode VariantDocument(JsonNode original, decimal multiplier)
    {
        if (!Multipliers.Contains(multiplier)) throw new InvalidDataException("Multiplier outside frozen grid.");
        var copy = original.DeepClone();
        var scaling = copy["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 10)!["guardianScaling"]!;
        if (scaling["health"]!.GetValue<decimal>() != 1.64m || scaling["offense"]!.GetValue<decimal>() != .92m)
            throw new InvalidDataException("Changed floor-10 baseline.");
        scaling["health"] = 1.64m * multiplier; scaling["offense"] = .92m * multiplier;
        return copy;
    }

    private static void ValidateCells(IReadOnlyList<Cell> cells)
    {
        string[] cases = ["floor10-authored", "floor10-retained-1", "floor10-retained-2"];
        string[] profiles = ["baseline", "precision", "ability-haste", "restorer-specialization", "armor-and-health", "resistance-and-health", "health-and-regeneration"];
        Assert.Equal(cases.SelectMany(c => profiles.Select(p => c + "/" + p)), cells.Select(c => c.Case + "/" + c.Profile));
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

    private static Summary Summarize(int variant, IReadOnlyList<Row> rows)
    {
        if (variant < 0 || variant >= Multipliers.Length || rows.Count != Cells
            || rows.Select(r => (r.Case, r.Profile)).Distinct().Count() != Cells
            || rows.Any(r => r.Variant != variant || r.Multiplier != Multipliers[variant] || r.Samples != Samples
                || r.Wins is < 0 or > Samples || r.Draws < 0 || r.Draws + r.Wins > Samples
                || r.EssenceSlots != 6 || r.Purpose != "intended-progression"))
            throw new InvalidDataException("Incomplete or changed calibration family.");
        var best = rows.Max(r => r.Wins);
        return new(variant, Multipliers[variant], 1.64m * Multipliers[variant], .92m * Multipliers[variant],
            best, rows.Count(r => r.Wins > 16), rows.Where(r => r.Wins == best).Select(r => $"{r.Case}/{r.Profile}").ToArray(),
            best is >= 4 and <= 16);
    }

    [Theory]
    [InlineData(3, false)] [InlineData(4, true)] [InlineData(16, true)] [InlineData(17, false)] [InlineData(32, false)]
    public void Screen_keeps_the_strongest_team_and_rejects_missing_or_invalid_evidence(int best, bool eligible)
    {
        var rows = Enumerable.Range(0, Cells).Select(i => new Row(0, 6m, $"case-{i / 7}", "intended-progression", 6,
            $"profile-{i % 7}", i == Cells - 1 ? best : 0, 0, Samples, 0, 0, 0, 0)).ToArray();
        Assert.Equal(eligible, Summarize(0, rows).Eligible);
        Assert.Equal(best, Summarize(0, rows).BestIntendedWins);
        Assert.Throws<InvalidDataException>(() => Summarize(0, rows.Skip(1).ToArray()));
        var duplicate = rows.ToArray(); duplicate[0] = duplicate[1];
        Assert.Throws<InvalidDataException>(() => Summarize(0, duplicate));
        rows[0] = rows[0] with { Samples = 31 };
        Assert.Throws<InvalidDataException>(() => Summarize(0, rows));
        rows[0] = rows[0] with { Samples = 32, Wins = 32, Draws = 1 };
        Assert.Throws<InvalidDataException>(() => Summarize(0, rows));
    }

    [Fact]
    public void Linked_variants_preserve_other_floors_and_all_other_fields()
    {
        var original = JsonNode.Parse("""
            {"floors":[{"floorNumber":10,"guardianScaling":{"health":1.64,"offense":0.92,"defense":2.29},"requiredSlots":15},
                       {"floorNumber":11,"guardianScaling":{"health":6.525,"offense":8.37}}]}
            """)!;
        var before = original.ToJsonString();
        foreach (var multiplier in Multipliers)
        {
            var copy = VariantDocument(original, multiplier);
            var scaling = copy["floors"]![0]!["guardianScaling"]!;
            Assert.Equal(1.64m * multiplier, scaling["health"]!.GetValue<decimal>());
            Assert.Equal(.92m * multiplier, scaling["offense"]!.GetValue<decimal>());
            scaling["health"] = 1.64m; scaling["offense"] = .92m;
            Assert.True(JsonNode.DeepEquals(original, copy));
        }
        Assert.Equal(before, original.ToJsonString());
        Assert.Throws<InvalidDataException>(() => VariantDocument(original, 5m));
        Assert.Throws<InvalidDataException>(() => VariantDocument(VariantDocument(original, 6m), 8m));
    }

    [Theory]
    [InlineData(0, 0, true)] [InlineData(8, 672, true)] [InlineData(8, 0, false)]
    [InlineData(1, 672, false)] [InlineData(1, 1344, true)] [InlineData(7, 1344, true)]
    [InlineData(9, 1344, false)]
    public void Intermediate_settings_require_both_complete_boundary_panels(int variant, int matched, bool allowed)
    {
        Assert.Equal(Enumerable.Range(0, 9), ExecutionOrder.Order());
        Assert.Equal(new[] { 0, 8 }, ExecutionOrder.Take(2));
        if (allowed) RequirePriorParity(variant, matched);
        else Assert.Throws<InvalidDataException>(() => RequirePriorParity(variant, matched));
    }

    [CalibrationFact]
    public async Task Frozen_finer_grid_replays_both_boundaries_before_all_intermediate_settings()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_REFINED_FLOOR10_CALIBRATION")!);
        Assert.Equal(Version, q.Version); Assert.False(Path.Exists(q.Output));
        foreach (var path in q.InputHashes.Keys.Append(q.Output).Append(q.Source).Append(q.Runtime).Append(q.ApiRoot)) TowerProposalStudy.Unlinked(path);
        void VerifyInputs() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        VerifyInputs(); Assert.Equal(q.SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name) { Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name))); return HarnessJson.Read<T>(Path.Combine(q.Source, name)); }
        var oldScope = Source<LoadoutScope>("source-scope.json");
        var scope = oldScope with { Algorithm = Version, Execution = ExecutionIdentity.Current(), ContentHashes = q.CurrentContentHashes };
        Assert.Equal(HarnessJson.Hash(q.AssemblyHashes), HarnessJson.Hash(scope.Execution.AssemblyHashes));
        Assert.Equal(new TowerBalanceSelection(18, 4, "healing-v1"), scope.Settings.Balance);
        Assert.Equal(HarnessJson.Hash(scope.Settings), HarnessJson.Hash(TowerBundle.ReadSettings(q.ApiRoot)));
        var cells = Source<Cell[]>("cells.json").Where(c => c.Floor == 10).ToArray();
        ValidateCells(cells);
        Assert.Equal(SourcePin, q.SourcePin);
        var seeds = cells[0].Scenario.Seeds.ToArray();
        Assert.Equal(Samples, seeds.Length); Assert.Equal(Samples, seeds.Distinct().Count());
        var oldTrials = new Dictionary<int, LoadoutTrial[]>();
        foreach (var boundary in new[] { 0, 8 })
        {
            var name = $"variant-{BoundarySource(boundary):D2}/study/trials.jsonl";
            Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name)));
            var trials = File.ReadLines(Path.Combine(q.Source, name))
                .Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!).ToArray();
            Assert.Equal(VariantFights, trials.Length); oldTrials.Add(boundary, trials);
        }
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
            Save("protocol.json", new { version = Version, multipliers = Multipliers, executionOrder = ExecutionOrder, samples = Samples, cells = Cells,
                maximumFights = Fights, maximumSeconds = 840, maximumBytes = 2L * 1073741824, newSeeds = 0, retries = 0,
                rule = "Lowest multiplier with strongest retained six-Essence team at 4–16/32. All 21 team/gear cells retained. Historical-seed range screen only; no lower-budget separation or balance acceptance. Replay both boundaries first, then complete all seven intermediate settings without extension." });
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
                    var hashes = new SortedDictionary<string, string>(TowerBundle.CopyContent(q.ApiRoot, contentRoot, token).ToDictionary(p => p.Key, p => p.Value), StringComparer.Ordinal);
                    Assert.Equal(HarnessJson.Hash(scope.ContentHashes), HarnessJson.Hash(hashes));
                    var path = Path.Combine(contentRoot, "Data", TowerBattleRunner.FloorFile);
                    var variant = VariantDocument(JsonNode.Parse(File.ReadAllText(path))!, Multipliers[i]);
                    File.WriteAllText(path, variant.ToJsonString(HarnessJson.Options)); hashes[TowerBattleRunner.FloorFile] = HarnessJson.FileHash(path);
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
                foreach (var boundary in new[] { 0, 8 })
                    for (var n = 0; n < VariantFights; n++)
                    {
                        var old = oldTrials[boundary][n];
                        Assert.Equal($"{cells[n / Samples].Case}/{cells[n / Samples].Profile}", old.Stage);
                        Assert.Equal(seeds[n % Samples], old.Seed);
                        Assert.Equal(old.InputHash, HarnessJson.Hash(roots[boundary].Runner.CreateInput(cells[n / Samples].Scenario,
                            seeds[n % Samples], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks)));
                    }
            }
            Save("preflight.json", new { status = "PreparedNoFights", variants = 9, cells = Cells * 9, historicalInputsMatched = 2 * VariantFights });
            VerifyInputs(); Check(); var rows = new List<Row>(); var baselines = new Dictionary<string, bool[]>();
            foreach (var i in ExecutionOrder)
            {
                RequirePriorParity(i, matched);
                var (root, variantScope, runner) = roots[i]; var study = Path.Combine(root, "study");
                var archive = F.Archive(study, variantScope, VariantFights, root, token);
                foreach (var (cell, cellIndex) in cells.Select((c, n) => (c, n)))
                {
                    var reports = new List<TowerBattleReport>(); var stage = $"{cell.Case}/{cell.Profile}";
                    foreach (var (seed, seedIndex) in seeds.Select((s, n) => (s, n)))
                    {
                        Check(); Assert.True(++attempts <= Fights);
                        TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"),
                            JsonSerializer.Serialize(new { attempt = attempts, variant = i, stage, seed }) + "\n");
                        var trial = await archive.EvaluateAsync(Version, stage, cell.Scenario, seed, token);
                        completed++; reports.Add(trial.Report);
                        if (i is 0 or 8)
                        {
                            var old = oldTrials[i][cellIndex * Samples + seedIndex];
                            var oldStudy = $"variant-{BoundarySource(i):D2}/study";
                            var name = oldStudy + "/battles/" + old.Id + ".json.gz";
                            Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name)));
                            Assert.Equal(old.InputHash, trial.Trial.InputHash);
                            Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(Path.Combine(q.Source, oldStudy), old.Id, oldScope.ReportStorage)), HarnessJson.Hash(trial.Report)); matched++;
                        }
                        else Assert.Equal(2 * VariantFights, matched);
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
            Assert.Equal(Fights, completed); Assert.Equal(2 * VariantFights, matched); VerifyInputs();
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            var summaries = Enumerable.Range(0, 9).Select(i => Summarize(i, rows.Where(r => r.Variant == i).ToArray())).ToArray();
            var selected = summaries.FirstOrDefault(s => s.Eligible);
            Save("result.json", new { status = "RefinedFloor10CalibrationComplete", fights = completed, rows, summaries, selected,
                decision = selected is null ? "NoSettingInGrid" : "DiagnosticCalibrationCandidate", runtimeParityReports = matched,
                newSeeds = 0, confirmedTeams = 0, searchRuns = 0, retries = 0, seconds = watch.Elapsed.TotalSeconds,
                balanceAcceptance = "NotAssessedHistoricalSeeds" });
            Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1073741824); Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
