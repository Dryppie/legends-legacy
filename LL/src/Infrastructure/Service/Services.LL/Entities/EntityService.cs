using Domain.Models.Entities;
using Services.LL.CharacterActions;
using Services.LL.Interfaces;

namespace Services.LL.Entities;
public class EntityService : IEntityService
{
    private readonly IEntityRepository _entityRepository;

    public EntityService(IEntityRepository entityRepository)
    {
        _entityRepository = entityRepository;
    }

    public async Task<List<Entity>> GetEntitiesByIdsForCombatAsync(List<Guid> entityIds)
    {
        return await _entityRepository.GetEntitiesByIdsForCombatAsync(entityIds);
    }

    public async Task UpdateEntities(List<Entity> playerCharacters, CancellationToken cancellationToken)
    {
        await _entityRepository.UpdateEntities(playerCharacters, cancellationToken);
    }

}