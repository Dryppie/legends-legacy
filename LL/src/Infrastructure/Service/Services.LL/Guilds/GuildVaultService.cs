using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Guilds;
using Domain.Extensions.Guilds;
using Domain.Models.Economy;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Guilds;

public class GuildVaultService : IGuildVaultService
{
    private readonly IDbContext _context;
    private readonly IEconomyLedgerRepository _economyLedger;

    public GuildVaultService(
        IDbContext context,
        IEconomyLedgerRepository economyLedger)
    {
        _context = context;
        _economyLedger = economyLedger;
    }

    public async Task<GuildOperationResult<GuildVaultMutation>> DonateAsync(Guid characterId, Guid equipmentInstanceId, CancellationToken cancellationToken)
    {
        var member = await GetMemberAsync(characterId, cancellationToken);
        if (member is null) return GuildOperationResult<GuildVaultMutation>.Fail("You are not in a guild.");

        var inventoryItem = await _context.InventoryItems
            .Include(x => x.ItemInstance)
                .ThenInclude(x => x.ItemBase)
                    .ThenInclude(x => (x as EquipmentBase)!.AttributeModifiers)
            .Include(x => x.ItemInstance)
                .ThenInclude(x => x.ItemBase)
                    .ThenInclude(x => (x as EquipmentBase)!.ToolBonuses)
            .Include(x => (x.ItemInstance as EquipmentInstance)!.InstanceModifiers)
            .Include(x => (x.ItemInstance as EquipmentInstance)!.ToolAffixes)
            .FirstOrDefaultAsync(
                x => x.InventoryId == characterId && x.ItemInstanceId == equipmentInstanceId,
                cancellationToken);
        if (inventoryItem?.ItemInstance is not EquipmentInstance equipment)
            return GuildOperationResult<GuildVaultMutation>.Fail("Only unequipped equipment can be donated.");
        if (await _context.EquipmentSlots.AnyAsync(
                x => x.EquipmentInstanceId == equipmentInstanceId,
                cancellationToken))
            return GuildOperationResult<GuildVaultMutation>.Fail("Equipped equipment must be unequipped before it can be donated.");
        if (await _context.GuildVaultItems.AnyAsync(x => x.EquipmentInstanceId == equipmentInstanceId, cancellationToken))
            return GuildOperationResult<GuildVaultMutation>.Fail("That equipment already belongs to a guild vault.");

        _context.InventoryItems.Remove(inventoryItem);
        var vaultItem = new GuildVaultItem
        {
            GuildId = member.GuildId,
            EquipmentInstanceId = equipmentInstanceId,
            DonatedByCharacterId = characterId
        };
        _context.GuildVaultItems.Add(vaultItem);
        await _economyLedger.RecordGuildVaultMovementAsync(
            EconomyEventType.GuildVaultDonation,
            vaultItem.Id,
            member.GuildId,
            member.Character,
            equipment,
            participantIsSender: true,
            source: "guild-vault:donation",
            cancellationToken: cancellationToken);

        return GuildOperationResult<GuildVaultMutation>.Success(new(
            member.GuildId,
            characterId,
            member.Character.Name,
            equipment));
    }

