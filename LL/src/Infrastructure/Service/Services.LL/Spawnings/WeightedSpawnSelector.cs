using Domain.Models.Regions.Areas;
using Domain.Models.Essences;

namespace Services.LL.Spawnings;

public static class WeightedSpawnSelector
{
    public static IReadOnlyList<AreaCreature> ApplyCreatureFocus(
        IReadOnlyList<AreaCreature> creatures, IReadOnlySet<Guid> focusedCreatureIds)
    {
        var total = creatures.Sum(creature => (double)creature.WeightedSpawnRate);
        var focused = creatures.Where(creature => focusedCreatureIds.Contains(creature.CreatureId))
            .Sum(creature => (double)creature.WeightedSpawnRate);
        if (total <= 0 || focused <= 0 || focused >= total) return creatures;

        var boosted = Math.Min(total, focused * CreatureFocusRules.SpawnChanceMultiplier);
        var otherMultiplier = (total - boosted) / (total - focused);
        return creatures.Select(creature => new AreaCreature
        {
            AreaId = creature.AreaId,
            CreatureId = creature.CreatureId,
            WeightedSpawnRate = (float)(creature.WeightedSpawnRate *
                (focusedCreatureIds.Contains(creature.CreatureId) ? boosted / focused : otherMultiplier))
        }).ToArray();
    }

    public static int SelectCreatureCount(
        IReadOnlyList<float> probabilities,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(probabilities);
        ArgumentNullException.ThrowIfNull(random);
        if (probabilities.Count == 0)
            throw new ArgumentException("Spawn probabilities cannot be empty.", nameof(probabilities));

        return SelectIndex(probabilities, random) + 1;
    }

    public static IReadOnlyList<AreaCreature> SelectCreatures(
        IReadOnlyList<AreaCreature> creatures,
        int count,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(creatures);
        ArgumentNullException.ThrowIfNull(random);
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (creatures.Count == 0)
            return [];

        var weights = creatures.Select(x => x.WeightedSpawnRate).ToArray();
        var selected = new List<AreaCreature>(count);
        for (var index = 0; index < count; index++)
            selected.Add(creatures[SelectIndex(weights, random)]);
        return selected;
    }

    public static int SelectIndex(IReadOnlyList<float> weights, Random random)
    {
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(random);
        if (weights.Count == 0)
            throw new ArgumentException("Weights cannot be empty.", nameof(weights));
        if (weights.Any(weight => weight < 0))
            throw new ArgumentException("Weights cannot contain negative values.", nameof(weights));

        var total = weights.Sum();
        if (total <= 0)
            throw new ArgumentException("The total weight must be greater than zero.", nameof(weights));

        var roll = random.NextDouble() * total;
        var cumulative = 0d;
        for (var index = 0; index < weights.Count; index++)
        {
            cumulative += weights[index];
            if (roll < cumulative)
                return index;
        }

        return weights.Count - 1;
    }
}
