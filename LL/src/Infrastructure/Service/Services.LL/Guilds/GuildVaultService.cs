using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Guilds;
using Domain.Extensions.Guilds;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Guilds;

public class GuildVaultService : IGuildVaultService
{
    private readonly IDbContext _context;

    public GuildVaultService(IDbContext context)
    {
        _context = context;
    }

    public async Task<GuildOperationResult<bool>> DonateAsync(Guid characterId, Guid equipmentInstanceId, CancellationToken cancellationToken)
    {
        var member = await GetMemberAsync(characterId, cancellationToken);
        if (member is null) return GuildOperationResult<bool>.Fail("You are not in a guild.");

        var inventoryItem = await _context.InventoryItems
            .Include(x => x.ItemInstance)
            .FirstOrDefaultAsync(
                x => x.InventoryId == characterId && x.ItemInstanceId == equipmentInstanceId,
                cancellationToken);
        if (inventoryItem?.ItemInstance is not EquipmentInstance)
            return GuildOperationResult<bool>.Fail("Only unequipped equipment can be donated.");
        if (await _context.GuildVaultItems.AnyAsync(x => x.EquipmentInstanceId == equipmentInstanceId, cancellationToken))
            return GuildOperationResult<bool>.Fail("That equipment already belongs to a guild vault.");

        _context.InventoryItems.Remove(inventoryItem);
        _context.GuildVaultItems.Add(new GuildVaultItem
        {
            GuildId = member.GuildId,
            EquipmentInstanceId = equipmentInstanceId,
            DonatedByCharacterId = characterId
        });

        return GuildOperationResult<bool>.Success(true);
    }

    public async Task<GuildOperationResult<bool>> BorrowAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken)
    {
        var member = await GetMemberAsync(characterId, cancellationToken);
        if (member is null) return GuildOperationResult<bool>.Fail("You are not in a guild.");
        if (!member.Guild.PermissionsFor(member.Role).CanBorrowVault)
            return GuildOperationResult<bool>.Fail("Your guild role cannot borrow vault equipment.");

        var vaultItem = await _context.GuildVaultItems
            .FirstOrDefaultAsync(x => x.Id == vaultItemId && x.GuildId == member.GuildId, cancellationToken);
        if (vaultItem is null) return GuildOperationResult<bool>.Fail("Vault equipment was not found.");
        if (vaultItem.BorrowedByCharacterId is not null)
            return GuildOperationResult<bool>.Fail("That equipment is already borrowed.");
        if (await _context.InventoryItems.AnyAsync(x => x.ItemInstanceId == vaultItem.EquipmentInstanceId, cancellationToken)
            || await _context.EquipmentSlots.AnyAsync(x => x.EquipmentInstanceId == vaultItem.EquipmentInstanceId, cancellationToken))
            return GuildOperationResult<bool>.Fail("That equipment is not currently available.");

        vaultItem.BorrowedByCharacterId = characterId;
        vaultItem.BorrowedAt = DateTimeOffset.UtcNow;
        _context.InventoryItems.Add(new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = vaultItem.EquipmentInstanceId,
            Quantity = 1
        });

        return GuildOperationResult<bool>.Success(true);
    }

    public async Task<GuildOperationResult<bool>> ReturnAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken)
    {
        var vaultItem = await _context.GuildVaultItems
            .FirstOrDefaultAsync(x => x.Id == vaultItemId && x.BorrowedByCharacterId == characterId, cancellationToken);
        if (vaultItem is null) return GuildOperationResult<bool>.Fail("You are not borrowing that equipment.");

        var inventoryItem = await _context.InventoryItems
            .FirstOrDefaultAsync(
                x => x.InventoryId == characterId && x.ItemInstanceId == vaultItem.EquipmentInstanceId,
                cancellationToken);
        if (inventoryItem is not null) _context.InventoryItems.Remove(inventoryItem);

        var equippedSlots = await _context.EquipmentSlots
            .Where(x => x.EntityId == characterId && x.EquipmentInstanceId == vaultItem.EquipmentInstanceId)
            .ToListAsync(cancellationToken);
        foreach (var slot in equippedSlots)
        {
            slot.EquipmentInstanceId = null;
            slot.EquipmentInstance = null;
        }

        vaultItem.BorrowedByCharacterId = null;
        vaultItem.BorrowedAt = null;
        return GuildOperationResult<bool>.Success(true);
    }

    private Task<GuildMember?> GetMemberAsync(Guid characterId, CancellationToken cancellationToken) =>
        _context.GuildMembers
            .Include(x => x.Guild)
                .ThenInclude(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.CharacterId == characterId, cancellationToken);
}
