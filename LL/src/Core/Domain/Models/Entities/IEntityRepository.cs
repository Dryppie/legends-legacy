

namespace Domain.Models.Entities;
public interface IEntityRepository
{
    Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds);
    Task UpdateEntities(List<Entity> playerCharacters, CancellationToken cancellationToken);
}