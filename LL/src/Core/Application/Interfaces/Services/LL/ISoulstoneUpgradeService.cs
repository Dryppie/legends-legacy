using Domain.Models.Soulstones.UpgradeDefinition;

namespace Application.Interfaces.Services.LL;
public interface ISoulstoneUpgradeService
{
    Task<List<SoulstoneUpgradeView>> GetForCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> PurchaseAsync(Guid characterId, string upgradeId, CancellationToken cancellationToken);
    Task<bool> ResetSoulstoneUpgradesAsync(Guid characterId, CancellationToken cancellationToken);
}
