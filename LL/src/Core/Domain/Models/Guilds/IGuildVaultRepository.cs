using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;

namespace Domain.Models.Guilds;

public interface IGuildVaultRepository
{
    Task<GuildMember?> GetMemberAsync(Guid characterId, CancellationToken ct);
    Task<InventoryItem?> GetDonationAsync(Guid characterId, Guid equipmentId, CancellationToken ct);
    Task<bool> IsEquippedAsync(Guid equipmentId, CancellationToken ct);
    Task<bool> IsInVaultAsync(Guid equipmentId, CancellationToken ct);
    Task<bool> IsInInventoryAsync(Guid equipmentId, CancellationToken ct);
    Task<GuildVaultItem?> GetVaultItemAsync(Guid vaultItemId, Guid? expectedGuildId, CancellationToken ct);
    Task<Character?> GetCharacterAsync(Guid characterId, CancellationToken ct);
    void Donate(InventoryItem inventoryItem, GuildVaultItem vaultItem);
    void AddToInventory(Guid characterId, Guid equipmentId);
    Task RemoveFromCharacterAsync(Guid characterId, Guid equipmentId, CancellationToken ct);
    Task WithdrawAsync(Guid characterId, GuildVaultItem vaultItem, CancellationToken ct);
}
