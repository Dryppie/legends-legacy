using Domain.Models.Achievements;
using Domain.Models.WorldTower;

namespace Application.Interfaces.Services.LL.WorldTower;

public interface IWorldTowerTitleService
{
    Task<TitleDefinition> GetRewardAsync(TowerFloorDefinition floor, CancellationToken cancellationToken);
    Task<int> GrantAsync(TowerFloorDefinition floor, IReadOnlyCollection<TowerTitleRecipient> recipients,
        bool announce, CancellationToken cancellationToken);
    Task<int> BackfillAsync(int floorNumber, CancellationToken cancellationToken);
}
