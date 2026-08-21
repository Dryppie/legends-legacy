using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Application.UseCases.Raids.Dtos;
using Domain.Models.Combat;
using Domain.Models.Raids;
using Services.LL.Combat.Engine;

namespace Services.LL.Raids;

public interface IRaidPlaybackBundleBuilder
{
    RaidPlayback Build(Guid raidRunId, RaidLanePlaybackCapture capture);
}

public sealed class RaidPlaybackBundleBuilder(
    JsonSerializerOptions jsonOptions,
    TimeProvider timeProvider) : IRaidPlaybackBundleBuilder
{
    private const int TicksPerFrame = 10;
    private const int MaximumUncompressedBytes = 16 * 1024 * 1024;
    private const int MaximumCompressedBytes = 4 * 1024 * 1024;

    public RaidPlayback Build(Guid raidRunId, RaidLanePlaybackCapture capture)
    {
        if (capture.Checkpoints.Count == 0 || !capture.Checkpoints[^1].IsFinal)
            throw new InvalidOperationException($"Raid {capture.Lane} playback has no final frame.");

        var bundle = CreateBundle(capture);
        var uncompressed = JsonSerializer.SerializeToUtf8Bytes(bundle, jsonOptions);
        if (uncompressed.Length > MaximumUncompressedBytes)
            throw new InvalidOperationException(
                $"Raid {capture.Lane} playback exceeded the {MaximumUncompressedBytes} byte uncompressed limit.");

        var compressed = Compress(uncompressed);
        if (compressed.Length > MaximumCompressedBytes)
            throw new InvalidOperationException(
                $"Raid {capture.Lane} playback exceeded the {MaximumCompressedBytes} byte compressed limit.");

        var playback = new RaidPlayback
        {
            RaidRunId = raidRunId,
            Lane = capture.Lane,
            SchemaVersion = RaidPlayback.CompactBundleSchemaVersion,
            TicksPerSecond = FastCombatEngine.TicksPerSecond,
            TicksPerFrame = TicksPerFrame,
            TotalTicks = capture.Result.Duration,
            FrameCount = bundle.Frames.Count,
            BundleHash = Convert.ToHexString(SHA256.HashData(compressed)).ToLowerInvariant(),
            BundleLength = compressed.Length,
            CreatedAt = timeProvider.GetUtcNow()
        };
        playback.Artifact = new RaidPlaybackArtifact
        {
            RaidPlaybackId = playback.Id,
            Playback = playback,
            BundleBytes = compressed
        };
        return playback;
    }

    private static RaidPlaybackBundleDto CreateBundle(RaidLanePlaybackCapture capture)
    {
        var entityById = new Dictionary<string, RaidPlaybackEntityDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var checkpoint in capture.Checkpoints)
        {
            AddEntities(checkpoint.Friendly, isFriendly: true);
            AddEntities(checkpoint.Hostile, isFriendly: false);
        }

        var entities = entityById.Values.OrderBy(x => x.Index).ToArray();
        var abilityKeys = capture.Checkpoints
            .SelectMany(x => x.EntityStats)
            .Where(x => entityById.ContainsKey(x.EntityId))
            .SelectMany(entity => entity.Abilities.Select(ability =>
                (EntityIndex: entityById[entity.EntityId].Index, ability.Name)))
            .Distinct()
            .OrderBy(x => x.EntityIndex)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToArray();
        var abilities = abilityKeys
            .Select((key, index) => new RaidPlaybackAbilityDto(index, key.EntityIndex, key.Name))
            .ToArray();
        var abilityIndex = abilities.ToDictionary(
            x => (x.EntityIndex, x.Name),
            x => x.Index);

        var frames = capture.Checkpoints.Select(checkpoint =>
        {
            var state = checkpoint.Friendly
                .Concat(checkpoint.Hostile)
                .Select(entity => new RaidPlaybackEntityStateDto(
                    entityById[entity.Id].Index,
                    entity.Health,
                    entity.Barrier,
                    entity.CurrentStagger,
                    entity.MaxStagger,
                    entity.IsStaggered,
                    entity.IsStaggerRecovering))
                .OrderBy(x => x.EntityIndex)
                .ToArray();
            var totals = checkpoint.EntityStats
                .Where(entity => entityById.ContainsKey(entity.EntityId))
                .Select(entity => new RaidPlaybackEntityTotalsDto(
                    entityById[entity.EntityId].Index,
                    entity.DamageDone,
                    entity.DamageTaken,
                    entity.HealingDone,
                    entity.HealingReceived,
                    entity.HealthRegenerated,
                    entity.BarrierGenerated,
                    entity.DamageBlocked,
                    entity.ThreatGenerated,
                    entity.StaggerContributed,
                    entity.StaggerBreaks))
                .OrderBy(x => x.EntityIndex)
                .ToArray();
            var abilityTotals = checkpoint.EntityStats
                .Where(entity => entityById.ContainsKey(entity.EntityId))
                .SelectMany(entity => entity.Abilities.Select(ability =>
                    new RaidPlaybackAbilityTotalsDto(
                        abilityIndex[(entityById[entity.EntityId].Index, ability.Name)],
                        ability.Uses,
                        ability.TotalDamage,
                        ability.TotalHealing,
                        ability.TotalBarrier,
                        ability.DamageByType,
                        ability.TotalThreat,
                        ability.TotalStagger,
                        ability.StaggerBreaks)))
                .OrderBy(x => x.AbilityIndex)
                .ToArray();
            return new RaidPlaybackFrameDto(
                checkpoint.Sequence,
                checkpoint.Tick,
                state,
                totals,
                abilityTotals,
                checkpoint.IsFinal,
                checkpoint.IsFinal ? capture.Result.Outcome : null);
        }).ToArray();

        return new RaidPlaybackBundleDto(
            RaidPlayback.CompactBundleSchemaVersion,
            FastCombatEngine.TicksPerSecond,
            TicksPerFrame,
            capture.Result.Duration,
            entities,
            abilities,
            frames);

        void AddEntities(IEnumerable<SimpleCombatEntity> source, bool isFriendly)
        {
            foreach (var entity in source)
            {
                if (entityById.ContainsKey(entity.Id))
                    continue;
                entityById[entity.Id] = new RaidPlaybackEntityDto(
                    entityById.Count,
                    entity.Id,
                    entity.Name,
                    entity.ImagePath,
                    isFriendly,
                    entity.MaxHealth,
                    entity.Level,
                    entity.PartyNumber);
            }
        }
    }

    private static byte[] Compress(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            brotli.Write(bytes);
        return output.ToArray();
    }
}
