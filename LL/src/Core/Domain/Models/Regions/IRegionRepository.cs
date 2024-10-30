namespace Domain.Models.Regions;
public interface IRegionRepository
{
    /// <summary>
    /// Get the Region by Id
    /// </summary>
    /// <param name="regionId"></param>
    /// <returns></returns>
    Task<Region> GetRegionByIdAsync(int regionId, CancellationToken cancellationToken);
}
