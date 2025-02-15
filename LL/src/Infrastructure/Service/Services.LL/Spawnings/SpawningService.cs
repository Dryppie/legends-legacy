using Domain.Models.Regions.Areas;
using Services.LL.CharacterActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.LL.Spawnings;
public class SpawningService : ISpawningService
{
    private readonly Random _random;
    public SpawningService()
    {
        // Use a single Random instance to avoid repeated seeding
        _random = new Random();
    }

    /// <summary>
    /// Determines how many monsters to spawn based on the
    /// provided counterProbabilities, which is a list of
    /// relative probabilities where the index corresponds to
    /// the number of monsters. E.g., index 0 = chance of spawning 0,
    /// index 1 = chance of spawning 1, etc.
    /// </summary>
    /// <param name="counterProbabilities">List of probabilities corresponding to 0..N monsters.</param>
    /// <returns>The chosen number of monsters to spawn.</returns>
    public int HowManyMonstersToSpawn(List<float> counterProbabilities)
    {
        if (counterProbabilities == null || counterProbabilities.Count == 0)
            throw new ArgumentException("counterProbabilities cannot be null or empty.");

        // Sum up all the probabilities to get a total "weight".
        float total = counterProbabilities.Sum();
        if (total <= 0)
            throw new ArgumentException("Sum of counterProbabilities must be greater than zero.");

        // Generate a random value between 0 and total.
        double randomValue = _random.NextDouble() * total;

        // Pick which "bucket" the random value falls into.
        float cumulative = 0f;
        for (int i = 0; i < counterProbabilities.Count; i++)
        {
            cumulative += counterProbabilities[i];
            if (randomValue <= cumulative)
            {
                return i + 1; // +1 because of list index
            }
        }

        // In case of floating-point inaccuracies, return 1 monster count by default.
        return 1;
    }

    /// <summary>
    /// Spawns multiple creatures by calling RandomSpawn once for each creature needed.
    /// </summary>
    /// <param name="creatures">A list of AreaCreature, each with a spawn rate.</param>
    /// <param name="count">How many creatures to spawn.</param>
    /// <returns>A list of the chosen creatures.</returns>
    public List<AreaCreature> WhatAreaCreaturesToSpawn(List<AreaCreature> creatures, int count)
    {
        var creaturesToSpawn = new List<AreaCreature>();
        if (creatures == null || creatures.Count == 0)
            return creaturesToSpawn;

        // Extract the spawn rates from each creature
        var spawnRates = creatures.Select(c => c.WeightedSpawnRate).ToList();

        for (int i = 0; i < count; i++)
        {
            // Randomly pick one creature index
            int chosenIndex = RandomSpawn(spawnRates);

            // Safety check: ensure the chosen index is in range
            if (chosenIndex >= 0 && chosenIndex < creatures.Count)
            {
                creaturesToSpawn.Add(creatures[chosenIndex]);
            }
        }

        return creaturesToSpawn;
    }

    /// <summary>
    /// Determines which single creature index to spawn based on the provided
    /// weightedSpawnRates. E.g., if weightedSpawnRates = [0.1, 0.7, 0.2],
    /// index 1 is the most likely to be returned.
    /// </summary>
    /// <param name="weightedSpawnRates">Relative spawn probabilities for each creature type.</param>
    /// <returns>The index of the creature chosen to spawn.</returns>
    public int RandomSpawn(List<float> weightedSpawnRates)
    {
        if (weightedSpawnRates == null || weightedSpawnRates.Count == 0)
            throw new ArgumentException("weightedSpawnRates cannot be null or empty.");

        float total = weightedSpawnRates.Sum();
        if (total <= 0)
            throw new ArgumentException("Sum of weightedSpawnRates must be greater than zero.");

        double randomValue = _random.NextDouble() * total;
        float cumulative = 0f;

        for (int i = 0; i < weightedSpawnRates.Count; i++)
        {
            cumulative += weightedSpawnRates[i];
            if (randomValue <= cumulative)
            {
                return i; // +1 because of list index
            }
        }

        // Fallback (should not normally happen unless floating precision issues)
        return 0;
    }
}
