
namespace Domain.Models.Entities;
public interface IEntityRepository
{
    Task<List<Entity>> GetEntitiesByIdsAsync(List<Guid> entityIds);
}