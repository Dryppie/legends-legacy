using Domain.Models.Regions.Areas;

namespace Domain.Models.Regions.Areas;
public interface IAreaRepository
{
    Task<Area?> GetAreaByIdAsync(string id);
}
