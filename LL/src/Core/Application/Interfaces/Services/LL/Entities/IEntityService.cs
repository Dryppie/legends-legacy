using Domain.Models.Entities;

namespace Application.Interfaces.Services.LL.Entities;
public interface IEntityService
{
    Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken);
    void UpdateEntities(List<Entity> playerCharacters);
}