using Domain.Models.Guilds.Buildings;

namespace Application.Interfaces.Services.LL;
public interface IGuildBuildingUpgradeService
{
    Task<List<BuildingUpgradeView>> GetForGuildAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> PurchaseAsync(Guid characterId, string upgradeId, CancellationToken cancellationToken);
}
