using Common.Primitives;
using Domain.Models.Soulstones.UpgradeDefinition;

namespace Application.Interfaces.Services.LL;

public interface ISoulstoneUpgradeService
{
    Task<List<SoulstoneUpgradeView>> GetForCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Response<SoulstoneUpgradeMutationResult>> PurchaseAsync(Guid characterId, string upgradeId, CancellationToken cancellationToken);
    Task<Response<SoulstoneUpgradeMutationResult>> ResetSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken);
}
