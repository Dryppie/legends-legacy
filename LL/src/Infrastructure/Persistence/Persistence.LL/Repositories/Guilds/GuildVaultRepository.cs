using Application.Common.Interfaces;
using Domain.Models.Entities.Characters;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Guilds;

public sealed class GuildVaultRepository(IDbContext context) : IGuildVaultRepository
{
    public async Task<GuildMember?> GetMemberAsync(Guid characterId, CancellationToken ct)
    {
        var guildId = await context.GuildMembers.Where(x => x.CharacterId == characterId)
            .Select(x => (Guid?)x.GuildId).SingleOrDefaultAsync(ct);
        if (guildId is null) return null;
        await context.AcquireStateSyncScopeLockAsync($"guild-vault:{guildId:N}", ct);
        return await context.GuildMembers.Include(x => x.Character)
            .Include(x => x.Guild).ThenInclude(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.CharacterId == characterId && x.GuildId == guildId, ct);
    }

    public Task<InventoryItem?> GetDonationAsync(Guid characterId, Guid equipmentId, CancellationToken ct) =>
        context.InventoryItems
            .Include(x => x.ItemInstance).ThenInclude(x => x.ItemBase).ThenInclude(x => (x as EquipmentBase)!.AttributeModifiers)
            .Include(x => x.ItemInstance).ThenInclude(x => x.ItemBase).ThenInclude(x => (x as EquipmentBase)!.ToolBonuses)
            .Include(x => (x.ItemInstance as EquipmentInstance)!.InstanceModifiers)
            .Include(x => (x.ItemInstance as EquipmentInstance)!.ToolAffixes)
            .FirstOrDefaultAsync(x => x.InventoryId == characterId && x.ItemInstanceId == equipmentId, ct);

    public Task<bool> IsEquippedAsync(Guid equipmentId, CancellationToken ct) =>
        context.EquipmentSlots.AnyAsync(x => x.EquipmentInstanceId == equipmentId, ct);
    public Task<bool> IsInVaultAsync(Guid equipmentId, CancellationToken ct) =>
        context.GuildVaultItems.AnyAsync(x => x.EquipmentInstanceId == equipmentId, ct);
    public Task<bool> IsInInventoryAsync(Guid equipmentId, CancellationToken ct) =>
        context.InventoryItems.AnyAsync(x => x.ItemInstanceId == equipmentId, ct);

    public async Task<GuildVaultItem?> GetVaultItemAsync(Guid vaultItemId, Guid? expectedGuildId, CancellationToken ct)
    {
        var guildId = await context.GuildVaultItems.Where(x => x.Id == vaultItemId && (!expectedGuildId.HasValue || x.GuildId == expectedGuildId))
            .Select(x => (Guid?)x.GuildId).SingleOrDefaultAsync(ct);
        if (guildId is null) return null;
        await context.AcquireStateSyncScopeLockAsync($"guild-vault:{guildId:N}", ct);
        return await context.GuildVaultItems
            .Include(x => x.EquipmentInstance).ThenInclude(x => x.ItemBase).ThenInclude(x => (x as EquipmentBase)!.AttributeModifiers)
            .Include(x => x.EquipmentInstance).ThenInclude(x => x.ItemBase).ThenInclude(x => (x as EquipmentBase)!.ToolBonuses)
            .Include(x => x.EquipmentInstance).ThenInclude(x => x.InstanceModifiers)
            .Include(x => x.EquipmentInstance).ThenInclude(x => x.ToolAffixes)
            .FirstOrDefaultAsync(x => x.Id == vaultItemId, ct);
    }

    public Task<Character?> GetCharacterAsync(Guid characterId, CancellationToken ct) =>
        context.Characters.FirstOrDefaultAsync(x => x.Id == characterId, ct);

    public void Donate(InventoryItem inventoryItem, GuildVaultItem vaultItem)
    {
        context.InventoryItems.Remove(inventoryItem);
        context.GuildVaultItems.Add(vaultItem);
    }

    public void AddToInventory(Guid characterId, Guid equipmentId) => context.InventoryItems.Add(new InventoryItem
    {
        InventoryId = characterId, ItemInstanceId = equipmentId, Quantity = 1, SeenAtUtc = DateTimeOffset.UtcNow
    });

    public async Task RemoveFromCharacterAsync(Guid characterId, Guid equipmentId, CancellationToken ct)
    {
        var items = await context.InventoryItems.Where(x => x.InventoryId == characterId && x.ItemInstanceId == equipmentId).ToListAsync(ct);
        context.InventoryItems.RemoveRange(items);
        var slots = await context.EquipmentSlots.Where(x => x.EntityId == characterId && x.EquipmentInstanceId == equipmentId).ToListAsync(ct);
        foreach (var slot in slots) { slot.EquipmentInstanceId = null; slot.EquipmentInstance = null; }
    }

    public async Task WithdrawAsync(Guid characterId, GuildVaultItem vaultItem, CancellationToken ct)
    {
        // Repair stale references before completing an authorized legacy withdrawal.
        var items = await context.InventoryItems.Where(x => x.ItemInstanceId == vaultItem.EquipmentInstanceId).ToListAsync(ct);
        var kept = items.FirstOrDefault(x => x.InventoryId == characterId);
        context.InventoryItems.RemoveRange(items.Where(x => x != kept));
        var slots = await context.EquipmentSlots.Where(x => x.EquipmentInstanceId == vaultItem.EquipmentInstanceId).ToListAsync(ct);
        foreach (var slot in slots) { slot.EquipmentInstanceId = null; slot.EquipmentInstance = null; }
        if (kept is null) AddToInventory(characterId, vaultItem.EquipmentInstanceId);
        else { kept.Quantity = 1; kept.SeenAtUtc ??= DateTimeOffset.UtcNow; }
        context.GuildVaultItems.Remove(vaultItem);
    }
}
