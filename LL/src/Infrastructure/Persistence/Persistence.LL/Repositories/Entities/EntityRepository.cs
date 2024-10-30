using Common.Exceptions;
using Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Interfaces;

namespace Persistence.LL.Repositories.Entities;
public class EntityRepository : IEntityRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public EntityRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<Entity>> GetEntitiesByIdsAsync(List<Guid> entityIds)
    {
        var entityTasks = entityIds.Select(id => GetEntityByIdAsync(id, CancellationToken.None)).ToList();
        var entities = await Task.WhenAll(entityTasks);

        return entities.ToList();
    }

    public async Task<Entity> GetEntityByIdAsync(Guid entityId, CancellationToken cancellationToken)
    {
        var entity = await _unitOfWork.Context.Entities
            .Include(e => e.BaseAttributes)
            .Include(e => e.AbilityIds)
            .FirstOrDefaultAsync(e => e.Id.Equals(entityId), cancellationToken);
        
        NotFoundException.ThrowIfNull(entity, nameof(entity), entityId);

        return entity;
    }

}