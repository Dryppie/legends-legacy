using Domain.Models.Regions.Areas;

namespace Services.LL.CharacterActions;
public interface ISpawningService
{
    int HowManyMonstersToSpawn(List<float> counterProbabilities, Random? random = null);
    List<AreaCreature> WhatAreaCreaturesToSpawn(List<AreaCreature> creatures, int count, Random? random = null);
}
