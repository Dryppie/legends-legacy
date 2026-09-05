using Application.Common.Interfaces;
using Domain.Models.Economy;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Equipments;

public sealed class EquipmentUpgradeRepository(IDbContext db, EquipmentBlueprintCatalog? blueprints = null) : IEquipmentUpgradeRepository
{
    public async Task<EquipmentUpgradeReceipt?> GetReceiptAsync(
        Guid characterId,
        Guid operationId,
        CancellationToken cancellationToken) =>
        db.EquipmentUpgradeReceipts.Local.SingleOrDefault(receipt =>
            receipt.CharacterId == characterId && receipt.OperationId == operationId)
        ?? await db.EquipmentUpgradeReceipts.SingleOrDefaultAsync(receipt =>
            receipt.CharacterId == characterId && receipt.OperationId == operationId,
            cancellationToken);

    public async Task<EquipmentUpgradeContext?> LoadAsync(
        Guid characterId,
        Guid itemId,
        bool forMutation,
        CancellationToken cancellationToken)
    {
        if (forMutation)
            await db.AcquireCharacterRowsLockAsync([characterId], cancellationToken);

        var character = await db.Characters.SingleOrDefaultAsync(
            candidate => candidate.Id == characterId,
            cancellationToken);
        if (character is null)
            return null;

        var blueprintIds = blueprints?.Blueprints.Select(x => x.ItemId).ToArray() ?? [];
        var inventoryRows = await db.InventoryItems
            .Include(row => row.ItemInstance)
                .ThenInclude(instance => instance.ItemBase)
            .Include(row => (row.ItemInstance as EquipmentInstance)!.InstanceModifiers)
            .Where(row => row.InventoryId == characterId
                && (row.ItemInstanceId == itemId
                    || row.ItemInstance.ItemBaseId == EquipmentKeys.ReinforcementPartsItemBaseId
                    || blueprintIds.Contains(row.ItemInstance.ItemBaseId)))
            .ToListAsync(cancellationToken);
        inventoryRows = inventoryRows
            .Concat(db.InventoryItems.Local.Where(row => row.InventoryId == characterId
                && (row.ItemInstanceId == itemId
                    || row.ItemInstance.ItemBaseId == EquipmentKeys.ReinforcementPartsItemBaseId
                    || blueprintIds.Contains(row.ItemInstance.ItemBaseId))))
            .DistinctBy(row => row.ItemInstanceId)
            .Where(row => db.GetEntry(row).State != EntityState.Deleted)
            .ToList();

        var inventoryItem = inventoryRows.SingleOrDefault(row => row.ItemInstanceId == itemId);
        var equippedSlots = await db.EquipmentSlots
            .Include(slot => slot.EquipmentInstance)
                .ThenInclude(instance => instance!.ItemBase)
            .Include(slot => slot.EquipmentInstance)
                .ThenInclude(instance => instance!.InstanceModifiers)
            .Where(slot => slot.EquipmentInstanceId == itemId)
            .ToListAsync(cancellationToken);
        var ownedSlots = equippedSlots.Where(slot => slot.EntityId == characterId).ToArray();
        var equipment = inventoryItem?.ItemInstance as EquipmentInstance
            ?? ownedSlots.FirstOrDefault()?.EquipmentInstance;

        string? unavailableReason = null;
        if (inventoryItem is null && ownedSlots.Length == 0)
            unavailableReason = "Item is not in your inventory or equipment slots.";
        if (equippedSlots.Any(slot => slot.EntityId != characterId)
            || inventoryItem is not null && ownedSlots.Length > 0)
            unavailableReason = "Item location is inconsistent; this item cannot be changed.";
        if (await db.MarketPlaceListings.AnyAsync(
                listing => listing.ItemInstanceId == itemId,
                cancellationToken))
            unavailableReason = "Listed items cannot be reinforced or dismantled.";
        if (await db.GuildVaultItems.AnyAsync(
                vaultItem => vaultItem.EquipmentInstanceId == itemId,
                cancellationToken))
            unavailableReason = "Guild equipment cannot be reinforced or dismantled personally.";

        return new EquipmentUpgradeContext(
            character,
            inventoryItem,
            equipment,
            ownedSlots.Length > 0,
            unavailableReason,
            inventoryRows
                .Where(row => row.ItemInstance.ItemBaseId == EquipmentKeys.ReinforcementPartsItemBaseId)
                .OrderBy(row => row.ItemInstanceId)
                .ToArray(),
            inventoryRows.Where(row => blueprintIds.Contains(row.ItemInstance.ItemBaseId))
                .OrderBy(row => row.ItemInstanceId).ToArray());
    }

