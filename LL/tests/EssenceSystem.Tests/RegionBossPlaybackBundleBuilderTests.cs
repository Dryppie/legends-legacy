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
        Assert.True(firstFrame.TryGetProperty("entityStates", out _));
        Assert.False(firstFrame.TryGetProperty("friendly", out _));
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
