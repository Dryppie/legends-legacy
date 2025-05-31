using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Entities;

namespace Services.LL.Entities;
public class EntityService : IEntityService
{
    private readonly IEntityRepository _entityRepository;

    public EntityService(IEntityRepository entityRepository)
    {
        _entityRepository = entityRepository;
    }

    public async Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds, CancellationToken cancellationToken)
    {
        return await _entityRepository.GetEntitiesByIdsForCombatAsync(entityIds, cancellationToken);
    }

    public async Task UpdateEntities(List<Entity> playerCharacters, CancellationToken cancellationToken)
    {
        await _entityRepository.UpdateEntities(playerCharacters, cancellationToken);
    }

}