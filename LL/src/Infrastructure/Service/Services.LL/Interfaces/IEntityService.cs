using Domain.Models.Entities;

namespace Services.LL.Interfaces;

public interface IEntityService
{
    Task<List<Entity>> GetEntitiesByIdsAsync(List<Guid> entityIds);
}