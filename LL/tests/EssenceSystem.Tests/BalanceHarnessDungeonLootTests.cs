using System.IO.Compression;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Interfaces.Combat.Resolution;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessDungeonLootTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static readonly Guid Owner = Guid.Parse("ce342272-8dcf-4532-8a82-5c241afbc03a");
    private static DungeonAcquisitionRun Recorded(int seed, bool complete, bool minibossSurvived = true) => new(
        "loot-boundary-test", "goblin_mines", seed, complete ? DungeonRunStatus.Completed : DungeonRunStatus.Failed,
        complete ? null : "Attrition", complete ? 1 : 0, 1, new Dictionary<string,int> { ["sigil_goblin_mines"] = 1 },
        [new(0, RoomType.MiniBoss, "fight", null, 100, minibossSurvived ? 70 : 0, minibossSurvived ? DungeonRunStatus.Active : DungeonRunStatus.Failed),
         .. minibossSurvived ? new[] { new DungeonAcquisitionAction(1, RoomType.Boss, "fight", null, 70, complete ? 40 : 0,
             complete ? DungeonRunStatus.Completed : DungeonRunStatus.Failed) } : []], [], [],
        JsonSerializer.SerializeToElement(new { rooms = new[] { new RoomInstance { RoomIndex = 0, Type = RoomType.MiniBoss }, new RoomInstance { RoomIndex = 1, Type = RoomType.Boss } } }, HarnessJson.Options), 0, 0);

    [Fact]
    public async Task Production_projection_replays_and_retains_claimed_descriptors()
    {
        var content = OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root));
        var model = new TowerDungeonLoot(Root, content); var items = 0; var blueprints = 0;
        for (var seed = 0; seed < 16; seed++)
        {
            var fork = model.Fork(); var run = Recorded(seed, true);
            var loot = await model.ApplyAsync(Owner, run, default);
            Assert.Equal(HarnessJson.Hash(loot), HarnessJson.Hash(await fork.ApplyAsync(Owner, run, default)));
            Assert.Equal(3, loot.RetryChecks);
            Assert.Equal(HarnessJson.Hash(loot.Pending.Where(r => r.ProgressionData != null).Select(r => r.ProgressionData).ToArray()), HarnessJson.Hash(loot.Equipment));
            Assert.All(loot.Equipment, item => { Assert.Equal(Owner, item.State.Ownership.OwnerId); Assert.Equal(1, item.State.Rank); Assert.Equal(1, item.State.Tier); });
            Assert.Empty(loot.Lost); items += loot.Equipment.Count; blueprints += loot.Blueprints.Values.Sum();
        }
        Assert.True(items > 0); Assert.True(blueprints >= 4);
    }

    [Fact]
    public async Task Later_failure_discards_miniboss_loot_and_never_claims_it()
    {
        var content = OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root)); var lost = 0;
        for (var seed = 0; seed < 16; seed++)
        {
            var model = new TowerDungeonLoot(Root, content);
            var loot = await model.ApplyAsync(Owner, Recorded(seed, false), default);
            Assert.Single(loot.Rolls); Assert.Empty(loot.Equipment); Assert.Empty(loot.Blueprints); Assert.Empty(loot.Pending);
            Assert.Empty(loot.After); Assert.Equal(1, loot.RetryChecks); lost += loot.Lost.Count;
        }
        Assert.True(lost > 0);
    }

    [Fact]
    public async Task Vigor_terminal_miniboss_does_not_reach_reward_callback()
    {
        var content = OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root));
        var loot = await new TowerDungeonLoot(Root, content).ApplyAsync(Owner, Recorded(1, false, false), default);
        Assert.Empty(loot.Rolls); Assert.Empty(loot.Pending); Assert.Empty(loot.Equipment); Assert.Empty(loot.Lost);
    }

    private sealed class ProjectionFactAttribute : FactAttribute
    {
        public ProjectionFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_DUNGEON_LOOT_PROJECTION"))) Skip = "Requires pinned archived first attempts; zero new fights."; }
    }
    [ProjectionFact]
    public async Task Project_first_earned_attempts_without_reusing_later_outcomes()
    {
        const string pin = "d70b1c4d85231a1b30518bd5f6a89ba3316eac2e3c2790be6b7aa1833335ad65";
        var output = Environment.GetEnvironmentVariable("LL_TOWER_DUNGEON_LOOT_PROJECTION")!;
        Assert.False(File.Exists(output));
        var repository = Path.GetFullPath(Path.Combine(Root, "../../../.."));
        var archive = Path.Combine(repository, "TestResults/tower-entry-readiness-study-20260929");
        Assert.Equal(pin, HarnessJson.FileHash(Path.Combine(archive, "files.json")));
        var manifest = HarnessJson.Read<Dictionary<string,string>>(Path.Combine(archive, "files.json"));
        var content = OfflineContent.ForTower(Root, TowerBundle.ReadSettings(Root));
        var inventory = new TowerActivityInventory(Root, content); var rows = new List<object>(); var changes = 0;
        foreach (var file in manifest.Keys.Where(k => k.EndsWith("--full-slot-ready--history.json")).Order())
        {
            Assert.Equal(manifest[file], HarnessJson.FileHash(Path.Combine(archive, file)));
            var history = HarnessJson.Read<JsonElement>(Path.Combine(archive, file));
            var step = history.GetProperty("steps")[0].Deserialize<TowerActivityAttempt>(HarnessJson.Options)!;
            Assert.Equal(manifest[step.File], HarnessJson.FileHash(Path.Combine(archive, step.File)));
            using var compressed = File.OpenRead(Path.Combine(archive, step.File)); using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            var run = (await JsonSerializer.DeserializeAsync<DungeonAcquisitionRun>(gzip, HarnessJson.Options))!;
            var owned = history.GetProperty("starting").Deserialize<List<Domain.Models.Items.Equipments.Progression.EquipmentData>>(HarnessJson.Options)!;
            foreach (var window in history.GetProperty("windows").EnumerateArray())
                if (window.GetProperty("until").GetInt32() <= step.Encounter)
                    owned.AddRange(window.GetProperty("equipment").Deserialize<Domain.Models.Items.Equipments.Progression.EquipmentData[]>(HarnessJson.Options)!);
            Assert.Equal(HarnessJson.Hash(step.Before.Equipment), HarnessJson.Hash(inventory.Select(owned)));
            var loot = await new TowerDungeonLoot(Root, content).ApplyAsync(step.Before.Id, run, default);
            if (step.Award is not null) owned.Add(step.Award);
            var baseline = inventory.Select(owned); owned.AddRange(loot.Equipment);
            var enhanced = inventory.Select(owned); var changed = HarnessJson.Hash(baseline) != HarnessJson.Hash(enhanced);
            if (changed) changes++;
            var character = step.Before with { Equipment = enhanced };
            var setup = content.CreateSetup(character.Materialize(content.Equipment), character.MaterializeEssences());
            var preparation = new CombatPreparationPipeline(new TowerBattleRunner.FileSnapshotBuilder(content, setup), setup);
            var prepared = await preparation.PrepareAsync(CombatContentType.Dungeon,
                [new(new("player", character.Id, CombatSide.Friendly, 1), new SnapshotCombatantPreparationSource(TowerBattleRunner.ToSnapshot(character, content)))], default);
            Assert.Equal(enhanced.Select(e => e.Data.State.Id).Order(), prepared.Single().Combatant.Equipment.Select(e => e.ProgressionData!.State.Id).Order());
            rows.Add(new { file, firstAttempt = step.File, character.Id, loot, baseline, enhanced, changed, preparedEquipment = prepared.Single().Combatant.Equipment.Select(e => e.ProgressionData).ToArray() });
        }
        Assert.Equal(32, rows.Count);
        HarnessJson.WriteNew(output, new { status = "FirstAttemptProjectionOnly", archivePin = pin, firstAttempts = rows.Count, changedLoadouts = changes, newFights = 0, newCombatSeeds = 0, rows });
    }

    private sealed class StudyFactAttribute : FactAttribute
    {
        public StudyFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LL_TOWER_DUNGEON_LOOT"))) Skip = "Requires frozen dungeon-loot owner."; }
    }
    [StudyFact]
    public async Task Frozen_paired_dungeon_loot()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(14));
        await TowerActivityStudy.RunDungeonLootAsync(HarnessJson.Read<TowerActivityRequest>(Environment.GetEnvironmentVariable("LL_TOWER_DUNGEON_LOOT")!), deadline.Token);
    }
}