    public async Task ApplyAsync(
        EquipmentUpgradeContext context,
        EquipmentUpgradeQuote quote,
        EquipmentUpgradeReceipt receipt,
        CancellationToken cancellationToken)
    {
        if (!quote.CanExecute)
            throw new InvalidOperationException("An unavailable equipment-upgrade quote cannot be committed.");

        var character = context.Character;
        var outcome = receipt.Outcome;
        character.Cinders = checked(character.Cinders - quote.CinderCost);

        var remainingParts = quote.PartsCost;
        foreach (var stack in context.PartStacks)
        {
            var spent = Math.Min(remainingParts, stack.Quantity);
            if (spent == 0)
                break;
            Consume(stack, (int)spent);
            remainingParts -= spent;
            Ledger(
                EquipmentKeys.ReinforcementPartsItemBaseId,
                "Reinforcement Parts",
                spent,
                outgoing: true,
                stack.ItemInstanceId);
        }
        if (remainingParts != 0)
            throw new InvalidOperationException("Reinforcement Parts changed while applying the upgrade.");
        if (quote.CinderCost > 0)
            Ledger("currency:cinders", "Cinders", quote.CinderCost, outgoing: true);

        switch (quote.Request.Kind)
        {
            case EquipmentUpgradeOperationKind.ApplyVariant:
                var blueprintStack = context.BlueprintStacks?.FirstOrDefault(x =>
                    x.ItemInstance.ItemBaseId == quote.BlueprintItemId && x.Quantity > 0)
                    ?? throw new InvalidOperationException("Blueprint balance changed during conversion.");
                Consume(blueprintStack, 1);
                Ledger(blueprintStack.ItemInstance.ItemBaseId, blueprintStack.ItemInstance.ItemBase.Name,
                    1, outgoing: true, blueprintStack.ItemInstanceId);
                context.Equipment!.ApplyProgressionData(quote.After!);
                break;
            case EquipmentUpgradeOperationKind.Reinforce:
                context.Equipment!.ApplyProgressionData(quote.After!);
                break;
            case EquipmentUpgradeOperationKind.Dismantle:
                var item = context.InventoryItem!;
                var equipment = context.Equipment!;
                db.InventoryItems.Remove(item);
                db.ItemInstances.Remove(equipment);
                Ledger(equipment.ItemBaseId, equipment.DisplayName, 1, outgoing: true, equipment.Id);
                if (quote.PartsReturned > 0)
                {
                    var partsBase = await db.ItemBases.SingleOrDefaultAsync(
                        candidate => candidate.Id == EquipmentKeys.ReinforcementPartsItemBaseId,
                        cancellationToken);
                    if (partsBase is not { Stackable: true, IsBound: true })
                        throw new InvalidOperationException("Reinforcement Parts are not configured correctly.");

                    var partsStack = context.PartStacks.FirstOrDefault();
                    if (partsStack is null)
                    {
                        var instance = new ItemInstance
                        {
                            Id = Guid.NewGuid(),
                            ItemBaseId = partsBase.Id,
                            ItemBase = partsBase,
                            AcquisitionSource = ItemAcquisitionSources.EquipmentDismantle,
                            AcquiredAtUtc = outcome.OccurredAtUtc
                        };
                        partsStack = new InventoryItem
                        {
                            InventoryId = character.Id,
                            ItemInstanceId = instance.Id,
                            ItemInstance = instance,
                            Quantity = 0,
                            SeenAtUtc = outcome.OccurredAtUtc
                        };
                        db.InventoryItems.Add(partsStack);
                    }

                    partsStack.Quantity = checked(partsStack.Quantity + (int)quote.PartsReturned);
                    Ledger(
                        EquipmentKeys.ReinforcementPartsItemBaseId,
                        partsBase.Name,
                        quote.PartsReturned,
                        outgoing: false,
                        partsStack.ItemInstanceId);
                }
                break;
            default:
                throw new InvalidOperationException("Unsupported equipment upgrade operation.");
        }

        db.EquipmentUpgradeReceipts.Add(receipt);

        void Consume(InventoryItem stack, int quantity)
        {
            if (quantity <= 0 || stack.Quantity < quantity)
                throw new InvalidOperationException("Inventory changed during the equipment upgrade.");
            stack.Quantity -= quantity;
            if (stack.Quantity == 0)
                db.InventoryItems.Remove(stack);
        }

        void Ledger(
            string assetId,
            string assetName,
            long quantity,
            bool outgoing,
            Guid? instanceId = null) => db.EconomyLedger.Add(new EconomyLedgerEntry
        {
            EventType = EconomyEventType.EquipmentUpgrade,
            AssetType = assetId == "currency:cinders"
                ? EconomyAssetType.Currency
                : EconomyAssetType.Item,
            ReferenceId = receipt.OperationId,
            AssetId = assetId,
            AssetName = assetName,
            Quantity = quantity,
            SenderCharacterId = outgoing ? character.Id : null,
            SenderAccountId = outgoing ? character.UserId : null,
            SenderCharacterLevel = outgoing ? character.Level : null,
            SourceItemInstanceId = outgoing ? instanceId : null,
            RecipientCharacterId = outgoing ? null : character.Id,
            RecipientAccountId = outgoing ? null : character.UserId,
            RecipientCharacterLevel = outgoing ? null : character.Level,
            DestinationItemInstanceId = outgoing ? null : instanceId,
            Source = quote.Request.Kind switch
            {
                EquipmentUpgradeOperationKind.Reinforce => EquipmentKeys.ReinforcementSource,
                EquipmentUpgradeOperationKind.ApplyVariant => "equipment-blueprint",
                _ => EquipmentKeys.DismantleSource
            },
            OccurredAt = outcome.OccurredAtUtc
        });
    }
}
