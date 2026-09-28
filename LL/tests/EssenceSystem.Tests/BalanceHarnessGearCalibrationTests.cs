using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessGearCalibrationTests
{
    private const string Version = "tower-gear-offense-calibration-v1";
    private const int Samples = 32, Fights = 1120;
    private static readonly decimal[] Offenses = [3.36m, 4.20m, 5.04m, 6.72m, 10.08m];
    private static readonly string[] Profiles = ["baseline", "precision", "ability-haste", "restorer-specialization",
        "armor-and-health", "resistance-and-health", "health-and-regeneration"];
    private sealed record Request(string Version, string Output, string Source, string SourcePin, string Runtime,
        string Proposal, string Controls, IReadOnlyDictionary<string, string> InputHashes);
    private sealed record Control(string Profile, TowerScenario Scenario);
    private sealed record SourceCell(int Floor, string Profile, TowerScenario Scenario);
    private sealed record Row(int Variant, decimal Offense, string Profile, int Wins, int Samples, decimal MeanGuardianHealth);
    private sealed record Summary(int Variant, decimal Offense, int BestWins, IReadOnlyList<string> BestProfiles, bool Eligible);
    private sealed class CalibrationFactAttribute : FactAttribute
    {
        public CalibrationFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_GEAR_CALIBRATION")))
                Skip = "Set LL_GEAR_CALIBRATION for the frozen 1,120-fight historical-seed calibration.";
        }
    }

    private static JsonNode VariantDocument(JsonNode original, decimal offense)
    {
        if (!Offenses.Contains(offense)) throw new InvalidDataException("Offense is outside the frozen grid.");
        var result = original.DeepClone();
        var floor = result["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 13)!;
        if (floor["guardianScaling"]!["offense"]!.GetValue<decimal>() != Offenses[0])
            throw new InvalidDataException("Source floor-13 offense has changed.");
        floor["guardianScaling"]!["offense"] = offense;
        return result;
    }

    private static Summary[] Summarize(IReadOnlyList<Row> rows)
    {
        if (rows.Count != Offenses.Length * Profiles.Length) throw new InvalidDataException("Incomplete calibration matrix.");
        var summaries = new List<Summary>();
        for (var i = 0; i < Offenses.Length; i++)
        {
            var group = rows.Skip(i * Profiles.Length).Take(Profiles.Length).ToArray();
            if (!group.Select(r => r.Profile).SequenceEqual(Profiles)
                || group.Any(r => r.Variant != i || r.Offense != Offenses[i] || r.Samples != Samples || r.Wins is < 0 or > Samples))
                throw new InvalidDataException("Changed profile, variant or sample schedule.");
            var best = group.Max(r => r.Wins);
            summaries.Add(new(i, Offenses[i], best, group.Where(r => r.Wins == best).Select(r => r.Profile).ToArray(), best is >= 4 and <= 28));
        }
        return summaries.ToArray();
    }

    [Theory]
    [InlineData(0, false)] [InlineData(3, false)] [InlineData(4, true)]
    [InlineData(28, true)] [InlineData(29, false)] [InlineData(32, false)]
    public void Eligibility_uses_the_best_profile_and_inclusive_boundaries(int strongest, bool eligible)
    {
        var rows = Offenses.SelectMany((offense, i) => Profiles.Select((p, j) =>
            new Row(i, offense, p, j == 5 ? strongest : Math.Min(strongest, 4), Samples, 0))).ToArray();
        Assert.All(Summarize(rows), s => Assert.Equal(eligible, s.Eligible));
        Assert.Equal(eligible ? (int?)0 : null, Summarize(rows).FirstOrDefault(s => s.Eligible)?.Variant);
        Assert.Throws<InvalidDataException>(() => Summarize(rows.Skip(1).ToArray()));
        rows[0] = rows[0] with { Profile = Profiles[1] };
        Assert.Throws<InvalidDataException>(() => Summarize(rows));
    }

    [Fact]
    public void Selection_keeps_the_lowest_eligible_setting_without_assuming_monotonic_results()
    {
        int[] wins = [32, 28, 0, 16, 32];
        var rows = Offenses.SelectMany((offense, i) => Profiles.Select(p => new Row(i, offense, p, wins[i], Samples, 0))).ToArray();
        Assert.Equal(1, Summarize(rows).First(s => s.Eligible).Variant);
        Assert.Equal(new[] { 1, 3 }, Summarize(rows).Where(s => s.Eligible).Select(s => s.Variant));
    }

    [Fact]
    public void Content_variants_change_only_floor13_offense_and_preserve_the_source()
    {
        var original = JsonNode.Parse(File.ReadAllText(Path.Combine(TestContentPaths.FindApiRoot(), "Data", TowerBattleRunner.FloorFile)))!;
        var before = original.ToJsonString();
        foreach (var offense in Offenses)
        {
            var variant = VariantDocument(original, offense);
            var floor = variant["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 13)!;
            Assert.Equal(offense, floor["guardianScaling"]!["offense"]!.GetValue<decimal>());
            floor["guardianScaling"]!["offense"] = Offenses[0];
            Assert.True(JsonNode.DeepEquals(original, variant));
        }
        Assert.Equal(before, original.ToJsonString());
        Assert.Throws<InvalidDataException>(() => VariantDocument(original, 7));
        var changed = VariantDocument(original, Offenses[1]);
        Assert.Throws<InvalidDataException>(() => VariantDocument(changed, Offenses[2]));
    }

    [CalibrationFact]
    public async Task Frozen_floor13_matrix_completes_without_changing_parties_or_allocating_seeds()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_GEAR_CALIBRATION")!);
        Assert.Equal(Version, q.Version); Assert.False(Path.Exists(q.Output));
        foreach (var path in q.InputHashes.Keys.Append(q.Source).Append(q.Output).Append(q.Runtime)) TowerProposalStudy.Unlinked(path);
        void VerifyInputs() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        VerifyInputs();
        Assert.Equal(q.SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var sourceFiles = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name) { var path = Path.Combine(q.Source, name); Assert.Equal(sourceFiles[name], HarnessJson.FileHash(path)); return HarnessJson.Read<T>(path); }
        var scope = Source<LoadoutScope>("scope.json") with { Algorithm = Version };
        Assert.Equal(HarnessJson.Hash(scope.Execution), HarnessJson.Hash(ExecutionIdentity.Current()));
        Assert.Equal(new TowerBalanceSelection(18, 4, "healing-v1"), scope.Settings.Balance);
        var proposal = HarnessJson.Read<JsonElement>(q.Proposal);
        Assert.Equal("tower-gear-aware-benchmark-proposal-v1", proposal.GetProperty("version").GetString());
        Assert.Equal(13, proposal.GetProperty("floor").GetInt32());
        Assert.Equal(Fights, proposal.GetProperty("maximumFights").GetInt32());
        Assert.Equal(Offenses, proposal.GetProperty("offenseValues").EnumerateArray().Select(v => v.GetDecimal()));
        Assert.Equal(HarnessJson.Hash(scope.Settings), HarnessJson.Hash(proposal.GetProperty("settings")));
        var seeds = proposal.GetProperty("seeds").Deserialize<int[]>(HarnessJson.Options)!;
        Assert.Equal(Samples, seeds.Length); Assert.Equal(Samples, seeds.Distinct().Count());
        Assert.Equal(seeds, Source<JsonElement>("request.json").GetProperty("seeds").Deserialize<int[]>(HarnessJson.Options));
        var controls = HarnessJson.Read<Control[]>(q.Controls);
        Assert.Equal(Profiles, controls.Select(c => c.Profile));
        var sourceCells = Source<SourceCell[]>("cells.json").Where(c => c.Floor == 13).ToArray();
        Assert.Equal(HarnessJson.Hash(sourceCells.Select(c => new Control(c.Profile, c.Scenario with { Seeds = [] }))), HarnessJson.Hash(controls));
        foreach (var c in controls)
        {
            Assert.Empty(c.Scenario.Seeds); Assert.Equal(13, c.Scenario.FloorNumber);
            TowerBossDiscovery.ValidateEquipment(c.Scenario.Party, TowerPartyProgression.Budget(7) with { PriorityFloor = 13 }, 10);
        }
        controls = controls.Select(c => c with { Scenario = c.Scenario with { Seeds = seeds } }).ToArray();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(840)); var token = deadline.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output);
        Directory.CreateDirectory(q.Output);
        var attempts = 0; var completed = 0; var success = false;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % Samples == 0) Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 1073741824); }
        try
        {
            Save("request.json", q); Save("source-scope.json", scope); Save("proposal.json", proposal); Save("controls.json", controls);
            Save("protocol.json", new { version = Version, samples = Samples, profiles = Profiles, offenses = Offenses,
                maximumFights = Fights, maximumSeconds = 840, maximumBytes = 1073741824, newSeeds = 0, retries = 0,
                rule = "Lowest offense whose best profile wins 4 through 28 of 32; all 35 cells required. No automatic search or extension." });
            foreach (var pin in scope.Execution.AssemblyHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var path in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, path);
                if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(path, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var roots = new List<(string Root, LoadoutScope Scope, TowerBattleRunner Runner)>();
            var matched = 0;
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Calibration preparation cannot fight.")).Activate())
            {
                for (var i = 0; i < Offenses.Length; i++)
                {
                    Check(); var variantRoot = Path.Combine(q.Output, $"variant-{i:D2}"); var contentRoot = Path.Combine(variantRoot, "content");
                    var hashes = new SortedDictionary<string, string>(TowerBundle.CopyContent(Path.Combine(q.Source, "content"), contentRoot, token).ToDictionary(p => p.Key, p => p.Value), StringComparer.Ordinal);
                    Assert.Equal(HarnessJson.Hash(scope.ContentHashes), HarnessJson.Hash(hashes));
                    if (i > 0)
                    {
                        var path = Path.Combine(contentRoot, "Data", TowerBattleRunner.FloorFile);
                        var original = JsonNode.Parse(File.ReadAllText(path))!;
                        File.WriteAllText(path, VariantDocument(original, Offenses[i]).ToJsonString(HarnessJson.Options));
                        hashes[TowerBattleRunner.FloorFile] = HarnessJson.FileHash(path);
                    }
                    var variantScope = scope with { ContentHashes = hashes };
                    TowerBundle.WriteSettings(Path.Combine(contentRoot, "appsettings.json"), scope.Settings);
                    HarnessJson.WriteNew(Path.Combine(variantRoot, "scope.json"), variantScope);
                    var runner = new TowerBattleRunner(contentRoot, OfflineContent.ForTower(contentRoot, scope.Settings));
                    foreach (var c in controls)
                    foreach (var seed in seeds)
                        _ = await runner.PrepareAsync(runner.CreateInput(c.Scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks), token);
                    roots.Add((variantRoot, variantScope, runner));
                }
                var trialsPath = Path.Combine(q.Source, "study/trials.jsonl");
                Assert.Equal(sourceFiles["study/trials.jsonl"], HarnessJson.FileHash(trialsPath));
                foreach (var trial in File.ReadLines(trialsPath).Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!).Where(t => t.Stage.StartsWith("13/", StringComparison.Ordinal)))
                {
                    var control = controls.Single(c => trial.Stage == "13/" + c.Profile);
                    Assert.Equal(HarnessJson.Hash(control.Scenario), trial.Recipe);
                    Assert.Equal(trial.InputHash, HarnessJson.Hash(roots[0].Runner.CreateInput(control.Scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks)));
                    matched++;
                }
                Assert.Equal(224, matched);
            }
            Save("preflight.json", new { status = "PreparedNoFights", cells = 35, inputs = Fights, historicalInputsMatched = matched });
            VerifyInputs(); Check();
            var rows = new List<Row>();
            for (var i = 0; i < roots.Count; i++)
            {
                var (root, variantScope, runner) = roots[i]; var study = Path.Combine(root, "study");
                var archive = F.Archive(study, variantScope, Profiles.Length * Samples, root, token);
                foreach (var control in controls)
                {
                    var reports = new List<TowerBattleReport>();
                    foreach (var seed in seeds)
                    {
                        Check(); Assert.True(++attempts <= Fights);
                        TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                        var trial = await archive.EvaluateAsync(Version, control.Profile, control.Scenario, seed, token);
                        completed++; reports.Add(trial.Report);
                    }
                    var row = new Row(i, Offenses[i], control.Profile, reports.Count(r => r.Succeeded), Samples, reports.Average(r => r.GuardianHealthRemainingPercent));
                    rows.Add(row); Console.WriteLine(JsonSerializer.Serialize(row, HarnessJson.Options));
                }
                Assert.Equal(0, archive.CacheHits); F.Seal(study);
                var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(224, trials.Count);
                using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Calibration audit cannot fight.")).Activate())
                for (var n = 0; n < trials.Count; n++)
                {
                    Check(); var trial = trials[n]; var control = controls[n / Samples];
                    Assert.Equal(control.Profile, trial.Stage); Assert.Equal(seeds[n % Samples], trial.Seed);
                    Assert.Equal(HarnessJson.Hash(control.Scenario), trial.Recipe);
                    var input = runner.CreateInput(control.Scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                    Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(variantScope, Version, input), trial.CacheKey);
                    var report = TowerLoadoutArchive.ReadBattle(study, trial.Id, variantScope.ReportStorage);
                    Assert.Equal(trial.Seed, report.Battle.Seed); Assert.Equal(control.Scenario.Id, report.Battle.ScenarioId);
                    Assert.Equal(report.Battle.Summary.ContentOutcome == BattleOutcome.Victory, report.Succeeded);
                }
                foreach (var pin in variantScope.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "content", "Data", pin.Key)));
            }
            Assert.Equal(Fights, completed); var summaries = Summarize(rows); var selected = summaries.FirstOrDefault(s => s.Eligible);
            VerifyInputs();
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            Save("result.json", new { status = "CalibrationComplete", fights = completed, rows, summaries, selected,
                decision = selected is null ? "NoInformativeBenchmark" : "DiagnosticBenchmarkCandidate", newSeeds = 0, confirmedTeams = 0, searchRuns = 0,
                retries = 0, seconds = watch.Elapsed.TotalSeconds, interpretation = "Historical-seed diagnostic on one fixed Essence composition; not balance acceptance or search superiority." });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
