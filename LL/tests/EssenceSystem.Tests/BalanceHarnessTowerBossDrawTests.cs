using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBossDrawTests
{
    [Fact]
    public async Task Full_duration_Tower_draw_is_archived_and_replayed_with_identical_summary_and_participants()
    {
        var source = TestContentPaths.FindApiRoot();
        using var temp = new Temp();
        var frozen = Path.Combine(temp.Path, "content");
        var files = OfflineContent.Files.Append(TowerBattleRunner.FloorFile).Distinct().ToArray();
        var originalHashes = files.ToDictionary(f => f, f => HarnessJson.FileHash(Path.Combine(source, "Data", f)));
        foreach (var file in files)
        {
            var destination = Path.Combine(frozen, "Data", file);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(source, "Data", file), destination);
        }
        // Deliberately synthetic, temporary content guarantees the otherwise rare draw outcome.
        // Production preparation, RequiredSlots, the complete 6000-tick fight and replay stay intact.
        var floorsPath = Path.Combine(frozen, "Data", TowerBattleRunner.FloorFile);
        var floors = JsonNode.Parse(File.ReadAllText(floorsPath))!;
        var floor = floors["floors"]!.AsArray().Single(f => f!["floorNumber"]!.GetValue<int>() == 1)!;
        floor["guardianScaling"]!["health"] = 10000;
        floor["guardianScaling"]!["regeneration"] = 10000;
        floor["guardianScaling"]!["offense"] = 0.00000001;
        floor["guardianAbilityProfileId"] = "monster.test_tower_draw";
        File.WriteAllText(floorsPath, floors.ToJsonString(HarnessJson.Options));
        // Production chooses live creature abilities from the creature name, so the test
        // changes both the authored profile declaration and its actual runtime lookup.
        var creaturesPath = Path.Combine(frozen, "Data/world/creatures.json");
        var creatures = JsonNode.Parse(File.ReadAllText(creaturesPath))!;
        var guardianId = floor["guardianCreatureId"]!.GetValue<string>();
        creatures["creatures"]!.AsArray().Single(c => c!["id"]!.GetValue<string>() == guardianId)!["name"] = "Test Tower Draw";
        File.WriteAllText(creaturesPath, creatures.ToJsonString(HarnessJson.Options));
        var profilesPath = Path.Combine(frozen, "Data/combat/creature-abilities.json");
        var profiles = JsonNode.Parse(File.ReadAllText(profilesPath))!;
        // Reuse an authored passive self-dodge ability, with no damaging actions or summons.
        profiles["creatures"]!.AsArray().Add(JsonSerializer.SerializeToNode(new {
            monsterId = "monster.test_tower_draw", abilityIds = new[] { "ability.creature.vampire_bat.erratic_flight" }
        }));
        File.WriteAllText(profilesPath, profiles.ToJsonString(HarnessJson.Options));
        var contentHashes = files.ToDictionary(f => f, f => HarnessJson.FileHash(Path.Combine(frozen, "Data", f)));
        var scope = new LoadoutScope(TowerBossSearch.Version, new(new ThreatAndTankingOptions(), 10),
            ExecutionIdentity.Current(), contentHashes, "gzip-json-v1");
        Directory.CreateDirectory(Path.Combine(temp.Path, "recipes"));
        Directory.CreateDirectory(Path.Combine(temp.Path, "battles"));
        HarnessJson.WriteNew(Path.Combine(temp.Path, "scope.json"), scope);
        var recipe = HarnessJson.Read<TowerScenario>(Path.GetFullPath(Path.Combine(source,
            "../../../tools/BalanceHarness/Fixtures/tower-floor-1.json"))) with {
            Id = "test-only-full-duration-draw", Seeds = [771933],
            Assumptions = ["Synthetic temporary guardian content tests draw replay only; this is not boss-strategy evidence."]
        };
        var runner = new TowerBattleRunner(frozen, new OfflineContent(frozen, scope.Settings.Threat));
        var input = runner.CreateInput(recipe, recipe.Seeds[0], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
        Assert.Equal(6000, input.Rules.MaxTicks);
        Assert.Equal(5, input.Floor.RequiredSlots);
        Assert.Equal(input.Floor.RequiredSlots, input.Party.Count);
        var archive = new TowerLoadoutArchive(temp.Path, scope, maximumBattles: 1);
        var recorded = await archive.EvaluateAsync("draw-fixture", "confirmation", recipe, recipe.Seeds[0], default);
        Assert.Single(archive.Trials);
        Assert.Equal(0, archive.CacheHits);
        Assert.Equal(HarnessJson.Hash(input), recorded.Trial.InputHash);
        Assert.False(recorded.Report.Succeeded);
        Assert.Equal(BattleOutcome.Draw, recorded.Report.Battle.Summary.EngineOutcome);
        Assert.Equal(BattleOutcome.Draw, recorded.Report.Battle.Summary.ContentOutcome);
        Assert.Equal("TickLimit", recorded.Report.Battle.Summary.TerminationReason);
        Assert.Equal(6000, recorded.Report.Battle.Summary.DurationTicks);
        HarnessJson.WriteNew(Path.Combine(temp.Path, "files.json"), Directory.EnumerateFiles(temp.Path, "*", SearchOption.AllDirectories)
            .ToDictionary(p => Path.GetRelativePath(temp.Path, p).Replace('\\', '/'), HarnessJson.FileHash));
        var replay = await TowerLoadoutArchive.ReplayAsync(temp.Path, recorded.Trial.Id, detailed: true);
        Assert.NotEmpty(replay.Battle.EventLog!);
        Assert.Equal(HarnessJson.Hash(recorded.Report.Battle.PreparedParticipants), HarnessJson.Hash(replay.Battle.PreparedParticipants));
        Assert.Equal(HarnessJson.Hash(recorded.Report.Battle.Summary), HarnessJson.Hash(replay.Battle.Summary));
        Assert.Equal(recorded.Report.GuardianHealthRemainingPercent, replay.GuardianHealthRemainingPercent);
        Assert.Equal(recorded.Report.DisplayDurationSeconds, replay.DisplayDurationSeconds);
        Assert.Equal(BattleOutcome.Draw, replay.Battle.Summary.ContentOutcome);
        Assert.All(originalHashes, p => Assert.Equal(p.Value, HarnessJson.FileHash(Path.Combine(source, "Data", p.Key))));
    }

    private sealed class Temp : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tower-boss-draw-tests-" + Guid.NewGuid().ToString("N"));
        public Temp() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
