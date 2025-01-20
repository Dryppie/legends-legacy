using Domain.Models.Entities;

namespace Application.Interfaces.Services.LL;
public interface IEntityService
{
    Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken);
    Task UpdateEntities(List<Entity> playerCharacters, CancellationToken cancellationToken);
}