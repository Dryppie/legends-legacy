using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Services.LL.PowerRatings;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProgressionUpgradeTests
{
    private const string Version = "tower-floor11-seventh-essence-screen-v1";
    private const int Samples = 32, QualificationCells = 112, AddedCells = 116, Fights = (QualificationCells + AddedCells) * Samples;
    private sealed record Request(string Version, string Output, string Source, string SourcePin, string Runtime,
        IReadOnlyDictionary<string, string> InputHashes, IReadOnlyDictionary<string, string> AssemblyHashes);
    private sealed record SourceCell(string Case, string Purpose, int Floor, int EssenceSlots, string Profile, TowerScenario Scenario);
    private sealed record Cell(string Id, string SourceCase, string Kind, string? Addition, string Profile, TowerScenario Scenario);
    private sealed record Row(string Id, string SourceCase, string Kind, string? Addition, string Profile, int Level, int EssenceSlots,
        int Wins, int Draws, int Samples, decimal MeanGuardianHealth, double MeanDurationSeconds,
        int? GainedAgainstLevel50, int? LostAgainstLevel50, int? GainedAgainstLevel60, int? LostAgainstLevel60);
    private sealed class ScreenFactAttribute : FactAttribute
    {
        public ScreenFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_PROGRESSION_UPGRADES")))
                Skip = "Set LL_PROGRESSION_UPGRADES for the frozen 7,296-fight addition-only screen.";
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Level_and_slot_changes_preserve_existing_instances_and_actual_loadout_legality(bool alreadyPinned)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate();
        var root = TestContentPaths.FindApiRoot(); var settings = TowerBundle.ReadSettings(root);
        var content = OfflineContent.ForTower(root, settings);
        var fixtures = Path.GetFullPath(Path.Combine(root, "../../../tools/BalanceHarness/Fixtures"));
        var source = TowerPartyProgression.Scenarios(root, fixtures, TowerPartyProgression.Budget(6)).Single(s => s.FloorNumber == 11);
        source = source with { Party = source.Party.Select(p => p with { Build = p.Build with {
            IdentityEssenceIds = alreadyPinned ? p.Build.EssenceIds.Select((_, i) => $"fixed-{i}").ToArray() : null } }).ToArray() };
        var hash = HarnessJson.Hash(source); var addition = TowerProgressionUpgrades.UniformAdditions(source, content).First();
        var levelOnly = TowerProgressionUpgrades.Apply(source, 60, null, content);
        var upgraded = TowerProgressionUpgrades.Apply(source, 60, addition, content);
        Assert.Empty(upgraded.Seeds); Assert.Equal(hash, HarnessJson.Hash(source));
        Assert.DoesNotContain("IdentityProgression", JsonSerializer.Serialize(source.Party[0].Build));
        foreach (var (before, after) in source.Party.Zip(upgraded.Party))
        {
            Assert.Equal(before.PartySlot, after.PartySlot);
            Assert.Equal(before.Build.EssenceIds, after.Build.EssenceIds.Take(6));
            Assert.Equal(addition, after.Build.EssenceIds[6]); Assert.Equal(60, after.Build.CharacterLevel);
            var a = content.CreateBuild(before.Build); var b = content.CreateBuild(after.Build);
            var control = content.CreateBuild(levelOnly.Party.Single(p => p.PartySlot == before.PartySlot).Build);
            Assert.Equal(a.Character.Id, b.Character.Id); Assert.Equal(a.Character.Id, control.Character.Id);
            Assert.Equal(a.Equipment.Select(e => e.Id), b.Equipment.Select(e => e.Id));
            Assert.Equal(a.EquippedEssences.Select(e => e.Id), b.EquippedEssences.Take(6).Select(e => e.Id));
            Assert.Equal(7, b.EquippedEssences.Select(e => e.Id).Distinct().Count());
            Assert.Equal(6, control.EquippedEssences.Count); Assert.Equal(60, b.Character.Level);
            Assert.Equal(HarnessJson.Hash(before.Build with { CharacterLevel = 60, EssenceIds = after.Build.EssenceIds,
                IdentityEssenceIds = null, IdentityProgression = after.Build.IdentityProgression }), HarnessJson.Hash(after.Build));
        }
        var runner = new TowerBattleRunner(root, content);
        _ = await runner.PrepareAsync(runner.CreateInput(upgraded with { Seeds = [1] }, 1, settings.Threat, settings.CheckpointIntervalTicks));
        Assert.Throws<InvalidDataException>(() => TowerProgressionUpgrades.Apply(source, 60, source.Party[0].Build.EssenceIds[0], content));
        Assert.Throws<ArgumentException>(() => TowerProgressionUpgrades.Apply(source, 50, addition, content));
        var build = upgraded.Party[0].Build;
        Assert.Throws<ArgumentException>(() => content.CreateBuild(build with { IdentityEssenceIds = build.EssenceIds }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(build with { IdentityProgression = new(1, build.EssenceIds) }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(build with { EssenceIds = [.. build.EssenceIds.Take(6), build.EssenceIds[0]] }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(build with { EssenceIds = [.. build.EssenceIds.Take(6), "missing"] }));
    }

    [ScreenFact]
    public async Task Fixed_seventh_essence_family_keeps_level_controls_and_every_original_strong_team()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_PROGRESSION_UPGRADES")!);
        Assert.Equal(Version, q.Version); Assert.False(Path.Exists(q.Output));
        foreach (var path in q.InputHashes.Keys.Append(q.Output).Append(q.Source).Append(q.Runtime)) TowerProposalStudy.Unlinked(path);
        void VerifyInputs() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        VerifyInputs(); Assert.Equal(q.SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        T Source<T>(string name) { Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name))); return HarnessJson.Read<T>(Path.Combine(q.Source, name)); }
        var priorScope = Source<LoadoutScope>("variant-03/scope.json");
        var scope = priorScope with { Algorithm = Version, Execution = ExecutionIdentity.Current() };
        Assert.Equal(HarnessJson.Hash(q.AssemblyHashes), HarnessJson.Hash(scope.Execution.AssemblyHashes));
        Assert.Equal(HarnessJson.Hash(scope.Settings), HarnessJson.Hash(TowerBundle.ReadSettings(TestContentPaths.FindApiRoot())));
        var oldCells = Source<SourceCell[]>("cells.json"); Assert.Equal(QualificationCells, oldCells.Length);
        var seeds = oldCells[0].Scenario.Seeds; Assert.Equal(Samples, seeds.Count); Assert.Equal(Samples, seeds.Distinct().Count());
        var ledger = Path.Combine(q.Source, "variant-03/study/trials.jsonl");
        Assert.Equal(files["variant-03/study/trials.jsonl"], HarnessJson.FileHash(ledger));
        var oldTrials = File.ReadLines(ledger).Select(line => JsonSerializer.Deserialize<LoadoutTrial>(line, HarnessJson.Options)!).ToArray();
        Assert.Equal(QualificationCells * Samples, oldTrials.Length);
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
            Save("request.json", q); Save("scope.json", scope);
            Save("protocol.json", new { version = Version, samples = Samples, maximumFights = Fights, qualificationFights = QualificationCells * Samples,
                uniformAdditionsPerSource = 57, maximumSeconds = 840, maximumBytes = 2L * 1073741824, newSeeds = 0, retries = 0,
                rule = "Replay every old floor-11 cell at factor 1.5. Each six-Essence source keeps resistance-and-health gear; test a level-60 six-Essence control and all 57 globally legal uniform seventh definitions. Rank observed additions by wins, lower guardian health, ordinal ID; export all co-leaders. Historical seeds only; no confirmation or calibration selection." });
            var root = Path.Combine(q.Output, "content");
            Assert.Equal(HarnessJson.Hash(scope.ContentHashes), HarnessJson.Hash(TowerBundle.CopyContent(Path.Combine(q.Source, "variant-03/content"), root, token)));
            TowerBundle.WriteSettings(Path.Combine(root, "appsettings.json"), scope.Settings);
            foreach (var pin in scope.Execution.AssemblyHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var path in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, path); if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(path, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var content = OfflineContent.ForTower(root, scope.Settings); var runner = new TowerBattleRunner(root, content);
            var cells = oldCells.Select(c => new Cell(c.Case + "/" + c.Profile, c.Case, "retained-control", null, c.Profile, c.Scenario)).ToList();
            var additions = new Dictionary<string, IReadOnlyList<string>>();
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate())
            {
                foreach (var name in new[] { "floor11-six-1", "floor11-six-2" })
                {
                    var original = oldCells.Single(c => c.Case == name && c.Profile == "resistance-and-health");
                    var eligible = TowerProgressionUpgrades.UniformAdditions(original.Scenario, content); Assert.Equal(57, eligible.Count); additions.Add(name, eligible);
                    cells.Add(new(name + "/level60-six", name, "level-control", null, original.Profile,
                        TowerProgressionUpgrades.Apply(original.Scenario, 60, null, content) with { Seeds = seeds }));
                    cells.AddRange(eligible.Select(id => new Cell(name + "/add/" + id, name, "seventh-addition", id, original.Profile,
                        TowerProgressionUpgrades.Apply(original.Scenario, 60, id, content) with { Seeds = seeds })));
                }
                Assert.Equal(QualificationCells + AddedCells, cells.Count);
                foreach (var cell in cells)
                    _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario, seeds[0], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks), token);
                for (var n = 0; n < oldTrials.Length; n++)
                    Assert.Equal(oldTrials[n].InputHash, HarnessJson.Hash(runner.CreateInput(cells[n / Samples].Scenario, seeds[n % Samples], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks)));
            }
            Save("cells.json", cells); Save("eligible-additions.json", additions);
            Save("preflight.json", new { status = "PreparedNoFights", cells = cells.Count, knownInputMatches = QualificationCells * Samples });
            VerifyInputs(); var study = Path.Combine(q.Output, "study"); var archive = F.Archive(study, scope, Fights, q.Output, token);
            var rows = new List<Row>(); var outcomes = new Dictionary<string, bool[]>();
            foreach (var (cell, ordinal) in cells.Select((c, n) => (c, n)))
            {
                var reports = new List<TowerBattleReport>();
                foreach (var (seed, seedIndex) in seeds.Select((s, n) => (s, n)))
                {
                    Check(); Assert.True(++attempts <= Fights);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), $"{{\"attempt\":{attempts}}}\n");
                    var trial = await archive.EvaluateAsync(Version, cell.Id, cell.Scenario, seed, token);
                    completed++; reports.Add(trial.Report);
                    if (ordinal < QualificationCells)
                    {
                        var old = oldTrials[ordinal * Samples + seedIndex]; var name = "variant-03/study/battles/" + old.Id + ".json.gz";
                        Assert.Equal(files[name], HarnessJson.FileHash(Path.Combine(q.Source, name)));
                        Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(Path.Combine(q.Source, "variant-03/study"), old.Id, priorScope.ReportStorage)), HarnessJson.Hash(trial.Report)); matched++;
                    }
                    else Assert.Equal(QualificationCells * Samples, matched);
                }
                var wins = reports.Select(r => r.Succeeded).ToArray(); outcomes.Add(cell.Id, wins);
                var baseline = cell.Kind == "retained-control" ? wins : outcomes[cell.SourceCase + "/resistance-and-health"];
                var pairs = wins.Zip(baseline).ToArray();
                var level = cell.Kind == "retained-control" ? null : wins.Zip(outcomes[cell.SourceCase + "/level60-six"]).ToArray();
                rows.Add(new(cell.Id, cell.SourceCase, cell.Kind, cell.Addition, cell.Profile, cell.Scenario.Party[0].Build.CharacterLevel,
                    cell.Scenario.Party[0].Build.EssenceIds.Count, wins.Count(w => w), reports.Count(r => r.Battle.Summary.ContentOutcome == BattleOutcome.Draw),
                    Samples, reports.Average(r => r.GuardianHealthRemainingPercent), reports.Average(r => r.Battle.Summary.DurationSeconds),
                    cell.Kind == "retained-control" ? null : pairs.Count(p => p.First && !p.Second),
                    cell.Kind == "retained-control" ? null : pairs.Count(p => !p.First && p.Second),
                    level?.Count(p => p.First && !p.Second), level?.Count(p => !p.First && p.Second)));
            }
            Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits); F.Seal(study);
            var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(Fights, trials.Count);
            foreach (var (trial, n) in trials.Select((t, n) => (t, n)))
            {
                Check(); var cell = cells[n / Samples]; Assert.Equal(seeds[n % Samples], trial.Seed); Assert.Equal(cell.Id, trial.Stage);
                Assert.Equal(HarnessJson.Hash(cell.Scenario), trial.Recipe);
                var input = runner.CreateInput(cell.Scenario, trial.Seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(scope, Version, input), trial.CacheKey);
            }
            var leaders = additions.Keys.Select(name => rows.Where(r => r.SourceCase == name && r.Kind == "seventh-addition")
                .OrderByDescending(r => r.Wins).ThenBy(r => r.MeanGuardianHealth).ThenBy(r => r.Addition, StringComparer.Ordinal).First()).ToArray();
            Save("handoff.json", new { status = "DiagnosticCandidatesNotConfirmed", leaders, teams = cells.Where(c => c.Kind == "level-control"
                || c.Kind == "seventh-addition" && c.SourceCase == leaders[0].SourceCase && rows.Single(r => r.Id == c.Id).Wins == leaders[0].Wins
                || c.Kind == "seventh-addition" && c.SourceCase == leaders[1].SourceCase && rows.Single(r => r.Id == c.Id).Wins == leaders[1].Wins)
                .Select(c => c with { Scenario = c.Scenario with { Seeds = [] } }).ToArray() });
            VerifyInputs();
            foreach (var pin in scope.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            Save("result.json", new { status = "ProgressionUpgradeScreenComplete", fights = completed, rows, leaders, runtimeParityReports = matched,
                newSeeds = 0, confirmedTeams = 0, supportedSearchRuns = 0, retries = 0, seconds = watch.Elapsed.TotalSeconds,
                balanceAcceptance = "NotAssessedHistoricalSeeds", scope = "All legal uniform additions on resistance-and-health; mixed seventh-slot assignments and other upgrade gear profiles not searched." });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
