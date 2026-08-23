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

public sealed class RegionBossPlaybackSizeLimitExceededException(
    string sizeKind,
    int actualBytes,
    int maximumBytes) : InvalidOperationException(
        $"Region Boss playback was {actualBytes} bytes and exceeded the {maximumBytes} byte {sizeKind} limit.")
{
    public string SizeKind { get; } = sizeKind;
    public int ActualBytes { get; } = actualBytes;
    public int MaximumBytes { get; } = maximumBytes;
}

public sealed class RegionBossPlaybackBundleBuilder(
    JsonSerializerOptions jsonOptions,
    TimeProvider timeProvider) : IRegionBossPlaybackBundleBuilder
{
    private const int TicksPerFrame = 10;
    private const int KeyframeIntervalTicks = 30 * FastCombatEngine.TicksPerSecond;
    private const int MaximumUncompressedBytes = 24 * 1024 * 1024;
    private const int MaximumCompressedBytes = 6 * 1024 * 1024;

    public RegionBossPlayback Build(Guid runId, RegionBossCombatResolution resolution)
    {
        if (resolution.Checkpoints.Count == 0 || !resolution.Checkpoints[^1].IsFinal)
            throw new InvalidOperationException("Region Boss playback has no final frame.");
        var bundle = CreateBundle(resolution);
        var uncompressed = JsonSerializer.SerializeToUtf8Bytes(bundle, jsonOptions);
        if (uncompressed.Length > MaximumUncompressedBytes)
            throw new RegionBossPlaybackSizeLimitExceededException(
                "uncompressed",
                uncompressed.Length,
                MaximumUncompressedBytes);
        var compressed = Compress(uncompressed);
        if (compressed.Length > MaximumCompressedBytes)
            throw new RegionBossPlaybackSizeLimitExceededException(
                "compressed",
                compressed.Length,
                MaximumCompressedBytes);
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

        var materializedStates = new Dictionary<int, RegionBossPlaybackEntityState>();
        var materializedTotals = new Dictionary<int, RegionBossPlaybackEntityTotals>();
        var materializedAbilityTotals = new Dictionary<int, RegionBossPlaybackAbilityTotals>();
        var frames = new RegionBossPlaybackFrame[resolution.Checkpoints.Count];
        var lastKeyframeTick = int.MinValue;
        for (var checkpointIndex = 0; checkpointIndex < resolution.Checkpoints.Count; checkpointIndex++)
        {
            var checkpoint = resolution.Checkpoints[checkpointIndex];
            var currentStates = checkpoint.Friendly
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
            var currentTotals = checkpoint.EntityStats
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
            var currentAbilityTotals = checkpoint.EntityStats
                .Where(stats => entityById.ContainsKey(stats.EntityId))
                .SelectMany(stats => stats.Abilities.Select(ability =>
                    new RegionBossPlaybackAbilityTotals(
                        abilityIndex[(entityById[stats.EntityId].Index, ability.Name)],
                        ability.Uses,
                        ability.TotalDamage,
                        ability.TotalHealing,
                        ability.TotalBarrier,
                        ability.DamageByType?.ToArray(),
                        ability.TotalThreat,
                        ability.TotalStagger,
                        ability.StaggerBreaks)))
                .OrderBy(totals => totals.AbilityIndex)
                .ToArray();

            var isKeyframe = checkpointIndex == 0
                || checkpoint.IsFinal
                || checkpoint.Tick - lastKeyframeTick >= KeyframeIntervalTicks;
            var states = ApplyStateChanges(currentStates, materializedStates, isKeyframe);
            var totals = ApplyEntityTotalChanges(currentTotals, materializedTotals, isKeyframe);
            var abilityTotals = ApplyAbilityTotalChanges(
                currentAbilityTotals,
                materializedAbilityTotals,
                isKeyframe);
            if (isKeyframe)
                lastKeyframeTick = checkpoint.Tick;

            frames[checkpointIndex] = new RegionBossPlaybackFrame(
                checkpoint.Sequence,
                checkpoint.Tick,
                isKeyframe,
                states,
                totals,
                abilityTotals,
                checkpoint.IsFinal,
                checkpoint.Context);
        }

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

        static IReadOnlyList<RegionBossPlaybackEntityState> ApplyStateChanges(
            IEnumerable<RegionBossPlaybackEntityState> current,
            IDictionary<int, RegionBossPlaybackEntityState> materialized,
            bool isKeyframe)
        {
            var changed = new List<RegionBossPlaybackEntityState>();
            foreach (var state in current)
            {
                if (!materialized.TryGetValue(state.EntityIndex, out var previous) || previous != state)
                    changed.Add(state);
                materialized[state.EntityIndex] = state;
            }

            return isKeyframe
                ? materialized.Values.OrderBy(state => state.EntityIndex).ToArray()
                : changed;
        }

        static IReadOnlyList<RegionBossPlaybackEntityTotals> ApplyEntityTotalChanges(
            IEnumerable<RegionBossPlaybackEntityTotals> current,
            IDictionary<int, RegionBossPlaybackEntityTotals> materialized,
            bool isKeyframe)
        {
            var changed = new List<RegionBossPlaybackEntityTotals>();
            foreach (var totals in current)
            {
                if (!materialized.TryGetValue(totals.EntityIndex, out var previous) || previous != totals)
                    changed.Add(totals);
                materialized[totals.EntityIndex] = totals;
            }

            return isKeyframe
                ? materialized.Values.OrderBy(totals => totals.EntityIndex).ToArray()
                : changed;
        }

        static IReadOnlyList<RegionBossPlaybackAbilityTotals> ApplyAbilityTotalChanges(
            IEnumerable<RegionBossPlaybackAbilityTotals> current,
            IDictionary<int, RegionBossPlaybackAbilityTotals> materialized,
            bool isKeyframe)
        {
            var changed = new List<RegionBossPlaybackAbilityTotals>();
            foreach (var totals in current)
            {
                if (!materialized.TryGetValue(totals.AbilityIndex, out var previous)
                    || !AbilityTotalsEqual(previous, totals))
                {
                    changed.Add(totals);
                }
                materialized[totals.AbilityIndex] = totals;
            }

            return isKeyframe
                ? materialized.Values.OrderBy(totals => totals.AbilityIndex).ToArray()
                : changed;
        }

        static bool AbilityTotalsEqual(
            RegionBossPlaybackAbilityTotals left,
            RegionBossPlaybackAbilityTotals right) =>
            left.AbilityIndex == right.AbilityIndex
            && left.Uses == right.Uses
            && left.TotalDamage == right.TotalDamage
            && left.TotalHealing == right.TotalHealing
            && left.TotalBarrier == right.TotalBarrier
            && DamageByTypeEqual(left.DamageByType, right.DamageByType)
            && left.TotalThreat == right.TotalThreat
            && left.TotalStagger == right.TotalStagger
            && left.StaggerBreaks == right.StaggerBreaks;

        static bool DamageByTypeEqual(
            IReadOnlyList<AbilityDamageTypeStats>? left,
            IReadOnlyList<AbilityDamageTypeStats>? right) =>
            ReferenceEquals(left, right)
            || left is not null && right is not null && left.SequenceEqual(right);

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
        bool IsKeyframe,
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
