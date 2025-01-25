using Domain.Models.Entities.Creatures;
using Services.LL.Interfaces;

namespace Services.LL.Entities.Creatures;
public class CreatureService : ICreatureService
{
    private readonly ICreatureRepository _creatureRepository;
    public CreatureService(ICreatureRepository creatureRepository)
    {
        _creatureRepository = creatureRepository;
    }
    public async Task<List<Guid>> GetCreatureIdsByArea(string areaId, CancellationToken cancellationToken)
    {
        return await _creatureRepository.GetCreatureIdsByArea(areaId, cancellationToken);
    }
}