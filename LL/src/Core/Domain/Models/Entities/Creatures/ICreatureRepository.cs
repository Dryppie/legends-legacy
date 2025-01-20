namespace Domain.Models.Entities.Creatures;
public interface ICreatureRepository
{
    Task<List<Guid>> GetCreatureIdsByArea(string areaName, CancellationToken cancellationToken);
}