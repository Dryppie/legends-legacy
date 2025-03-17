using Domain.Models.Regions.Areas;

namespace Domain.Models.Entities.Creatures;
public interface ICreatureRepository
{
    Task<List<Guid>> GetCreatureIdsByArea(string areaId, CancellationToken cancellationToken);
    Task<List<Creature>> GetCreaturesAsync(CancellationToken cancellationToken);
}