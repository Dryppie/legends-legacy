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
    public int HowManyMonstersToSpawn(List<float> counterProbabilities, Random? random = null)
    {
        return WeightedSpawnSelector.SelectCreatureCount(counterProbabilities, random ?? _random);
    }

    /// <summary>
    /// Spawns multiple creatures by calling RandomSpawn once for each creature needed.
    /// </summary>
    /// <param name="creatures">A list of AreaCreature, each with a spawn rate.</param>
    /// <param name="count">How many creatures to spawn.</param>
    /// <returns>A list of the chosen creatures.</returns>
    public List<AreaCreature> WhatAreaCreaturesToSpawn(List<AreaCreature> creatures, int count, Random? random = null)
    {
        return WeightedSpawnSelector.SelectCreatures(creatures, count, random ?? _random).ToList();
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
        return WeightedSpawnSelector.SelectIndex(weightedSpawnRates, _random);
    }
}
