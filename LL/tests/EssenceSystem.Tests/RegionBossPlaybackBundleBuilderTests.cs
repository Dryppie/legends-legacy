using System.IO.Compression;
using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.RegionBosses;
using Services.LL.RegionBosses;

namespace EssenceSystem.Tests;

public sealed class RegionBossPlaybackBundleBuilderTests
{
    [Fact]
    public void Maximum_duration_playback_uses_compact_frames_within_the_size_limit()
    {
        const int frameCount = RegionBossRules.EncounterTicks / 10;
        var friendly = Enumerable.Range(1, RegionBossRules.MaximumPartySize)
            .Select(index => CreateEntity($"player-{index}", $"Player {index}", 10_000))
            .ToArray();
        var hostile = new[] { CreateEntity("region-boss-level-1", "Region Boss", 1_000_000) };
        var checkpoints = Enumerable.Range(1, frameCount)
            .Select(sequence => new CombatCheckpoint(
                sequence,
                sequence * 10,
                friendly,
                hostile,
                friendly.Concat(hostile).Select(entity => CreateStats(entity, sequence)).ToArray(),
                [],
                sequence == frameCount,
                new CombatCheckpointContext(1, sequence / 60, [])))
            .ToArray();
        var resolution = new RegionBossCombatResolution(
            0,
            1,
            500_000,
            1_000_000,
            5_000,
            RegionBossRules.EncounterTicks,
            10,
            RegionBossTerminationReason.TimeExpired,
            [],
            new CombatResult { Duration = RegionBossRules.EncounterTicks },
            checkpoints);
        var builder = new RegionBossPlaybackBundleBuilder(
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            TimeProvider.System);

        var playback = builder.Build(Guid.NewGuid(), resolution);

        Assert.Equal(RegionBossPlayback.CompactBundleSchemaVersion, playback.SchemaVersion);
        Assert.Equal(frameCount, playback.FrameCount);
        using var compressed = new MemoryStream(playback.Artifact.BundleBytes);
        using var brotli = new BrotliStream(compressed, CompressionMode.Decompress);
        using var document = JsonDocument.Parse(brotli);
        var root = document.RootElement;
        Assert.Equal(RegionBossPlayback.CompactBundleSchemaVersion, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(frameCount, root.GetProperty("frames").GetArrayLength());
        Assert.Equal(RegionBossRules.MaximumPartySize + 1, root.GetProperty("entities").GetArrayLength());
        var firstFrame = root.GetProperty("frames")[0];
        Assert.True(firstFrame.GetProperty("isKeyframe").GetBoolean());
        Assert.True(firstFrame.TryGetProperty("entityStates", out _));
        Assert.False(firstFrame.TryGetProperty("friendly", out _));
        Assert.True(root.GetProperty("frames")[frameCount - 1].GetProperty("isKeyframe").GetBoolean());
    }

    [Fact]
    public void Delta_frames_retain_defeated_bosses_and_expired_summons_in_final_keyframe()
    {
        var checkpoints = new[]
        {
            Checkpoint(0, false,
                [Entity("player", "Player", 100), Entity("summon", "Summon", 50)],
                [Entity("boss-1", "Boss 1", 100)]),
            Checkpoint(10, false,
                [Entity("player", "Player", 90), Entity("summon", "Summon", 0)],
                [Entity("boss-1", "Boss 1", 0), Entity("boss-2", "Boss 2", 100)]),
            Checkpoint(20, false,
                [Entity("player", "Player", 80), Entity("summon", "Summon", 0)],
                [Entity("boss-1", "Boss 1", 0), Entity("boss-2", "Boss 2", 50)]),
            Checkpoint(30, true,
                [Entity("player", "Player", 70), Entity("summon", "Summon", 0)],
                [Entity("boss-1", "Boss 1", 0), Entity("boss-2", "Boss 2", 0)])
        };
        var playback = CreateBuilder().Build(Guid.NewGuid(), Resolution(checkpoints));

        using var document = ReadBundle(playback);
        var root = document.RootElement;
        var entities = root.GetProperty("entities").EnumerateArray()
            .ToDictionary(entity => entity.GetProperty("id").GetString()!, entity => entity.GetProperty("index").GetInt32());
        var frames = root.GetProperty("frames");

        Assert.True(frames[0].GetProperty("isKeyframe").GetBoolean());
        Assert.False(frames[2].GetProperty("isKeyframe").GetBoolean());
        Assert.DoesNotContain(
            frames[2].GetProperty("entityStates").EnumerateArray(),
            state => state.GetProperty("entityIndex").GetInt32() == entities["boss-1"]);
        Assert.DoesNotContain(
            frames[2].GetProperty("entityStates").EnumerateArray(),
            state => state.GetProperty("entityIndex").GetInt32() == entities["summon"]);

        var final = frames[3];
        Assert.True(final.GetProperty("isKeyframe").GetBoolean());
        Assert.Equal(4, final.GetProperty("entityStates").GetArrayLength());
        Assert.Equal(4, final.GetProperty("entityTotals").GetArrayLength());
        Assert.Equal(0, final.GetProperty("entityStates").EnumerateArray()
            .Single(state => state.GetProperty("entityIndex").GetInt32() == entities["boss-1"])
            .GetProperty("health").GetInt32());
        Assert.Equal(0, final.GetProperty("entityStates").EnumerateArray()
            .Single(state => state.GetProperty("entityIndex").GetInt32() == entities["summon"])
            .GetProperty("health").GetInt32());
    }

    private static RegionBossPlaybackBundleBuilder CreateBuilder() => new(
        new JsonSerializerOptions(JsonSerializerDefaults.Web),
        TimeProvider.System);

    private static RegionBossCombatResolution Resolution(IReadOnlyList<CombatCheckpoint> checkpoints) => new(
        1,
        2,
        0,
        100,
        10_000,
        checkpoints[^1].Tick,
        0,
        RegionBossTerminationReason.TimeExpired,
        [],
        new CombatResult { Duration = checkpoints[^1].Tick },
        checkpoints);

    private static CombatCheckpoint Checkpoint(
        int tick,
        bool isFinal,
        IReadOnlyList<SimpleCombatEntity> friendly,
        IReadOnlyList<SimpleCombatEntity> hostile)
    {
        var entities = friendly.Concat(hostile).ToArray();
        return new CombatCheckpoint(
            tick / 10,
            tick,
            friendly,
            hostile,
            entities.Select(entity => CreateStats(entity, Math.Max(1, tick / 10))).ToArray(),
            [],
            isFinal,
            new CombatCheckpointContext(tick < 10 ? 1 : 2, 0, []));
    }

    private static SimpleCombatEntity Entity(string id, string name, int health) => new()
    {
        Id = id,
        Name = name,
        ImagePath = $"/{id}.webp",
        Health = health,
        MaxHealth = 100,
        Level = 60
    };

    private static JsonDocument ReadBundle(RegionBossPlayback playback)
    {
        using var compressed = new MemoryStream(playback.Artifact.BundleBytes);
        using var brotli = new BrotliStream(compressed, CompressionMode.Decompress);
        return JsonDocument.Parse(brotli);
    }

    private static SimpleCombatEntity CreateEntity(string id, string name, int maxHealth) => new()
    {
        Id = id,
        Name = name,
        ImagePath = $"/{id}.webp",
        Health = maxHealth,
        MaxHealth = maxHealth,
        Barrier = 100,
        Level = 60
    };

    private static EntityStats CreateStats(SimpleCombatEntity entity, int sequence) => new(
        entity.Id,
        entity.Name,
        Enumerable.Range(1, 25)
            .Select(index => new AbilityStats(
                $"Ability {index}",
                TotalDamage: sequence * index,
                Uses: sequence,
                TotalThreat: sequence * index))
            .ToList(),
        DamageDone: sequence * 100,
        DamageTaken: sequence * 50,
        HealingDone: sequence * 10,
        HealingReceived: sequence * 10,
        BarrierGenerated: sequence * 5,
        DamageBlocked: sequence * 5,
        ThreatGenerated: sequence * 100);
}
