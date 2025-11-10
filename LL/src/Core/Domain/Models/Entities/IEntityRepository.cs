

namespace Domain.Models.Entities;
public interface IEntityRepository
{
    Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken);
    void UpdateEntities(List<Entity> playerCharacters);
}