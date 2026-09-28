using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessCarryForwardEquipmentTests
{
    private const string Version = "tower-floor11-carried-equipment-screen-v1";
    private const string SourcePin = "41a93659a05583d0cf55d91b98c2ef4442bfeba117ab1fea7d9daa0131827378";
    private const int Cells = 228, Samples = 32, PanelFights = Cells * Samples, Fights = 2 * PanelFights;
    private const string CarryAssumption = "Carried floor-10 equipment: Legendary / Masterpiece / rank 5 at the retained tier, with the same specializations and rolls. No gear downgrade at floor 11. Hypothetical ownership; no transferred strength claim.";
    private sealed record Request(string Version, string Source, string SourcePin, string Floor10Source, string Output,
        string ApiRoot, string Runtime, IReadOnlyDictionary<string, string> InputHashes,
        IReadOnlyDictionary<string, string> ContentHashes, IReadOnlyDictionary<string, string> AssemblyHashes);
    private sealed record Cell(string Id, string SourceCase, string Kind, string? Addition, string Profile, TowerScenario Scenario);
    private sealed record Floor10Cell(string Case, string Profile, TowerScenario Scenario);
    private sealed record Row(string Id, string Kind, string Profile, int Level, int EssenceSlots,
        int BaselineWins, int Wins, int Draws, int Samples, int GainedWins, int LostWins,
        decimal MeanGuardianHealth, double MeanDurationSeconds);
    private sealed record Summary(int IntendedCells, int ControlCells, int BestIntendedWins, int BestControlWins,
        int IntendedAboveCeiling, int ControlsAtOrAboveMinimum, int IntendedWithObservedViability, string Decision);

    private static TowerScenario Carry(TowerScenario scenario, OfflineContent content)
    {
        var band = new TowerProgressionEquipmentCycle(10,
            [new(1, 10, EquipmentRarity.Legendary, ItemQuality.Masterpiece, 5)]);
        var converted = TowerProgressionEquipment.Apply(scenario, band, content);
        return converted with { Seeds = scenario.Seeds, Assumptions = [.. scenario.Assumptions, CarryAssumption] };
    }

    private static Summary Summarize(IReadOnlyList<Row> rows)
    {
        if (rows.Count != Cells || rows.Select(r => r.Id).Distinct().Count() != Cells
            || rows.Count(r => r.EssenceSlots == 7 && r.Level == 60) != 198
            || rows.Count(r => r.EssenceSlots == 6 && r.Level == 50) != 14
            || rows.Count(r => r.EssenceSlots == 6 && r.Level == 60) != 2
            || rows.Count(r => r.EssenceSlots == 4 && r.Level == 30) != 14
            || rows.Any(r => r.Samples != Samples || r.Wins is < 0 or > Samples || r.Draws < 0
                || r.Wins + r.Draws > Samples || r.BaselineWins is < 0 or > Samples
                || r.GainedWins < 0 || r.LostWins < 0 || r.GainedWins > Samples - r.BaselineWins
                || r.LostWins > r.BaselineWins || r.Wins != r.BaselineWins + r.GainedWins - r.LostWins))
            throw new InvalidDataException("Complete valid paired family required.");
        var intended = rows.Where(r => r.EssenceSlots == 7).ToArray();
        var controls = rows.Where(r => r.EssenceSlots != 7).ToArray();
        var above = intended.Count(r => r.Wins > 16); var breach = controls.Count(r => r.Wins >= 4);
        var viable = intended.Count(r => r.Wins is >= 4 and <= 16);
        return new(intended.Length, controls.Length, intended.Max(r => r.Wins), controls.Max(r => r.Wins),
            above, breach, viable, breach > 0 ? "ObservedLowerBudgetBreach" : above > 0 ? "ObservedCeilingBreach"
                : viable == 0 ? "NoObservedViableTeam" : "CandidateNeedsFreshConfirmation");
    }

    [Theory]
    [InlineData(8, 0, "CandidateNeedsFreshConfirmation")]
    [InlineData(17, 0, "ObservedCeilingBreach")]
    [InlineData(8, 4, "ObservedLowerBudgetBreach")]
    [InlineData(32, 32, "ObservedLowerBudgetBreach")]
    [InlineData(3, 3, "NoObservedViableTeam")]
    public void Paired_screen_keeps_the_full_family_and_reports_control_and_ceiling_concerns(int best, int control, string expected)
    {
        var rows = Enumerable.Range(0, Cells).Select(i => {
            var slots = i < 198 ? 7 : i < 214 ? 6 : 4;
            var level = i < 198 ? 60 : i < 212 ? 50 : i < 214 ? 60 : 30;
            var wins = i == 197 ? best : i == 213 ? control : 0;
            return new Row($"cell-{i}", "test", "test", level, slots, 0, wins, 0, Samples, wins, 0, 0, 0);
        }).ToArray();
        Assert.Equal(expected, Summarize(rows).Decision);
        Assert.Equal(best > 16 ? 1 : 0, Summarize(rows).IntendedAboveCeiling);
        Assert.Throws<InvalidDataException>(() => Summarize(rows.Skip(1).ToArray()));
        var invalid = rows.ToArray(); invalid[0] = invalid[1];
        Assert.Throws<InvalidDataException>(() => Summarize(invalid));
        invalid = rows.ToArray(); invalid[0] = invalid[0] with { GainedWins = 1 };
        Assert.Throws<InvalidDataException>(() => Summarize(invalid));
    }

    [Fact]
    public void Carried_gear_keeps_tier_specializations_and_floor10_instances_through_level_and_essence_upgrades()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Conversion cannot fight.")).Activate();
        var root = TestContentPaths.FindApiRoot(); var content = OfflineContent.ForTower(root, TowerBundle.ReadSettings(root));
        var fixtures = Path.GetFullPath(Path.Combine(root, "../../../tools/BalanceHarness/Fixtures"));
        var source = TowerPartyProgression.Scenarios(root, fixtures, TowerPartyProgression.Budget(6)).Single(s => s.FloorNumber == 10);
        var original = HarnessJson.Hash(source); var carried = Carry(source, content);
        Assert.Equal(original, HarnessJson.Hash(source));
        var onward = carried with { FloorNumber = 11, Party = carried.Party.Take(10).ToArray() };
        var addition = TowerProgressionUpgrades.UniformAdditions(onward, content)[0];
        var upgraded = TowerProgressionUpgrades.Apply(onward, 60, addition, content);
        foreach (var (before, after) in onward.Party.Zip(upgraded.Party))
        {
            var a = content.CreateBuild(before.Build); var b = content.CreateBuild(after.Build);
            Assert.Equal(a.Character.Id, b.Character.Id);
            Assert.Equal(a.Equipment.Select(e => e.Id), b.Equipment.Select(e => e.Id));
            Assert.Equal(a.EquippedEssences.Select(e => e.Id), b.EquippedEssences.Take(6).Select(e => e.Id));
            Assert.Equal(HarnessJson.Hash(a.Equipment.Select(e => e.ProgressionData!.State)),
                HarnessJson.Hash(b.Equipment.Select(e => e.ProgressionData!.State)));
            Assert.All(b.Equipment, e => {
                Assert.Equal(EquipmentRarity.Legendary, e.ProgressionData!.Rarity);
                Assert.Equal(5, e.ProgressionData.State.Rank);
                Assert.Equal(ItemQuality.Masterpiece, e.ProgressionData.State.Quality);
            });
        }
        foreach (var (before, after) in source.Party.Zip(carried.Party))
        {
            Assert.Equal(before.Build.Tier, after.Build.Tier);
            Assert.Equal(before.Build.CharacterLevel, after.Build.CharacterLevel);
            Assert.Equal(before.Build.EssenceIds, after.Build.EssenceIds);
            foreach (var (a, b) in before.Build.Equipment.Zip(after.Build.Equipment))
            {
                var old = content.Equipment.Evaluator.GetDefinition(a.DefinitionId);
                var changed = content.Equipment.Evaluator.GetDefinition(b.DefinitionId);
                Assert.Equal((old.ArchetypeId, old.SpecializationId), (changed.ArchetypeId, changed.SpecializationId));
            }
        }
    }

    private sealed class ScreenFactAttribute : FactAttribute
    {
        public ScreenFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_CARRIED_EQUIPMENT_SCREEN")))
                Skip = "Set LL_CARRIED_EQUIPMENT_SCREEN for 7,296 historical parity replays followed by 7,296 carried-gear fights.";
        }
    }

    [ScreenFact]
    public async Task Complete_floor11_family_is_compared_with_equipment_carried_from_floor10()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_CARRIED_EQUIPMENT_SCREEN")!);
        Assert.Equal(Version, q.Version); Assert.Equal(SourcePin, q.SourcePin); Assert.False(Path.Exists(q.Output));
        foreach (var path in q.InputHashes.Keys.Concat(new[] { q.Source, q.Output, q.Floor10Source, q.ApiRoot, q.Runtime })) TowerProposalStudy.Unlinked(path);
        void Recheck() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        Recheck(); Assert.Equal(SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        string Source(string name) { var path = Path.Combine(q.Source, name); Assert.Equal(files[name], HarnessJson.FileHash(path)); return path; }
        var oldScope = HarnessJson.Read<LoadoutScope>(Source("scope.json"));
        var settings = TowerBundle.ReadSettings(q.ApiRoot); Assert.Equal(HarnessJson.Hash(oldScope.Settings), HarnessJson.Hash(settings));
        var scope = oldScope with { Algorithm = Version, Execution = ExecutionIdentity.Current(), ContentHashes = q.ContentHashes };
        Assert.Equal(HarnessJson.Hash(q.AssemblyHashes), HarnessJson.Hash(scope.Execution.AssemblyHashes));
        var panel = HarnessJson.Read<int[]>(Source("confirmation-seeds.json")); Assert.Equal(256, panel.Length);
        var seeds = panel.Take(Samples).ToArray(); Assert.Equal(Samples, seeds.Distinct().Count());
        var cells = HarnessJson.Read<Cell[]>(Source("cells.json")).Select(c => c with { Scenario = c.Scenario with { Seeds = panel } }).ToArray();
        Assert.Equal(Cells, cells.Length); Assert.Equal(Cells, cells.Select(c => c.Id).Distinct().Count());
        var originalTrials = File.ReadLines(Source("study/trials.jsonl")).Select(l => JsonSerializer.Deserialize<LoadoutTrial>(l, HarnessJson.Options)!).ToArray();
        Assert.Equal(58368, originalTrials.Length);
        var oldTrials = Enumerable.Range(0, Cells).SelectMany(i => originalTrials.Skip(i * 256).Take(Samples)).ToArray();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(1200)); var token = stop.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output); Directory.CreateDirectory(q.Output);
        var attempts = 0; var completed = 0; var matched = 0; var success = false; var lastStorageCheck = -1;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            if (completed % Samples == 0 && lastStorageCheck != completed) {
                Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1073741824); lastStorageCheck = completed; } }
        try
        {
            Save("request.json", q); Save("scope.json", scope); Save("seeds.json", seeds); Save("baseline-cells.json", cells);
            Save("protocol.json", new { version = Version, cells = Cells, samples = Samples, maximumFights = Fights,
                qualificationFights = PanelFights, screenFights = PanelFights, maximumSeconds = 1200,
                maximumBytes = 2L * 1073741824, newSeeds = 0, retries = 0,
                rule = "First 32 seeds of the existing confirmation, across every cell. Complete all baseline input/full-report parity before carried-gear combat. Observed intended >16/32 or controls >=4/32 flag concerns. Historical diagnostic only; no acceptance, tuning, extension or application." });
            var root = Path.Combine(q.Output, "content");
            Assert.Equal(HarnessJson.Hash(scope.ContentHashes), HarnessJson.Hash(TowerBundle.CopyContent(q.ApiRoot, root, token)));
            TowerBundle.WriteSettings(Path.Combine(root, "appsettings.json"), settings);
            foreach (var pin in q.AssemblyHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Runtime, pin.Key + ".dll")));
            foreach (var path in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, path); if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var destination = Path.Combine(q.Output, "executable", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(path, destination, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var content = OfflineContent.ForTower(root, settings); var runner = new TowerBattleRunner(root, content);
            Cell[] carried;
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate())
            {
                carried = cells.Select(c => c with { Scenario = Carry(c.Scenario, content) }).ToArray();
                var floor10 = HarnessJson.Read<Floor10Cell[]>(Path.Combine(q.Floor10Source, "cells.json"));
                foreach (var cell in carried.Where(c => c.Scenario.Party[0].Build.EssenceIds.Count == 6 && c.Scenario.Party[0].Build.CharacterLevel == 50))
                {
                    var source = floor10.Single(c => c.Case == cell.SourceCase.Replace("floor11-six-", "floor10-retained-") && c.Profile == cell.Profile);
                    Assert.Equal(HarnessJson.Hash(source.Scenario.Party.Take(10).ToArray()), HarnessJson.Hash(cell.Scenario.Party));
                }
                foreach (var cell in carried.Where(c => c.Kind is "level-control" or "seventh-addition"))
                {
                    var parent = carried.Single(c => c.Id == cell.SourceCase + "/resistance-and-health");
                    var expected = TowerProgressionUpgrades.Apply(parent.Scenario, 60, cell.Addition, content);
                    Assert.Equal(HarnessJson.Hash(expected.Party), HarnessJson.Hash(cell.Scenario.Party));
                }
                foreach (var cell in cells.Concat(carried))
                    _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario, seeds[0], settings.Threat, settings.CheckpointIntervalTicks), token);
                for (var n = 0; n < PanelFights; n++)
                {
                    var old = oldTrials[n]; var cell = cells[n / Samples];
                    Assert.Equal(cell.Id, old.Stage); Assert.Equal(seeds[n % Samples], old.Seed);
                    Assert.Equal(HarnessJson.Hash(cell.Scenario), old.Recipe);
                    Assert.Equal(old.InputHash, HarnessJson.Hash(runner.CreateInput(cell.Scenario, old.Seed, settings.Threat, settings.CheckpointIntervalTicks)));
                }
            }
            Save("carried-cells.json", carried);
            Save("preflight.json", new { status = "PreparedNoFights", cells = 2 * Cells, historicalInputsMatched = PanelFights,
                floor10PartiesMatched = 14, progressionPartiesMatched = 116 });
            Recheck(); Check(); var rows = new List<Row>(); var baseline = new Dictionary<string, bool[]>();
            foreach (var mode in new[] { "baseline", "carried" })
            {
                if (mode == "carried") Assert.Equal(PanelFights, matched);
                var current = mode == "baseline" ? cells : carried;
                var study = Path.Combine(q.Output, mode); var archive = F.Archive(study, scope, PanelFights, q.Output, token);
                foreach (var (cell, index) in current.Select((c, i) => (c, i)))
                {
                    var reports = new List<TowerBattleReport>();
                    foreach (var (seed, offset) in seeds.Select((s, i) => (s, i)))
                    {
                        Check(); Assert.True(++attempts <= Fights);
                        TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), JsonSerializer.Serialize(new { attempt = attempts, mode, stage = cell.Id, seed }) + "\n");
                        var trial = await archive.EvaluateAsync(Version, cell.Id, cell.Scenario, seed, token); completed++;
                        reports.Add(trial.Report);
                        if (mode == "baseline")
                        {
                            var old = oldTrials[index * Samples + offset]; _ = Source("study/battles/" + old.Id + ".json.gz");
                            Assert.Equal(old.InputHash, trial.Trial.InputHash);
                            Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(Path.Combine(q.Source, "study"), old.Id, oldScope.ReportStorage)), HarnessJson.Hash(trial.Report)); matched++;
                        }
                    }
                    var wins = reports.Select(r => r.Succeeded).ToArray();
                    if (mode == "baseline") baseline.Add(cell.Id, wins);
                    else
                    {
                        var prior = baseline[cell.Id]; var pairs = wins.Zip(prior).ToArray();
                        rows.Add(new(cell.Id, cell.Kind, cell.Profile, cell.Scenario.Party[0].Build.CharacterLevel,
                            cell.Scenario.Party[0].Build.EssenceIds.Count, prior.Count(w => w), wins.Count(w => w),
                            reports.Count(r => r.Battle.Summary.ContentOutcome == BattleOutcome.Draw), Samples,
                            pairs.Count(p => p.First && !p.Second), pairs.Count(p => !p.First && p.Second),
                            reports.Average(r => r.GuardianHealthRemainingPercent), reports.Average(r => r.Battle.Summary.DurationSeconds)));
                    }
                }
                Assert.Equal(0, archive.CacheHits); F.Seal(study);
                var trials = TowerLoadoutArchive.Verify(study, token); Assert.Equal(PanelFights, trials.Count);
                foreach (var (trial, n) in trials.Select((t, i) => (t, i)))
                {
                    Check(); var cell = current[n / Samples]; Assert.Equal(cell.Id, trial.Stage); Assert.Equal(seeds[n % Samples], trial.Seed);
                    Assert.Equal(HarnessJson.Hash(cell.Scenario), trial.Recipe);
                    var input = runner.CreateInput(cell.Scenario, trial.Seed, settings.Threat, settings.CheckpointIntervalTicks);
                    Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(scope, Version, input), trial.CacheKey);
                }
                if (mode == "baseline") Save("runtime-qualification.json", new { status = "Matched", inputs = matched, fullReports = matched });
            }
            Assert.Equal(Fights, completed); Assert.Equal(PanelFights, matched); Recheck();
            foreach (var pin in scope.ContentHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(root, "Data", pin.Key)));
            foreach (var pin in HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Output, "runtime-files.json")))
                Assert.Equal(pin.Value, HarnessJson.FileHash(Path.Combine(q.Output, "executable", pin.Key)));
            Save("result.json", new { status = "CarriedEquipmentScreenComplete", fights = completed, screenFights = PanelFights,
                runtimeParityReports = matched, rows, summary = Summarize(rows), newSeeds = 0, confirmedTeams = 0,
                searchRuns = 0, retries = 0, seconds = watch.Elapsed.TotalSeconds, balanceAcceptance = "NotAssessedHistoricalSeeds" });
            Check(); success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
