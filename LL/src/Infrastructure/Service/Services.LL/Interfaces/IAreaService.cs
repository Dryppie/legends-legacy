using Domain.Models.Regions.Areas;

namespace Services.LL.Interfaces;
public interface IAreaService
{
    Task<Area> GetAreaByIdAsync(string id);
}
