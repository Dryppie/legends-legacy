using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;

namespace Services.LL.Entities.Creatures;

public class CombatEncounterFactory(ICreatureScaler creatureScaler)
{
    private readonly ICreatureScaler _creatureScaler = creatureScaler;

    public List<Creature> BuildEncounter(Area area, Character character)
    {
        var creatures = PickCreaturesForArea(area); // uses AreaCreatureSpawn weights, etc.

        foreach (var creature in creatures)
        {
            _creatureScaler.ApplyScaling(creature, area);
        }

        return creatures;
    }

    private List<Creature> PickCreaturesForArea(Area area)
    {
        // stub: roll from AreaCreatureSpawn weights
        throw new NotImplementedException();
    }
}
