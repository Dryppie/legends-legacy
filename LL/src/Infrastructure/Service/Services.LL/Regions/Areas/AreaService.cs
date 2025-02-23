using Domain.Models.Regions.Areas;
using Services.LL.Interfaces;

namespace Services.LL.Regions.Areas;
public class AreaService : IAreaService
{
    public IAreaRepository _areaRepository;
    public AreaService(IAreaRepository areaRepository)
    {
        _areaRepository = areaRepository;
    }

    public async Task<Area> GetAreaByIdAsync(string id)
    {
        return await _areaRepository.GetAreaByIdAsync(id);
    }
}
