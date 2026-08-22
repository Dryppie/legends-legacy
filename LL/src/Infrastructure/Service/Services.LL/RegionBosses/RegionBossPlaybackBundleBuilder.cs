using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Domain.Models.Combat;
using Domain.Models.RegionBosses;
using Services.LL.Combat.Engine;

namespace Services.LL.RegionBosses;

public interface IRegionBossPlaybackBundleBuilder
{
    RegionBossPlayback Build(Guid runId, RegionBossCombatResolution resolution);
}

public sealed class RegionBossPlaybackBundleBuilder(
    JsonSerializerOptions jsonOptions,
    TimeProvider timeProvider) : IRegionBossPlaybackBundleBuilder
{
    private const int TicksPerFrame = 10;
    private const int MaximumUncompressedBytes = 24 * 1024 * 1024;
    private const int MaximumCompressedBytes = 6 * 1024 * 1024;

    public RegionBossPlayback Build(Guid runId, RegionBossCombatResolution resolution)
    {
        if (resolution.Checkpoints.Count == 0 || !resolution.Checkpoints[^1].IsFinal)
            throw new InvalidOperationException("Region Boss playback has no final frame.");
        var bundle = new RegionBossPlaybackBundle(
            RegionBossPlayback.CompactBundleSchemaVersion,
            FastCombatEngine.TicksPerSecond,
            TicksPerFrame,
            resolution.DurationTicks,
            resolution.HighestLevelDefeated,
            resolution.CurrentBossLevel,
            resolution.TerminationReason,
            resolution.Checkpoints);
        var uncompressed = JsonSerializer.SerializeToUtf8Bytes(bundle, jsonOptions);
        if (uncompressed.Length > MaximumUncompressedBytes)
            throw new InvalidOperationException("Region Boss playback exceeded its uncompressed size limit.");
        var compressed = Compress(uncompressed);
        if (compressed.Length > MaximumCompressedBytes)
            throw new InvalidOperationException("Region Boss playback exceeded its compressed size limit.");
        var playback = new RegionBossPlayback
        {
            RegionBossRunId = runId,
            SchemaVersion = RegionBossPlayback.CompactBundleSchemaVersion,
            TicksPerSecond = FastCombatEngine.TicksPerSecond,
            TicksPerFrame = TicksPerFrame,
            TotalTicks = resolution.DurationTicks,
            FrameCount = resolution.Checkpoints.Count,
            BundleHash = Convert.ToHexString(SHA256.HashData(compressed)).ToLowerInvariant(),
            BundleLength = compressed.Length,
            CreatedAtUtc = timeProvider.GetUtcNow()
        };
        playback.Artifact = new RegionBossPlaybackArtifact
        {
            RegionBossRunId = runId,
            Playback = playback,
            BundleBytes = compressed
        };
        return playback;
    }

    private static byte[] Compress(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            brotli.Write(bytes);
        return output.ToArray();
    }

    private sealed record RegionBossPlaybackBundle(
        int SchemaVersion,
        int TicksPerSecond,
        int TicksPerFrame,
        int TotalTicks,
        int HighestLevelDefeated,
        int CurrentBossLevel,
        RegionBossTerminationReason TerminationReason,
        IReadOnlyList<CombatCheckpoint> Frames);
}
