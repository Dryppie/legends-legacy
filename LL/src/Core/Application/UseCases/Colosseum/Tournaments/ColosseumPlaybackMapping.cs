using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Colosseum;
using AutoMapper;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Combat;

namespace Application.UseCases.Colosseum.Tournaments;

public sealed class ColosseumPlaybackMapping : IMapFrom<ColosseumPlaybackResult>
{
    public void Mapping(Profile profile) =>
        profile.CreateMap<ColosseumPlaybackResult, TournamentPlaybackBundleDto>()
            .ConvertUsing(source => Create(source));

    public static TournamentPlaybackBundleDto Create(ColosseumPlaybackResult playback)
    {
        var execution = playback.Execution;
        var entityById = new Dictionary<string, TournamentPlaybackEntityDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var checkpoint in execution.Checkpoints)
        {
            AddEntities(checkpoint.Friendly, true);
            AddEntities(checkpoint.Hostile, false);
        }

        var entities = entityById.Values.OrderBy(entity => entity.Index).ToArray();
        var abilityKeys = execution.Checkpoints
            .SelectMany(checkpoint => checkpoint.EntityStats)
            .Where(entity => entityById.ContainsKey(entity.EntityId))
            .SelectMany(entity => entity.Abilities.Select(ability =>
                (EntityIndex: entityById[entity.EntityId].Index, ability.Name)))
            .Distinct()
            .OrderBy(key => key.EntityIndex)
            .ThenBy(key => key.Name, StringComparer.Ordinal)
            .ToArray();
        var abilities = abilityKeys
            .Select((key, index) => new TournamentPlaybackAbilityDto(index, key.EntityIndex, key.Name))
            .ToArray();
        var abilityIndex = abilities.ToDictionary(
            ability => (ability.EntityIndex, ability.Name),
            ability => ability.Index);

        var materializedStates = new Dictionary<int, TournamentPlaybackEntityStateDto>();
        var materializedTotals = new Dictionary<int, TournamentPlaybackEntityTotalsDto>();
        var materializedAbilityTotals = new Dictionary<int, TournamentPlaybackAbilityTotalsDto>();
        var frames = new TournamentPlaybackFrameDto[execution.Checkpoints.Count];
        var lastKeyframeTick = int.MinValue;
        for (var checkpointIndex = 0; checkpointIndex < execution.Checkpoints.Count; checkpointIndex++)
        {
            var checkpoint = execution.Checkpoints[checkpointIndex];
            var currentStates = checkpoint.Friendly
                .Concat(checkpoint.Hostile)
                .Select(entity => new TournamentPlaybackEntityStateDto(
                    entityById[entity.Id].Index,
                    entity.Health,
                    entity.Barrier))
                .OrderBy(state => state.EntityIndex)
                .ToArray();
            var currentTotals = checkpoint.EntityStats
                .Where(entity => entityById.ContainsKey(entity.EntityId))
                .Select(entity => new TournamentPlaybackEntityTotalsDto(
                    entityById[entity.EntityId].Index,
                    entity.DamageDone,
                    entity.DamageTaken,
                    entity.HealingDone,
                    entity.HealingReceived,
                    entity.HealthRegenerated,
                    entity.BarrierGenerated,
                    entity.DamageBlocked,
                    entity.ThreatGenerated))
                .OrderBy(total => total.EntityIndex)
                .ToArray();
            var currentAbilityTotals = checkpoint.EntityStats
                .Where(entity => entityById.ContainsKey(entity.EntityId))
                .SelectMany(entity => entity.Abilities.Select(ability =>
                    new TournamentPlaybackAbilityTotalsDto(
                        abilityIndex[(entityById[entity.EntityId].Index, ability.Name)],
                        ability.Uses,
                        ability.TotalDamage,
                        ability.TotalHealing,
                        ability.TotalBarrier,
                        ability.TotalThreat,
                        ability.DamageByType?.ToArray())))
                .OrderBy(total => total.AbilityIndex)
                .ToArray();

            var isKeyframe = checkpointIndex == 0
                || checkpoint.IsFinal
                || checkpoint.Tick - lastKeyframeTick >= (30 * playback.TicksPerSecond);
            var states = ApplyChanges(
                currentStates,
                materializedStates,
                state => state.EntityIndex,
                isKeyframe);
            var totals = ApplyChanges(
                currentTotals,
                materializedTotals,
                total => total.EntityIndex,
                isKeyframe);
            var abilityTotals = ApplyChanges(
                currentAbilityTotals,
                materializedAbilityTotals,
                total => total.AbilityIndex,
                isKeyframe,
                AbilityTotalsEqual);
            if (isKeyframe)
                lastKeyframeTick = checkpoint.Tick;

            frames[checkpointIndex] = new TournamentPlaybackFrameDto(
                checkpoint.Sequence,
                checkpoint.Tick,
                isKeyframe,
                states,
                totals,
                abilityTotals,
                checkpoint.IsFinal,
                checkpoint.IsFinal ? execution.Result.Outcome : null);
        }

        return new TournamentPlaybackBundleDto(
            TournamentCombatReplay.CompactBundleSchemaVersion,
            playback.TicksPerSecond,
            playback.TicksPerFrame,
            execution.Result.Duration,
            entities,
            abilities,
            frames);

        static IReadOnlyList<T> ApplyChanges<T>(
            IEnumerable<T> current,
            IDictionary<int, T> materialized,
            Func<T, int> getIndex,
            bool isKeyframe,
            Func<T, T, bool>? equals = null)
        {
            equals ??= EqualityComparer<T>.Default.Equals;
            var changed = new List<T>();
            foreach (var value in current)
            {
                var index = getIndex(value);
                if (!materialized.TryGetValue(index, out var previous) || !equals(previous, value))
                    changed.Add(value);
                materialized[index] = value;
            }

            return isKeyframe
                ? materialized.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray()
                : changed;
        }

        static bool AbilityTotalsEqual(
            TournamentPlaybackAbilityTotalsDto left,
            TournamentPlaybackAbilityTotalsDto right) =>
            left.AbilityIndex == right.AbilityIndex
            && left.Uses == right.Uses
            && left.TotalDamage == right.TotalDamage
            && left.TotalHealing == right.TotalHealing
            && left.TotalBarrier == right.TotalBarrier
            && left.TotalThreat == right.TotalThreat
            && DamageByTypeEqual(left.DamageByType, right.DamageByType);

        static bool DamageByTypeEqual(
            IReadOnlyList<AbilityDamageTypeStats>? left,
            IReadOnlyList<AbilityDamageTypeStats>? right) =>
            ReferenceEquals(left, right)
            || left is not null && right is not null && left.SequenceEqual(right);

        void AddEntities(IEnumerable<SimpleCombatEntity> source, bool friendly)
        {
            foreach (var entity in source)
            {
                if (entityById.ContainsKey(entity.Id)) continue;
                entityById[entity.Id] = new TournamentPlaybackEntityDto(
                    entityById.Count,
                    entity.Id,
                    entity.Name,
                    entity.ImagePath,
                    friendly,
                    entity.MaxHealth,
                    entity.Level);
            }
        }
    }

}
