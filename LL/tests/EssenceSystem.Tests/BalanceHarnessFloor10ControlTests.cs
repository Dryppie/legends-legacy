using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Services.LL.Items;
using F = EssenceSystem.Tests.BalanceHarnessAffinityFloorEvaluationTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessFloor10ControlTests
{
    private const string Version = "tower-floor10-five-essence-controls-v1";
    private const string SourcePin = "95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944";
    private const int Samples = 32, ParentCells = 21, Cells = 273, Fights = Cells * Samples;
    private sealed record Request(string Version, string Source, string SourcePin, string ApiRoot, string Output,
        string Runtime, IReadOnlyDictionary<string, string> InputHashes,
        IReadOnlyDictionary<string, string> ContentHashes, IReadOnlyDictionary<string, string> AssemblyHashes);
    private sealed record Parent(string Case, string Profile, TowerScenario Scenario);
    private sealed record Cell(string Id, string SourceCase, string Profile, int Level, int? RemovedIndex, TowerScenario Scenario);
    private sealed record Row(string Id, string SourceCase, string Profile, int Level, int Tier, int? RemovedIndex,
        int EssenceSlots, int Wins, int Draws, int Samples, int GainedWins, int LostWins,
        decimal MeanGuardianHealth, double MeanDurationSeconds);
    private sealed class ScreenFactAttribute : FactAttribute
    {
        public ScreenFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LL_FLOOR10_CONTROLS")))
                Skip = "Set LL_FLOOR10_CONTROLS for the frozen 8,736-fight historical five-Essence screen.";
        }
    }

    private static TowerScenario TestSource(string root, OfflineContent content)
    {
        var fixtures = Path.GetFullPath(Path.Combine(root, "../../../tools/BalanceHarness/Fixtures"));
        var draft = TowerContractJson.Read<TowerProgressionDraft>(Path.Combine(fixtures, TowerProgressionEquipmentCycleFixture));
        var scenario = TowerPartyProgression.Scenarios(root, fixtures, TowerPartyProgression.Budget(6)).Single(s => s.FloorNumber == 10);
        return TowerProgressionEquipment.Apply(scenario, draft.EquipmentCycle!, content);
    }
    private const string TowerProgressionEquipmentCycleFixture = TowerProgressionPreview.CycleFixture;

    [Theory]
    [InlineData(0, 40)] [InlineData(1, 40)] [InlineData(2, 40)] [InlineData(3, 40)] [InlineData(4, 40)] [InlineData(5, 40)]
    [InlineData(0, 50)] [InlineData(1, 50)] [InlineData(2, 50)] [InlineData(3, 50)] [InlineData(4, 50)] [InlineData(5, 50)]
    public async Task Removal_preserves_surviving_instances_order_and_real_preparation(int index, int level)
    {
        using var noCombat = new TowerPerformanceTrace(_ => throw new InvalidOperationException("No combat.")).Activate();
        var root = TestContentPaths.FindApiRoot(); var settings = TowerBundle.ReadSettings(root);
        var content = OfflineContent.ForTower(root, settings); var source = TestSource(root, content);
        var beforeHash = HarnessJson.Hash(source); var changed = TowerEssenceAblation.Remove(source, index, level, content);
        Assert.Empty(changed.Seeds); Assert.Equal(beforeHash, HarnessJson.Hash(source));
        Assert.DoesNotContain("identityEssenceIndices", JsonSerializer.Serialize(source, HarnessJson.Options));
        foreach (var (a, b) in source.Party.Zip(changed.Party))
        {
            var before = content.CreateBuild(a.Build); var after = content.CreateBuild(b.Build);
            Assert.Equal(a.PartySlot, b.PartySlot); Assert.Equal(a.Build.Equipment, b.Build.Equipment);
            Assert.Equal(before.Character.Id, after.Character.Id); Assert.Equal(level, after.Character.Level);
            Assert.Equal(level == 40 ? 1 : 2, b.Build.Tier);
            Assert.Equal(before.Equipment.Select(e => e.Id), after.Equipment.Select(e => e.Id));
            Assert.Equal(before.EquippedEssences.Where((_, i) => i != index).Select(e => (e.Id, e.EssenceDefinitionId)),
                after.EquippedEssences.Select(e => (e.Id, e.EssenceDefinitionId)));
            Assert.Equal(a.Build.EssenceIds.Where((_, i) => i != index), b.Build.EssenceIds);
        }
        var runner = new TowerBattleRunner(root, content);
        _ = await runner.PrepareAsync(runner.CreateInput(changed with { Seeds = [1] }, 1, settings.Threat, settings.CheckpointIntervalTicks));
    }

    [Fact]
    public void Invalid_identity_maps_and_illegal_lower_levels_are_rejected()
    {
        var root = TestContentPaths.FindApiRoot(); var content = OfflineContent.ForTower(root, TowerBundle.ReadSettings(root));
        var source = TestSource(root, content); var build = TowerEssenceAblation.Remove(source, 2, 40, content).Party[0].Build;
        foreach (var map in new int[][] { [0, 1, 2, 3], [0, 1, 1, 3, 4], [0, 1, 2, 3, 6], [-1, 1, 2, 3, 4] })
            Assert.Throws<ArgumentException>(() => content.CreateBuild(build with { IdentityEssenceIndices = map }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(build with { IdentityProgression = null }));
        Assert.Throws<ArgumentException>(() => content.CreateBuild(build with { IdentityProgression = build.IdentityProgression! with { Tier = 3 } }));
        Assert.Throws<ArgumentException>(() => TowerEssenceAblation.Remove(source, 2, 30, content));
        Assert.Throws<InvalidDataException>(() => TowerEssenceAblation.Remove(source, 6, 40, content));
        Assert.Throws<InvalidDataException>(() => TowerEssenceAblation.Remove(TowerEssenceAblation.Remove(source, 0, 40, content), 0, 40, content));
    }

    [Fact]
    public void Acquisition_audit_uses_occupied_slots_and_actual_drop_and_rank_rules()
    {
        var root = TestContentPaths.FindApiRoot(); var content = OfflineContent.ForTower(root, TowerBundle.ReadSettings(root));
        var source = TestSource(root, content);
        var acquisition = JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment, Path.Combine(root, "Data/equipment/equipment-ordinary.v1.json"));
        var prices = JsonEquipmentUpgradePrices.Load(Path.Combine(root, "Data/equipment/equipment-upgrades.v1.json"));
        var row = TowerEquipmentAcquisitionAudit.Inspect(source, content, acquisition, prices);
        Assert.Equal(15, row.Characters); Assert.Equal(120, row.OccupiedSlots); Assert.Equal(0, row.MissingOrdinaryDefinitions);
        Assert.InRange(row.Items, 105, 120); Assert.Equal(.0005, row.ChampionLegendaryMasterpiecePerEquipmentDrop, 10);
        Assert.Equal(.00025, row.ChampionLegendaryMasterpiecePerCompletionAtMasteryZero, 10);
        Assert.Equal(.0005, row.ChampionLegendaryMasterpiecePerCompletionAtMasteryTen, 10);
        Assert.Equal(row.Items * 2000d, row.ExpectedDropsIgnoringFitAndRolls);
        Assert.Equal(new TowerReinforcementCost(1, 36000, 80280000), row.ReinforcementFromDungeonRank);
    }

    [ScreenFact]
    public async Task Complete_floor10_family_is_compared_with_five_essences_at_two_levels()
    {
        var q = TowerContractJson.Read<Request>(Environment.GetEnvironmentVariable("LL_FLOOR10_CONTROLS")!);
        Assert.Equal(Version, q.Version); Assert.Equal(SourcePin, q.SourcePin); Assert.False(Path.Exists(q.Output));
        foreach (var p in q.InputHashes.Keys.Concat(new[] { q.Source, q.Output, q.ApiRoot, q.Runtime })) TowerProposalStudy.Unlinked(p);
        void Recheck() { foreach (var pin in q.InputHashes) Assert.Equal(pin.Value, HarnessJson.FileHash(pin.Key)); }
        Recheck(); Assert.Equal(SourcePin, HarnessJson.FileHash(Path.Combine(q.Source, "files.json")));
        var files = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(q.Source, "files.json"));
        string Source(string name) { var p = Path.Combine(q.Source, name); Assert.Equal(files[name], HarnessJson.FileHash(p)); return p; }
        var prior = HarnessJson.Read<LoadoutScope>(Source("scope.json"));
        var settings = TowerBundle.ReadSettings(q.ApiRoot); Assert.Equal(HarnessJson.Hash(prior.Settings), HarnessJson.Hash(settings));
        var parents = HarnessJson.Read<Parent[]>(Source("cells.json")); Assert.Equal(ParentCells, parents.Length);
        string[] cases = ["floor10-authored", "floor10-retained-1", "floor10-retained-2"];
        string[] profiles = ["baseline", "precision", "ability-haste", "restorer-specialization", "armor-and-health", "resistance-and-health", "health-and-regeneration"];
        Assert.Equal(cases.SelectMany(c => profiles.Select(p => c + "/" + p)), parents.Select(p => p.Case + "/" + p.Profile));
        var panel = HarnessJson.Read<int[]>(Source("confirmation-seeds.json")); Assert.Equal(256, panel.Length); Assert.Equal(256, panel.Distinct().Count());
        var seeds = panel.Take(Samples).ToArray();
        var original = File.ReadLines(Source("study/trials.jsonl")).Select(l => JsonSerializer.Deserialize<LoadoutTrial>(l, HarnessJson.Options)!).ToArray();
        Assert.Equal(5376, original.Length);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(1200)); var token = deadline.Token;
        using var lease = TowerCompactBundle.AcquireWriter(q.Output); Directory.CreateDirectory(q.Output);
        var attempts = 0; var completed = 0; var matched = 0; var success = false; var watch = System.Diagnostics.Stopwatch.StartNew();
        void Save<T>(string name, T value) => HarnessJson.WriteNew(Path.Combine(q.Output, name), value);
        void Check() { token.ThrowIfCancellationRequested(); Assert.True(attempts <= Fights);
            Assert.True(TowerBulkCampaign.StorageBytes(q.Output, token) < 2L * 1073741824); }
        try
        {
            Save("request.json", q); Save("seeds.json", seeds);
            var root = Path.Combine(q.Output, "content"); var hashes = TowerBundle.CopyContent(q.ApiRoot, root, token);
            Assert.Equal(HarnessJson.Hash(q.ContentHashes), HarnessJson.Hash(hashes));
            TowerBundle.WriteSettings(Path.Combine(root, "appsettings.json"), settings);
            foreach (var extra in new[] { "equipment-ordinary.v1.json", "equipment-upgrades.v1.json" })
                File.Copy(Path.Combine(q.ApiRoot, "Data/equipment", extra), Path.Combine(root, "Data/equipment", extra), false);
            var scope = new LoadoutScope(Version, settings, ExecutionIdentity.Current(), hashes, "gzip-json-v1");
            Assert.Equal(HarnessJson.Hash(q.AssemblyHashes), HarnessJson.Hash(scope.Execution.AssemblyHashes)); Save("scope.json", scope);
            foreach (var path in Directory.EnumerateFiles(q.Runtime, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(q.Runtime, path); if (relative.StartsWith("Fixtures" + Path.DirectorySeparatorChar)) continue;
                var target = Path.Combine(q.Output, "executable", relative); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(path, target, false);
            }
            Save("runtime-files.json", F.Inventory(Path.Combine(q.Output, "executable")));
            var content = OfflineContent.ForTower(root, settings); var runner = new TowerBattleRunner(root, content);
            var cells = parents.Select(p => new Cell(p.Case + "/" + p.Profile, p.Case, p.Profile, 50, null, p.Scenario with { Seeds = panel })).ToList();
            foreach (var parent in parents)
                foreach (var level in new[] { 40, 50 })
                    for (var removed = 0; removed < 6; removed++)
                        cells.Add(new(parent.Case + "/" + parent.Profile + $"/level-{level}/remove-{removed + 1}", parent.Case, parent.Profile, level, removed,
                            TowerEssenceAblation.Remove(parent.Scenario, removed, level, content) with { Seeds = seeds }));
            Assert.Equal(Cells, cells.Count); Save("cells.json", cells);
            var acquisition = JsonStarterEquipmentCatalog.LoadOrdinary(content.Equipment, Path.Combine(root, "Data/equipment/equipment-ordinary.v1.json"));
            var prices = JsonEquipmentUpgradePrices.Load(Path.Combine(root, "Data/equipment/equipment-upgrades.v1.json"));
            Save("acquisition.json", cells.Take(ParentCells).Select(c => new { c.Id, audit = TowerEquipmentAcquisitionAudit.Inspect(c.Scenario, content, acquisition, prices) }).ToArray());
            using (new TowerPerformanceTrace(_ => throw new InvalidOperationException("Preparation cannot fight.")).Activate())
            {
                foreach (var cell in cells) { Check(); _ = await runner.PrepareAsync(runner.CreateInput(cell.Scenario, seeds[0], settings.Threat, settings.CheckpointIntervalTicks), token); }
                for (var i = 0; i < original.Length; i++)
                {
                    token.ThrowIfCancellationRequested(); var old = original[i]; var cell = cells[i / 256];
                    Assert.Equal(cell.Id, old.Stage); Assert.Equal(panel[i % 256], old.Seed); Assert.Equal(HarnessJson.Hash(cell.Scenario), old.Recipe);
                    Assert.Equal(HarnessJson.Hash(runner.CreateInput(cell.Scenario, old.Seed, settings.Threat, settings.CheckpointIntervalTicks)), old.InputHash);
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "input-matches.jsonl"), JsonSerializer.Serialize(new { old.Id, old.InputHash }) + "\n");
                }
            }
            Save("preflight.json", new { prepared = Cells, historicalInputsMatched = 5376, fights = 0 });
            Directory.CreateDirectory(Path.Combine(q.Output, "recipes")); Directory.CreateDirectory(Path.Combine(q.Output, "battles"));
            var archive = new TowerLoadoutArchive(q.Output, scope, Fights); var rows = new List<Row>();
            var outcomes = new Dictionary<string, bool[]>();
            foreach (var (cell, index) in cells.Select((c, i) => (c, i)))
            {
                Check(); var reports = new List<TowerBattleReport>();
                foreach (var (seed, sample) in seeds.Select((s, i) => (s, i)))
                {
                    token.ThrowIfCancellationRequested();
                    TowerWorkAccounting.AppendAllText(Path.Combine(q.Output, "attempts.jsonl"), JsonSerializer.Serialize(new { attempt = ++attempts, cell = cell.Id, seed }) + "\n");
                    var value = await archive.EvaluateAsync("fixed", cell.Id, cell.Scenario, seed, token); completed++; reports.Add(value.Report);
                    if (index < ParentCells)
                    {
                        var old = original[index * 256 + sample]; _ = Source("study/battles/" + old.Id + ".json.gz");
                        Assert.Equal(old.InputHash, value.Trial.InputHash); Assert.Equal(old.Recipe, value.Trial.Recipe);
                        Assert.Equal(HarnessJson.Hash(TowerLoadoutArchive.ReadBattle(Path.Combine(q.Source, "study"), old.Id, prior.ReportStorage)), HarnessJson.Hash(value.Report)); matched++;
                    }
                }
                var wins = reports.Select(r => r.Succeeded).ToArray(); var key = cell.SourceCase + "/" + cell.Profile;
                if (cell.RemovedIndex is null) outcomes.Add(key, wins);
                var before = outcomes[key];
                rows.Add(new(cell.Id, cell.SourceCase, cell.Profile, cell.Level, cell.Scenario.Party[0].Build.Tier, cell.RemovedIndex, cell.RemovedIndex is null ? 6 : 5,
                    wins.Count(w => w), reports.Count(r => r.Battle.Summary.ContentOutcome == BattleOutcome.Draw), Samples,
                    wins.Zip(before).Count(p => p.First && !p.Second), wins.Zip(before).Count(p => !p.First && p.Second),
                    reports.Average(r => r.GuardianHealthRemainingPercent), reports.Average(r => r.Battle.Summary.DurationSeconds)));
                if (index == ParentCells - 1) { Assert.Equal(672, matched); Save("runtime-qualification.json", new { status = "Matched", inputs = 5376, fullReports = matched }); }
            }
            Assert.Equal(Fights, attempts); Assert.Equal(Fights, completed); Assert.Equal(0, archive.CacheHits);
            var saved = archive.Trials;
            for (var i = 0; i < saved.Count; i++)
            {
                token.ThrowIfCancellationRequested(); var cell = cells[i / Samples]; var trial = saved[i];
                var input = runner.CreateInput(cell.Scenario, seeds[i % Samples], settings.Threat, settings.CheckpointIntervalTicks);
                Assert.Equal(HarnessJson.Hash(input), trial.InputHash); Assert.Equal(TowerLoadoutArchive.Key(scope, "fixed", input), trial.CacheKey);
            }
            Recheck(); Check(); Assert.Equal(HarnessJson.Hash(settings), HarnessJson.Hash(TowerBundle.ReadSettings(q.ApiRoot)));
            var controls = rows.Where(r => r.RemovedIndex is not null).ToArray();
            Save("result.json", new { status = "Floor10ControlScreenComplete", fights = completed, qualificationReports = matched,
                historicalInputsMatched = 5376, controlFights = 8064, samples = Samples, cells = Cells,
                newSeeds = 0, retries = 0, confirmedTeams = 0, balanceAcceptance = "NotAssessedHistoricalSeeds", rows,
                summary = new { bestReferenceWins = rows.Take(ParentCells).Max(r => r.Wins),
                    bestLevel40ControlWins = controls.Where(r => r.Level == 40).Max(r => r.Wins),
                    bestLevel50ControlWins = controls.Where(r => r.Level == 50).Max(r => r.Wins),
                    controlsAtOrAboveMinimum = controls.Count(r => r.Wins >= 4),
                    decision = controls.Any(r => r.Wins >= 4) ? "ObservedLowerEssenceBreach" : "NoObservedLowerEssenceBreach" },
                seconds = watch.Elapsed.TotalSeconds });
            success = true;
        }
        finally { Save("completion.json", new { status = success ? "Complete" : "Failed", attempts, completed, retries = 0 }); F.Seal(q.Output); }
    }
}
