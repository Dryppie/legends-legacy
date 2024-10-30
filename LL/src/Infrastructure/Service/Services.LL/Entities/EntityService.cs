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

    public Task<List<Entity>> GetEntitiesByIdsAsync(List<Guid> entityIds)
    {
        return _entityRepository.GetEntitiesByIdsAsync(entityIds);
    }
}