    public async Task<GuildOperationResult<bool>> BorrowAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken)
    {
        var member = await GetMemberAsync(characterId, cancellationToken);
        if (member is null) return GuildOperationResult<bool>.Fail("You are not in a guild.");
        if (!member.Guild.PermissionsFor(member.Role).CanBorrowVault)
            return GuildOperationResult<bool>.Fail("Your guild role cannot borrow vault equipment.");

        var vaultItem = await _context.GuildVaultItems
            .Include(x => x.EquipmentInstance)
                .ThenInclude(x => x.ItemBase)
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
            Quantity = 1,
            SeenAtUtc = DateTimeOffset.UtcNow
        });
        await _economyLedger.RecordGuildVaultMovementAsync(
            EconomyEventType.GuildVaultBorrow,
            vaultItem.Id,
            member.GuildId,
            member.Character,
            vaultItem.EquipmentInstance,
            participantIsSender: false,
            source: "guild-vault:borrow",
            cancellationToken: cancellationToken);

        return GuildOperationResult<bool>.Success(true);
    }

    public async Task<GuildOperationResult<bool>> ReturnAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken)
    {
        var vaultItem = await _context.GuildVaultItems
            .Include(x => x.EquipmentInstance)
                .ThenInclude(x => x.ItemBase)
            .FirstOrDefaultAsync(x => x.Id == vaultItemId && x.BorrowedByCharacterId == characterId, cancellationToken);
        if (vaultItem is null) return GuildOperationResult<bool>.Fail("You are not borrowing that equipment.");

        var character = await _context.Characters
            .FirstOrDefaultAsync(x => x.Id == characterId, cancellationToken);
        if (character is null) return GuildOperationResult<bool>.Fail("Your character could not be found.");

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
        await _economyLedger.RecordGuildVaultMovementAsync(
            EconomyEventType.GuildVaultReturn,
            vaultItem.Id,
            vaultItem.GuildId,
            character,
            vaultItem.EquipmentInstance,
            participantIsSender: true,
            source: "guild-vault:return",
            cancellationToken: cancellationToken);
        return GuildOperationResult<bool>.Success(true);
    }

    public async Task<GuildOperationResult<GuildVaultMutation>> WithdrawAsync(Guid characterId, Guid vaultItemId, CancellationToken cancellationToken)
    {
        var member = await GetMemberAsync(characterId, cancellationToken);
        if (member is null) return GuildOperationResult<GuildVaultMutation>.Fail("You are not in a guild.");

        var canWithdraw = member.Role == GuildRole.Leader
            || member.Role == GuildRole.Officer && member.Guild.PermissionsFor(member.Role).CanWithdrawVault;
        if (!canWithdraw)
            return GuildOperationResult<GuildVaultMutation>.Fail("Your guild role cannot withdraw vault equipment.");

        var vaultItem = await _context.GuildVaultItems
            .Include(x => x.EquipmentInstance)
                .ThenInclude(x => x.ItemBase)
                    .ThenInclude(x => (x as EquipmentBase)!.AttributeModifiers)
            .Include(x => x.EquipmentInstance)
                .ThenInclude(x => x.ItemBase)
                    .ThenInclude(x => (x as EquipmentBase)!.ToolBonuses)
            .Include(x => x.EquipmentInstance)
                .ThenInclude(x => x.InstanceModifiers)
            .Include(x => x.EquipmentInstance)
                .ThenInclude(x => x.ToolAffixes)
            .FirstOrDefaultAsync(x => x.Id == vaultItemId && x.GuildId == member.GuildId, cancellationToken);
        if (vaultItem is null) return GuildOperationResult<GuildVaultMutation>.Fail("Vault equipment was not found.");
        if (vaultItem.BorrowedByCharacterId is not null)
            return GuildOperationResult<GuildVaultMutation>.Fail("Borrowed equipment must be returned before it can be withdrawn.");

        // The vault record is the ownership source of truth. Older or interrupted
        // operations may have left a stale inventory/loadout reference behind;
        // normalize those references while completing the authorized withdrawal.
        var existingInventoryItems = await _context.InventoryItems
            .Where(x => x.ItemInstanceId == vaultItem.EquipmentInstanceId)
            .ToListAsync(cancellationToken);
        var withdrawingCharacterItem = existingInventoryItems
            .FirstOrDefault(x => x.InventoryId == characterId);
        foreach (var existingInventoryItem in existingInventoryItems)
        {
            if (existingInventoryItem != withdrawingCharacterItem)
                _context.InventoryItems.Remove(existingInventoryItem);
        }

        var equippedSlots = await _context.EquipmentSlots
            .Where(x => x.EquipmentInstanceId == vaultItem.EquipmentInstanceId)
            .ToListAsync(cancellationToken);
        foreach (var equippedSlot in equippedSlots)
        {
            equippedSlot.EquipmentInstanceId = null;
            equippedSlot.EquipmentInstance = null;
        }

        if (withdrawingCharacterItem is null)
        {
            _context.InventoryItems.Add(new InventoryItem
            {
                InventoryId = characterId,
                ItemInstanceId = vaultItem.EquipmentInstanceId,
                Quantity = 1,
                SeenAtUtc = DateTimeOffset.UtcNow
            });
        }
        else
        {
            withdrawingCharacterItem.Quantity = 1;
            withdrawingCharacterItem.SeenAtUtc ??= DateTimeOffset.UtcNow;
        }

        _context.GuildVaultItems.Remove(vaultItem);
        await _economyLedger.RecordGuildVaultMovementAsync(
            EconomyEventType.GuildVaultWithdrawal,
            vaultItem.Id,
            member.GuildId,
            member.Character,
            vaultItem.EquipmentInstance,
            participantIsSender: false,
            source: "guild-vault:withdrawal",
            cancellationToken: cancellationToken);

        return GuildOperationResult<GuildVaultMutation>.Success(new(
            member.GuildId,
            characterId,
            member.Character.Name,
            vaultItem.EquipmentInstance));
    }

    private Task<GuildMember?> GetMemberAsync(Guid characterId, CancellationToken cancellationToken) =>
        _context.GuildMembers
            .Include(x => x.Character)
            .Include(x => x.Guild)
                .ThenInclude(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.CharacterId == characterId, cancellationToken);

}
