using Domain.Models.Regions.Areas;

namespace Services.LL.Interfaces;
public interface IAreaService
{
    Task<Area?> GetAreaByIdAsync(string id);
    Task<IReadOnlyList<Area>> GetAllAreasAsync(CancellationToken cancellationToken);
}
