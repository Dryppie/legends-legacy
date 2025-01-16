

namespace Domain.Models.Entities;
public interface IEntityRepository
{
    Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken);
    Task UpdateEntities(List<Entity> playerCharacters, CancellationToken cancellationToken);
}