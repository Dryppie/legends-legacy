using Domain.Models.Entities;

namespace Services.LL.Interfaces;

public interface IEntityService
{
    Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken);
    Task UpdateEntities(List<Entity> playerCharacters, CancellationToken cancellationToken);
}