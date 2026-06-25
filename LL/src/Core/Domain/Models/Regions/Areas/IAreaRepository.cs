using Domain.Models.Regions.Areas;

namespace Domain.Models.Regions.Areas;
public interface IAreaRepository
{
    Task<Area?> GetAreaByIdAsync(string id);
    Task<IReadOnlyList<Area>> GetAreasWithCreaturesAsync(CancellationToken cancellationToken);
    Task<int> CountByIdAsync(string areaId, CancellationToken cancellationToken);
}
