using Domain.Models.Regions.Areas;

namespace Services.LL.Spawnings;

public static class WeightedSpawnSelector
{
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
