using Domain.Models.Soulstones.UpgradeDefinition;

namespace Application.Interfaces.Services.LL;
public interface ISoulstoneUpgradeService
{
    Task<List<SoulstoneUpgradeView>> GetForCharacterAsync(Guid characterId, CancellationToken cancellationToken);
    Task<Dictionary<string, double>> GetSoulstoneBonusesByCharacterIdAsync(Guid characterId, string[] upgrades, CancellationToken cancellationToken);
    Task<bool> PurchaseAsync(Guid characterId, string upgradeId, CancellationToken cancellationToken);
}
