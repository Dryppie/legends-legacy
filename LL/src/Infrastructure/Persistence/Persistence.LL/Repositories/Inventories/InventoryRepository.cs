using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.Economy;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Transfers;
using Domain.Models.MarketPlaces;
using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Inventories;
public class InventoryRepository : IInventoryRepository
{
    private readonly IDbContext _context;
    public InventoryRepository(IDbContext unitOfWork)
    {
        _context = unitOfWork;
    }

    public async Task<Inventory> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = await _context.Inventories
            .Include(i => i.InventoryItems)
                .ThenInclude(ii => ii.ItemInstance)
                    .ThenInclude(ii => ii.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase).AttributeModifiers)
            .Include(i => i.InventoryItems)
                .ThenInclude(ii => ii.ItemInstance)
                    .ThenInclude(ii => ii.ItemBase)
                        .ThenInclude(ib => (ib as EquipmentBase).ToolBonuses)
            .Include(i => i.InventoryItems)
                .ThenInclude(ii => (ii.ItemInstance as EquipmentInstance).InstanceModifiers)
            .Include(i => i.InventoryItems)
                .ThenInclude(ii => (ii.ItemInstance as EquipmentInstance).ToolAffixes)
            .Include(i => i.InventoryItems)
                .ThenInclude(ii => (ii.ItemInstance as EquipmentInstance).GuildVaultItem)
                    .ThenInclude(x => x!.Guild)
            .FirstOrDefaultAsync(i => i.CharacterId == characterId, cancellationToken); // Assuming CharacterId is the foreign key

        NotFoundException.ThrowIfNull(inventory, nameof(inventory), characterId);

        return inventory;
    }

    public Task AddItemsToInventory(
        Guid characterId,
        List<InventoryItem> items,
        string acquisitionSource,
        CancellationToken cancellationToken) =>
        AddItemsToInventoryCore(
            characterId,
            items,
            acquisitionSource,
            null,
            cancellationToken);

    public Task AddItemsToInventory(
        Guid characterId,
        List<InventoryItem> items,
        string acquisitionSource,
        Guid correlationId,
        CancellationToken cancellationToken) =>
        AddItemsToInventoryCore(
            characterId,
            items,
            acquisitionSource,
            correlationId,
            cancellationToken);

    private async Task AddItemsToInventoryCore(
        Guid characterId,
        List<InventoryItem> items,
        string acquisitionSource,
        Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var normalizedSource = string.IsNullOrWhiteSpace(acquisitionSource)
            ? ItemAcquisitionSources.Unknown
            : acquisitionSource.Trim();
        var acquiredAt = DateTimeOffset.UtcNow;
        foreach (var item in items)
        {
            item.ItemInstance.AcquiredAtUtc = acquiredAt;
            item.ItemInstance.AcquisitionSource = normalizedSource;
        }

        var recipient = await _context.Characters
            .AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new { x.Id, x.UserId, x.Level })
            .SingleOrDefaultAsync(cancellationToken);
        DateTime? recipientAccountCreatedUtc = null;
        if (recipient is not null)
        {
            recipientAccountCreatedUtc = await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == recipient.UserId)
                .Select(x => (DateTime?)x.CreatedUtc)
                .SingleOrDefaultAsync(cancellationToken);
        }

        // Separate stackable and non-stackable items
        var stackableGroups = items
            .Where(i => i.ItemInstance.ItemBase.Stackable)
            .GroupBy(i => i.ItemInstance.ItemBaseId)
            .ToDictionary(g => g.Key, g => new
            {
                TotalQuantity = g.Sum(x => x.Quantity),
                RepresentativeItem = g.First() // Used if we need to add a new instance
            });

        var nonStackableLoot = items
            .Where(item => !item.ItemInstance.ItemBase.Stackable)
            .ToList();

        var stackableBaseIds = stackableGroups.Keys.ToList();

        // Load existing inventory entries for stackables
        var existingStackables = await _context.InventoryItems
            .Include(ii => ii.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .Where(ii => ii.InventoryId == characterId && stackableBaseIds.Contains(ii.ItemInstance.ItemBaseId))
            .ToListAsync(cancellationToken);

        foreach (var (itemBaseId, group) in stackableGroups)
        {
            var existing = existingStackables.FirstOrDefault(i => i.ItemInstance.ItemBaseId == itemBaseId) ??
                       _context.InventoryItems.Local
                           .FirstOrDefault(i => i.InventoryId == characterId && i.ItemInstance.ItemBaseId == itemBaseId);

            if (existing != null)
            {
                existing.Quantity += group.TotalQuantity;
                AddAcquisitionLedgerEntry(
                    group.RepresentativeItem,
                    existing.ItemInstanceId,
                    group.TotalQuantity);
            }
            else
            {
                var itemToAdd = new InventoryItem
                {
                    InventoryId = characterId,
                    ItemInstanceId = group.RepresentativeItem.ItemInstanceId,
                    ItemInstance = group.RepresentativeItem.ItemInstance,
                    Quantity = group.TotalQuantity
                };

                if (_context.GetEntry(itemToAdd.ItemInstance).State == EntityState.Detached)
                    await _context.ItemInstances.AddAsync(itemToAdd.ItemInstance, cancellationToken);

                await _context.InventoryItems.AddAsync(itemToAdd, cancellationToken);
                AddAcquisitionLedgerEntry(
                    group.RepresentativeItem,
                    itemToAdd.ItemInstanceId,
                    group.TotalQuantity);
            }
        }

        // Add non-stackable items as separate entries
        foreach (var item in nonStackableLoot)
        {
            item.Quantity = 1;
            await _context.ItemInstances.AddAsync(item.ItemInstance, cancellationToken);
            await _context.InventoryItems.AddAsync(item, cancellationToken);
            AddAcquisitionLedgerEntry(item, item.ItemInstanceId, 1);
        }

        void AddAcquisitionLedgerEntry(
            InventoryItem sourceItem,
            Guid destinationItemInstanceId,
            int quantity)
        {
            _context.EconomyLedger.Add(new EconomyLedgerEntry
            {
                EventType = EconomyEventType.ItemAcquisition,
                AssetType = EconomyAssetType.Item,
                ReferenceId = correlationId ?? destinationItemInstanceId,
                RecipientAccountId = recipient?.UserId,
                RecipientCharacterId = characterId,
                RecipientAccountCreatedUtc = recipientAccountCreatedUtc,
                RecipientCharacterLevel = recipient?.Level,
                AssetId = sourceItem.ItemInstance.ItemBaseId,
                AssetName = sourceItem.ItemInstance.ItemBase.Name,
                DestinationItemInstanceId = destinationItemInstanceId,
                Quantity = quantity,
                Source = normalizedSource,
                OccurredAt = acquiredAt
            });
        }
    }

    public async Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem item, CancellationToken cancellationToken)
    {
        if (!item.ItemInstance.ItemBase.Stackable)
        {
            item.Quantity = 1;
            await _context.InventoryItems.AddAsync(item, cancellationToken);
            return;
        }

        var existing = await _context.InventoryItems
            .Include(ii => ii.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .FirstOrDefaultAsync(ii => ii.InventoryId == characterId && ii.ItemInstance.ItemBaseId == item.ItemInstance.ItemBaseId, cancellationToken);


        if (existing != null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            var itemToAdd = new InventoryItem
            {
                InventoryId = characterId,
                ItemInstanceId = item.ItemInstanceId,
                ItemInstance = item.ItemInstance,
                Quantity = item.Quantity,
            };

            if (_context.GetEntry(itemToAdd.ItemInstance).State == EntityState.Detached)
                await _context.ItemInstances.AddAsync(itemToAdd.ItemInstance, cancellationToken);

            await _context.InventoryItems.AddAsync(itemToAdd, cancellationToken);
        }
    }

    public async Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = new Inventory()
        {
            CharacterId = characterId,
        };

        await _context.Inventories.AddAsync(inventory, cancellationToken);
    }

    public async Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, Dictionary<string, int> requiredByItemId, CancellationToken cancellationToken) =>
        await TryRemoveItemsByBaseIdAsync(characterId, requiredByItemId, cancellationToken);

    public async Task<bool> TryRemoveItemsByBaseIdAsync(Guid characterId, Dictionary<string, int> requiredByItemId, CancellationToken cancellationToken)
    {
        var candidateRows = await _context.InventoryItems
            .Where(i => i.InventoryId == characterId && requiredByItemId.Keys.Contains(i.ItemInstance.ItemBaseId))
            .Include(i => i.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .ToListAsync(cancellationToken);

        // Check if all required items exist in sufficient quantity
        foreach (var kvp in requiredByItemId)
        {
            var totalOwned = candidateRows
                .Where(i => i.ItemInstance.ItemBase.Id == kvp.Key)
                .Sum(i => i.Quantity);

            if (totalOwned < kvp.Value)
                return false; // Not enough of this item
        }

        // Proceed to deduct
        foreach (var kvp in requiredByItemId)
        {
            var remainingToRemove = kvp.Value;

            foreach (var invItem in candidateRows.Where(i => i.ItemInstance.ItemBase.Id == kvp.Key).OrderByDescending(i => i.Quantity))
            {
                if (remainingToRemove <= 0) break;

                if (invItem.Quantity <= remainingToRemove)
                {
                    remainingToRemove -= invItem.Quantity;
                    _context.InventoryItems.Remove(invItem);
                }
                else
                {
                    invItem.Quantity -= remainingToRemove;
                    remainingToRemove = 0;
                }
            }
        }
        
        return true;
    }

    public async Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid inventoryItemId, CancellationToken cancellationToken) =>
        await _context.InventoryItems
            .Include(x => x.ItemInstance)
                .ThenInclude(x => x.ItemBase)
            .Where(x => x.InventoryId == characterId && x.ItemInstanceId == inventoryItemId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> MarkItemSeenAsync(Guid characterId, Guid itemInstanceId, CancellationToken cancellationToken)
    {
        var inventoryItem = await _context.InventoryItems
            .FirstOrDefaultAsync(
                x => x.InventoryId == characterId && x.ItemInstanceId == itemInstanceId,
                cancellationToken);

        if (inventoryItem is null)
            return false;

        inventoryItem.SeenAtUtc ??= DateTimeOffset.UtcNow;
        return true;
    }

    public async Task<bool> SetItemFavoriteAsync(
        Guid characterId,
        Guid itemInstanceId,
        bool isFavorite,
        CancellationToken cancellationToken)
    {
        var inventoryItem = await _context.InventoryItems
            .FirstOrDefaultAsync(
                x => x.InventoryId == characterId && x.ItemInstanceId == itemInstanceId,
                cancellationToken);

        if (inventoryItem is not null)
        {
            inventoryItem.IsFavorite = isFavorite;
            return true;
        }

        var equippedItem = await _context.EquipmentSlots
            .Where(x => x.EntityId == characterId && x.EquipmentInstanceId == itemInstanceId)
            .Select(x => x.EquipmentInstance)
            .FirstOrDefaultAsync(cancellationToken);

        if (equippedItem is null)
            return false;

        equippedItem.IsFavorite = isFavorite;
        return true;
    }

    public async Task<int> GetInventoryQuantityAsync(Guid characterId, string itemBaseId, CancellationToken cancellationToken) =>
        await _context.InventoryItems
            .Include(x => x.ItemInstance)
            .Where(x => x.InventoryId == characterId && x.ItemInstance.ItemBaseId == itemBaseId)
            .SumAsync(x => x.Quantity, cancellationToken);

    public void RemoveInventoryItem(InventoryItem inventoryItem) =>
        _context.InventoryItems.Remove(inventoryItem);

    public async Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing listing, CancellationToken cancellationToken)
    {
        if (await _context.GuildVaultItems.AnyAsync(
            x => x.EquipmentInstanceId == listing.ItemInstanceId && x.BorrowedByCharacterId == characterId,
            cancellationToken))
            return false;

        var invItem = await _context.InventoryItems
            .Where(i => i.InventoryId == characterId &&
                i.ItemInstanceId == listing.ItemInstanceId)
            .Include(i => i.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .SingleOrDefaultAsync(cancellationToken);

        if (invItem == null || invItem.ItemInstance == null || invItem.ItemInstance.ItemBase == null)
            return false;

        bool isStackable = invItem.ItemInstance.ItemBase.Stackable;

        if (!isStackable)
        {
            _context.InventoryItems.Remove(invItem);
            return true;
        }

        var qty = listing.Quantity;
        if (invItem.Quantity < qty) return false;

        if (invItem.Quantity == qty)
        {
            _context.InventoryItems.Remove(invItem);
        }
        else
        {
            invItem.Quantity -= qty;
        }
        
        return true;
    }

    public async Task<InventoryItem?> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken)
    {
        var itemToAdd = new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = itemInstance.Id,
            ItemInstance = itemInstance,
            Quantity = 1
        };

        if (itemInstance is EquipmentInstance eq)
        {
            foreach (var mod in eq.InstanceModifiers)
            {
                if (_context.GetEntry(mod).State == EntityState.Detached)
                    _context.GetEntry(mod).State = EntityState.Added;
            }

            foreach (var affix in eq.ToolAffixes)
            {
                if (_context.GetEntry(affix).State == EntityState.Detached)
                    _context.GetEntry(affix).State = EntityState.Added;
            }
        }

        await _context.InventoryItems.AddAsync(itemToAdd, cancellationToken);
        return itemToAdd;
    }

    public async Task<InventoryItem?> ScrapEquipments(Guid characterId, List<Guid> parsedGuids, CancellationToken cancellationToken)
    {
        // Fetch all inventory items for this character in a single query
        var inventoryItems = await _context.InventoryItems
            .Where(i => i.InventoryId == characterId)
            .Include(i => i.ItemInstance)
                .ThenInclude(ii => ii.ItemBase)
            .ToListAsync(cancellationToken);

        // Find the equipment items
        var equipmentInventoryItems = inventoryItems
            .Where(i => parsedGuids.Contains(i.ItemInstance.Id));

        if (!equipmentInventoryItems.Any()) return null;
        if (parsedGuids.Count == 0 || parsedGuids.Count != equipmentInventoryItems.Count()) return null;
        if (await _context.GuildVaultItems.AnyAsync(
            x => x.BorrowedByCharacterId == characterId && parsedGuids.Contains(x.EquipmentInstanceId),
            cancellationToken))
            return null;
        if (equipmentInventoryItems.Any(i => i.ItemInstance is not EquipmentInstance))
        {
            return null;
        }

        // Define Tempered Scrap gain logic
        const int temperedScrapPerEquipment = 1;
        var temperedScrapGained = temperedScrapPerEquipment * parsedGuids.Count;

        // Remaining potential does not affect scrap eligibility.
        _context.InventoryItems.RemoveRange(equipmentInventoryItems);

        // Try to find Tempered Scrap item
        var temperedScrapItemId = "tempered_scrap";
        var temperedScrap = inventoryItems
            .FirstOrDefault(i => i.ItemInstance.ItemBase.Id == temperedScrapItemId);

        if (temperedScrap != null) temperedScrap.Quantity += temperedScrapGained;
        else
        {
            var itemBase = inventoryItems
                .Select(i => i.ItemInstance.ItemBase)
                .FirstOrDefault(b => b.Id == temperedScrapItemId);

            if (itemBase == null)
            {
                // Only query ItemBase if it's *really* not already in memory
                itemBase = await _context.ItemBases
                    .Where(b => b.Id == temperedScrapItemId)
                    .SingleOrDefaultAsync(cancellationToken);

                if (itemBase == null) return null;
            }

            var itemInstance = new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase,
                AcquiredAtUtc = DateTimeOffset.UtcNow,
                AcquisitionSource = ItemAcquisitionSources.EquipmentScrapping
            };

            temperedScrap = new InventoryItem
            {
                InventoryId = characterId,
                ItemInstance = itemInstance,
                Quantity = temperedScrapGained
            };

            _context.InventoryItems.Add(temperedScrap);
        }

        var recipient = await _context.Characters
            .AsNoTracking()
            .Where(x => x.Id == characterId)
            .Select(x => new { x.Id, x.UserId, x.Level })
            .SingleOrDefaultAsync(cancellationToken);
        DateTime? recipientAccountCreatedUtc = null;
        if (recipient is not null)
        {
            recipientAccountCreatedUtc = await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == recipient.UserId)
                .Select(x => (DateTime?)x.CreatedUtc)
                .SingleOrDefaultAsync(cancellationToken);
        }

        _context.EconomyLedger.Add(new EconomyLedgerEntry
        {
            EventType = EconomyEventType.ItemAcquisition,
            AssetType = EconomyAssetType.Item,
            ReferenceId = temperedScrap.ItemInstanceId,
            RecipientAccountId = recipient?.UserId,
            RecipientCharacterId = characterId,
            RecipientAccountCreatedUtc = recipientAccountCreatedUtc,
            RecipientCharacterLevel = recipient?.Level,
            AssetId = temperedScrap.ItemInstance.ItemBase.Id,
            AssetName = temperedScrap.ItemInstance.ItemBase.Name,
            DestinationItemInstanceId = temperedScrap.ItemInstanceId,
            Quantity = temperedScrapGained,
            Source = ItemAcquisitionSources.EquipmentScrapping
        });

        return temperedScrap;
    }

    public async Task<InventoryTransferResult> TransferItemAsync(
        Guid senderCharacterId,
        Guid recipientCharacterId,
        Guid itemInstanceId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (senderCharacterId == recipientCharacterId)
            return InventoryTransferResult.Fail(InventoryTransferFailure.SameRecipient);
        if (quantity <= 0)
            return InventoryTransferResult.Fail(InventoryTransferFailure.InvalidQuantity);

        await _context.AcquireCharacterRowsLockAsync(
            [senderCharacterId, recipientCharacterId],
            cancellationToken);

        var senderItem = await _context.InventoryItems
            .AsSplitQuery()
            .Include(x => x.ItemInstance)
                .ThenInclude(x => x.ItemBase)
                    .ThenInclude(x => (x as EquipmentBase).AttributeModifiers)
            .Include(x => x.ItemInstance)
                .ThenInclude(x => x.ItemBase)
                    .ThenInclude(x => (x as EquipmentBase).ToolBonuses)
            .Include(x => (x.ItemInstance as EquipmentInstance).InstanceModifiers)
            .Include(x => (x.ItemInstance as EquipmentInstance).ToolAffixes)
            .SingleOrDefaultAsync(
                x => x.InventoryId == senderCharacterId && x.ItemInstanceId == itemInstanceId,
                cancellationToken);

        if (senderItem?.ItemInstance?.ItemBase is null)
            return InventoryTransferResult.Fail(InventoryTransferFailure.ItemNotFound);
        if (senderItem.ItemInstance.ItemBase.IsBound)
            return InventoryTransferResult.Fail(InventoryTransferFailure.ItemIsBound);
        if (!senderItem.ItemInstance.ItemBase.Stackable && quantity != 1)
            return InventoryTransferResult.Fail(InventoryTransferFailure.NonStackableQuantity);
        if (senderItem.Quantity < quantity)
            return InventoryTransferResult.Fail(InventoryTransferFailure.InsufficientQuantity);
        if (await _context.GuildVaultItems.AnyAsync(
                x => x.EquipmentInstanceId == itemInstanceId && x.BorrowedByCharacterId == senderCharacterId,
                cancellationToken))
            return InventoryTransferResult.Fail(InventoryTransferFailure.BorrowedGuildItem);
        if (!await _context.Inventories.AnyAsync(x => x.CharacterId == recipientCharacterId, cancellationToken))
            return InventoryTransferResult.Fail(InventoryTransferFailure.RecipientInventoryNotFound);

        var participants = await _context.Characters
            .Where(x => x.Id == senderCharacterId || x.Id == recipientCharacterId)
            .ToListAsync(cancellationToken);
        var sender = participants.FirstOrDefault(x => x.Id == senderCharacterId);
        if (sender is null)
            return InventoryTransferResult.Fail(InventoryTransferFailure.SenderNotFound);
        var recipient = participants.FirstOrDefault(x => x.Id == recipientCharacterId);
        if (recipient is null)
            return InventoryTransferResult.Fail(InventoryTransferFailure.RecipientNotFound);

        var now = DateTimeOffset.UtcNow;
        var participantAccounts = await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == sender.UserId || x.Id == recipient.UserId)
            .Select(x => new
            {
                x.Id,
                x.IsGuest,
                x.CreatedUtc,
                IsRestricted = _context.AccountRestrictions.Any(restriction =>
                    restriction.AccountId == x.Id &&
                    restriction.RevokedAt == null &&
                    (restriction.ExpiresAt == null || restriction.ExpiresAt > now) &&
                    (restriction.RestrictionType == AccountRestrictionType.Ban ||
                     restriction.RestrictionType == AccountRestrictionType.MultiplayerRestriction))
            })
            .ToListAsync(cancellationToken);
        if (participantAccounts.Any(x => x.IsGuest))
            return InventoryTransferResult.Fail(InventoryTransferFailure.GuestAccount);
        if (participantAccounts.Any(x => x.IsRestricted))
            return InventoryTransferResult.Fail(InventoryTransferFailure.AccountRestricted);

        var itemBase = senderItem.ItemInstance.ItemBase;
        InventoryItem recipientItem;

        if (itemBase.Stackable)
        {
            var existingRecipientStack = await _context.InventoryItems
                .Include(x => x.ItemInstance)
                    .ThenInclude(x => x.ItemBase)
                .FirstOrDefaultAsync(
                    x => x.InventoryId == recipientCharacterId &&
                         x.ItemInstance.ItemBaseId == senderItem.ItemInstance.ItemBaseId,
                    cancellationToken);

            if (existingRecipientStack is not null)
            {
                existingRecipientStack.Quantity += quantity;
                recipientItem = new InventoryItem
                {
                    InventoryId = recipientCharacterId,
                    ItemInstanceId = existingRecipientStack.ItemInstanceId,
                    ItemInstance = existingRecipientStack.ItemInstance,
                    Quantity = quantity
                };
            }
            else
            {
                recipientItem = new InventoryItem
                {
                    InventoryId = recipientCharacterId,
                    ItemInstanceId = senderItem.ItemInstanceId,
                    ItemInstance = senderItem.ItemInstance,
                    Quantity = quantity
                };
                await _context.InventoryItems.AddAsync(recipientItem, cancellationToken);
            }
        }
        else
        {
            recipientItem = new InventoryItem
            {
                InventoryId = recipientCharacterId,
                ItemInstanceId = senderItem.ItemInstanceId,
                ItemInstance = senderItem.ItemInstance,
                Quantity = 1
            };
            await _context.InventoryItems.AddAsync(recipientItem, cancellationToken);
        }

        if (senderItem.Quantity == quantity)
            _context.InventoryItems.Remove(senderItem);
        else
            senderItem.Quantity -= quantity;

        var transferRecord = new PlayerTransferRecord
        {
            Kind = PlayerTransferKind.InventoryItem,
            SenderAccountId = sender.UserId,
            SenderCharacterId = sender.Id,
            SenderCharacterName = sender.Name,
            RecipientAccountId = recipient.UserId,
            RecipientCharacterId = recipient.Id,
            RecipientCharacterName = recipient.Name,
            AssetId = itemBase.Id,
            AssetName = itemBase.Name,
            SourceItemInstanceId = itemInstanceId,
            DestinationItemInstanceId = recipientItem.ItemInstanceId,
            Quantity = quantity
        };
        _context.PlayerTransferHistory.Add(transferRecord);

        var accountCreatedUtc = participantAccounts.ToDictionary(x => x.Id, x => x.CreatedUtc);
        _context.EconomyLedger.Add(new EconomyLedgerEntry
        {
            EventType = EconomyEventType.DirectItemTransfer,
            AssetType = EconomyAssetType.Item,
            ReferenceId = transferRecord.Id,
            SenderAccountId = sender.UserId,
            SenderCharacterId = sender.Id,
            SenderAccountCreatedUtc = accountCreatedUtc.TryGetValue(sender.UserId, out var senderCreatedUtc)
                ? senderCreatedUtc
                : null,
            SenderCharacterLevel = sender.Level,
            RecipientAccountId = recipient.UserId,
            RecipientCharacterId = recipient.Id,
            RecipientAccountCreatedUtc = accountCreatedUtc.TryGetValue(recipient.UserId, out var recipientCreatedUtc)
                ? recipientCreatedUtc
                : null,
            RecipientCharacterLevel = recipient.Level,
            AssetId = itemBase.Id,
            AssetName = itemBase.Name,
            SourceItemInstanceId = itemInstanceId,
            DestinationItemInstanceId = recipientItem.ItemInstanceId,
            Quantity = quantity,
            Source = "player-transfer",
            OccurredAt = transferRecord.OccurredAt
        });

        return InventoryTransferResult.Success(recipientItem, transferRecord);
    }
}
