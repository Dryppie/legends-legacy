using Application.Interfaces.Services.LL;
using Domain.Models.Regions;

namespace Services.LL.Regions;
public class RegionService : IRegionService
{
    private readonly  IRegionRepository _regionRepository;
    public RegionService(IRegionRepository regionRepository)
    {
        _regionRepository = regionRepository;
    }

    public async Task<Region> GetRegionByIdAsync(int regionId, CancellationToken cancellationToken)
    {
        return await _regionRepository.GetRegionByIdAsync(regionId, cancellationToken);
    }
}
