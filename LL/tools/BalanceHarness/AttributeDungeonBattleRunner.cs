using Common.Randomness;
using Domain.Models.Combat;
using Domain.Models.Dungeons.Definitions.Encounters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Resolution.Dungeon;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.PowerRatings;
using Services.LL.Interfaces.Combat.Resolution;

namespace BalanceHarness;

/// <summary>A single authored room at full health, without route rewards, vigor or run boons.</summary>
public sealed record AttributeDungeonScenario(string DungeonId, int RoomIndex);
public sealed class AttributeDungeonBattleRunner(string root, OfflineContent content)
{
    public const string ContentFile = "dungeons/dungeons.json";
    public async Task<BattleReport> RunAsync(AttributeDungeonScenario scenario, EquipmentReferenceBuildDefinition build,
        FixtureCombatStyle? doctrine, int seed, bool detailed, CancellationToken ct, Action<CombatEntity>? adjust = null)
    {
        var definitions = new DungeonDefinitionMaterializer(new DungeonCatalogValidator()).Materialize(
            HarnessJson.Read<DungeonCatalogDocument>(Path.Combine(root, "Data", ContentFile)));
        var dungeon = definitions.SingleOrDefault(x => x.Id == scenario.DungeonId)
            ?? throw new InvalidDataException("Unknown authored dungeon.");
        if (scenario.RoomIndex < 0 || scenario.RoomIndex >= dungeon.Rooms.Count)
            throw new InvalidDataException("Unknown authored room.");
        var room = dungeon.Rooms[scenario.RoomIndex];
        if (room.Type is not (Domain.Models.Dungeons.Definitions.Rooms.RoomType.Boss or Domain.Models.Dungeons.Definitions.Rooms.RoomType.MiniBoss))
            throw new InvalidDataException("Select a fixed boss room; ordinary room encounter IDs are a sampling pool.");
        var creatures = HarnessJson.Read<CreatureDocument>(Path.Combine(root, "Data", "world", "creatures.json")).Creatures;
        var enemies = room.EncounterIds.Select(key => creatures.SingleOrDefault(x => x.ImagePath.Equals(
            DungeonEncounterIdentity.NormalizeCreatureKey(key), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"Unresolved dungeon creature '{key}'.")).ToArray();
        var fixture = FixtureCharacter.From(content.CreateBuild(build));
        if (doctrine is not null) fixture = fixture with { CombatStyle = content.FreezeCombatStyle(fixture, doctrine) };
        var setup = content.CreateSetup(fixture.Materialize(content.Equipment), fixture.MaterializeEssences());
        var pipeline = new CombatPreparationPipeline(new TowerBattleRunner.FileSnapshotBuilder(content, setup), setup);
        var area = new Area { DifficultyTier = DungeonEnemyDifficultyScaling.GetProgressionPosition(dungeon.Tier, dungeon.Region) };
        var requests = new List<CombatantPreparationRequest>
        {
            new(new("friendly-1", fixture.Id, CombatSide.Friendly),
                new SnapshotCombatantPreparationSource(TowerBattleRunner.ToSnapshot(fixture, content)))
        };
        requests.AddRange(enemies.Select((enemy, index) => new CombatantPreparationRequest(
            new($"hostile-{index}", enemy.Id, CombatSide.Hostile), new LiveCombatantPreparationSource(enemy, area),
            actor => DungeonEnemyDifficultyScaling.Apply(actor, dungeon.Tier, dungeon.EnemyStrengthMultiplier))));
        var participants = await pipeline.PrepareAsync(CombatContentType.Dungeon, requests, ct);
        var encounter = StableRandom.Guid("attribute-dungeon", scenario.DungeonId, seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var plan = new CombatEncounterPlan(encounter, CombatMode.Dungeon, 1, DateTimeOffset.UnixEpoch,
            requests.Select(x => x.Slot).ToArray(), new DungeonEncounterSourceContext(encounter))
            { ContentType = CombatContentType.Dungeon, RandomSeed = seed };
        var runtime = new CombatEncounterRuntime(plan, participants.Where(x => x.Slot.Side == CombatSide.Friendly).ToArray(),
            participants.Where(x => x.Slot.Side == CombatSide.Hostile).ToArray());
        foreach (var participant in runtime.FriendlyParticipants) adjust?.Invoke(participant.Combatant);
        var prepared = IdleBattleRunner.DescribeParticipants(runtime);
        var result = await content.CreateExecutor().ExecuteSimulationAsync(runtime,
            new CombatRuleset(seed, 6000, CaptureEventLog: detailed), ct);
        return new(1, scenario.DungeonId, seed, FastCombatEngine.TicksPerSecond, prepared,
            BattleSummary.From(result, 6000), detailed ? result.EventLog : null);
    }
    private sealed record CreatureDocument(Creature[] Creatures);
}
