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
        var bundle = CreateBundle(resolution);
        var uncompressed = JsonSerializer.SerializeToUtf8Bytes(bundle, jsonOptions);
        if (uncompressed.Length > MaximumUncompressedBytes)
            throw new InvalidOperationException(
                $"Region Boss playback exceeded the {MaximumUncompressedBytes} byte uncompressed limit.");
        var compressed = Compress(uncompressed);
        if (compressed.Length > MaximumCompressedBytes)
            throw new InvalidOperationException(
                $"Region Boss playback exceeded the {MaximumCompressedBytes} byte compressed limit.");
        var playback = new RegionBossPlayback
        {
            RegionBossRunId = runId,
            SchemaVersion = RegionBossPlayback.CompactBundleSchemaVersion,
            TicksPerSecond = FastCombatEngine.TicksPerSecond,
            TicksPerFrame = TicksPerFrame,
            TotalTicks = resolution.DurationTicks,
            FrameCount = bundle.Frames.Count,
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

    private static RegionBossPlaybackBundle CreateBundle(RegionBossCombatResolution resolution)
    {
        var entityById = new Dictionary<string, RegionBossPlaybackEntity>(StringComparer.OrdinalIgnoreCase);
        foreach (var checkpoint in resolution.Checkpoints)
        {
            AddEntities(checkpoint.Friendly, isFriendly: true);
            AddEntities(checkpoint.Hostile, isFriendly: false);
        }

        var entities = entityById.Values.OrderBy(entity => entity.Index).ToArray();
        var abilityKeys = resolution.Checkpoints
            .SelectMany(checkpoint => checkpoint.EntityStats)
            .Where(stats => entityById.ContainsKey(stats.EntityId))
            .SelectMany(stats => stats.Abilities.Select(ability =>
                (EntityIndex: entityById[stats.EntityId].Index, ability.Name)))
            .Distinct()
            .OrderBy(key => key.EntityIndex)
            .ThenBy(key => key.Name, StringComparer.Ordinal)
            .ToArray();
        var abilities = abilityKeys
            .Select((key, index) => new RegionBossPlaybackAbility(index, key.EntityIndex, key.Name))
            .ToArray();
        var abilityIndex = abilities.ToDictionary(
            ability => (ability.EntityIndex, ability.Name),
            ability => ability.Index);

        var frames = resolution.Checkpoints.Select(checkpoint =>
        {
            var states = checkpoint.Friendly
                .Concat(checkpoint.Hostile)
                .Select(entity => new RegionBossPlaybackEntityState(
                    entityById[entity.Id].Index,
                    entity.Health,
                    entity.Barrier,
                    entity.CurrentStagger,
                    entity.MaxStagger,
                    entity.IsStaggered,
                    entity.IsStaggerRecovering))
                .OrderBy(state => state.EntityIndex)
                .ToArray();
            var totals = checkpoint.EntityStats
                .Where(stats => entityById.ContainsKey(stats.EntityId))
                .Select(stats => new RegionBossPlaybackEntityTotals(
                    entityById[stats.EntityId].Index,
                    stats.DamageDone,
                    stats.DamageTaken,
                    stats.HealingDone,
                    stats.HealingReceived,
                    stats.HealthRegenerated,
                    stats.BarrierGenerated,
                    stats.DamageBlocked,
                    stats.ThreatGenerated,
                    stats.StaggerContributed,
                    stats.StaggerBreaks))
                .OrderBy(totals => totals.EntityIndex)
                .ToArray();
            var abilityTotals = checkpoint.EntityStats
                .Where(stats => entityById.ContainsKey(stats.EntityId))
                .SelectMany(stats => stats.Abilities.Select(ability =>
                    new RegionBossPlaybackAbilityTotals(
                        abilityIndex[(entityById[stats.EntityId].Index, ability.Name)],
                        ability.Uses,
                        ability.TotalDamage,
                        ability.TotalHealing,
                        ability.TotalBarrier,
                        ability.DamageByType,
                        ability.TotalThreat,
                        ability.TotalStagger,
                        ability.StaggerBreaks)))
                .OrderBy(totals => totals.AbilityIndex)
                .ToArray();
            return new RegionBossPlaybackFrame(
                checkpoint.Sequence,
                checkpoint.Tick,
                states,
                totals,
                abilityTotals,
                checkpoint.IsFinal,
                checkpoint.Context);
        }).ToArray();

        return new RegionBossPlaybackBundle(
            RegionBossPlayback.CompactBundleSchemaVersion,
            FastCombatEngine.TicksPerSecond,
            TicksPerFrame,
            resolution.DurationTicks,
            resolution.HighestLevelDefeated,
            resolution.CurrentBossLevel,
            resolution.TerminationReason,
            entities,
            abilities,
            frames);

        void AddEntities(IEnumerable<SimpleCombatEntity> source, bool isFriendly)
        {
            foreach (var entity in source)
            {
                if (entityById.ContainsKey(entity.Id))
                    continue;
                entityById[entity.Id] = new RegionBossPlaybackEntity(
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

    private sealed record RegionBossPlaybackBundle(
        int SchemaVersion,
        int TicksPerSecond,
        int TicksPerFrame,
        int TotalTicks,
        int HighestLevelDefeated,
        int CurrentBossLevel,
        RegionBossTerminationReason TerminationReason,
        IReadOnlyList<RegionBossPlaybackEntity> Entities,
        IReadOnlyList<RegionBossPlaybackAbility> Abilities,
        IReadOnlyList<RegionBossPlaybackFrame> Frames);

    private sealed record RegionBossPlaybackEntity(
        int Index,
        string Id,
        string Name,
        string ImagePath,
        bool IsFriendly,
        int MaxHealth,
        int Level,
        int? PartyNumber);

    private sealed record RegionBossPlaybackAbility(int Index, int EntityIndex, string Name);

    private sealed record RegionBossPlaybackFrame(
        int Sequence,
        int Tick,
        IReadOnlyList<RegionBossPlaybackEntityState> EntityStates,
        IReadOnlyList<RegionBossPlaybackEntityTotals> EntityTotals,
        IReadOnlyList<RegionBossPlaybackAbilityTotals> AbilityTotals,
        bool IsFinal,
        CombatCheckpointContext? Context);

    private sealed record RegionBossPlaybackEntityState(
        int EntityIndex,
        int Health,
        int Barrier,
        int CurrentStagger,
        int MaxStagger,
        bool IsStaggered,
        bool IsStaggerRecovering);

    private sealed record RegionBossPlaybackEntityTotals(
        int EntityIndex,
        int DamageDone,
        int DamageTaken,
        int HealingDone,
        int HealingReceived,
        int HealthRegenerated,
        int BarrierGenerated,
        int DamageBlocked,
        int ThreatGenerated,
        int StaggerContributed,
        int StaggerBreaks);

    private sealed record RegionBossPlaybackAbilityTotals(
        int AbilityIndex,
        int Uses,
        int TotalDamage,
        int TotalHealing,
        int TotalBarrier,
        IReadOnlyList<AbilityDamageTypeStats>? DamageByType,
        int TotalThreat,
        int TotalStagger,
        int StaggerBreaks);
}
